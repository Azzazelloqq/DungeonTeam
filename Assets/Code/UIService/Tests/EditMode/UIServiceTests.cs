using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using ResourceLoader;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Code.UIService.Tests
{
    public sealed class UIServiceTests
    {
        private readonly List<GameObject> _objects = new();

        private FakeResourceLoader _resourceLoader;
        private UIService _service;

        [SetUp]
        public void SetUp()
        {
            _resourceLoader = new FakeResourceLoader();
            _service = new UIService(_resourceLoader, CreateCanvasContext());
        }

        [TearDown]
        public void TearDown()
        {
            _service?.Dispose();

            for (var index = _objects.Count - 1; index >= 0; index--)
            {
                if (_objects[index] != null)
                    Object.DestroyImmediate(_objects[index]);
            }

            _objects.Clear();
        }

        [Test]
        public async Task CreateAsync_DefaultsToHidden_HidesBeforeFirstEnable()
        {
            var prefab = RegisterPrefab(
                "fullscreen",
                UIElementGroup.FullScreen,
                UIElementHideBehavior.KeepInQueue);

            var instance = await _service.CreateAsync<TestUIElement>("fullscreen").AsTask();

            Assert.That(instance.HideImmediatelyCount, Is.EqualTo(1));
            Assert.That(instance.WasHiddenWhenEnabled, Is.True);
            Assert.That(instance.IsVisible, Is.False);
            Assert.That(instance.transform.parent, Is.SameAs(GetGroupParent(UIElementGroup.FullScreen)));
            Assert.That(prefab.activeSelf, Is.False);
        }

        [Test]
        public async Task CreateAsync_WithUIInterface_ReturnsItsImplementation()
        {
            RegisterPrefab(
                "interface-contract",
                UIElementGroup.OverlayElement,
                UIElementHideBehavior.KeepInQueue);

            var element = await _service.CreateAsync<ITestUIElement>("interface-contract").AsTask();

            Assert.That(element, Is.InstanceOf<TestUIElement>());
        }

        [Test]
        public async Task ShowAsync_SecondFullScreen_HidesFirstAndCloseReturnsFirst()
        {
            RegisterPrefab("first", UIElementGroup.FullScreen, UIElementHideBehavior.KeepInQueue);
            RegisterPrefab("second", UIElementGroup.FullScreen, UIElementHideBehavior.Close);

            var first = await _service.CreateAsync<TestUIElement>("first").AsTask();
            var second = await _service.CreateAsync<TestUIElement>("second").AsTask();

            await _service.ShowAsync(first).AsTask();
            await _service.ShowAsync(second).AsTask();

            Assert.That(first.IsVisible, Is.False);
            Assert.That(first.HideCount, Is.EqualTo(1));
            Assert.That(second.IsVisible, Is.True);

            await _service.CloseAsync(second).AsTask();

            Assert.That(second == null, Is.True);
            Assert.That(first.IsVisible, Is.True);
            Assert.That(first.ShowCount, Is.EqualTo(2));
        }

        [Test]
        public async Task ShowAsync_PopupsAreWaiting_ShowsThemInFifoOrder()
        {
            RegisterPrefab("first", UIElementGroup.Popup, UIElementHideBehavior.Close);
            RegisterPrefab("second", UIElementGroup.Popup, UIElementHideBehavior.Close);
            RegisterPrefab("third", UIElementGroup.Popup, UIElementHideBehavior.Close);

            var first = await _service.CreateAsync<TestUIElement>("first").AsTask();
            var second = await _service.CreateAsync<TestUIElement>("second").AsTask();
            var third = await _service.CreateAsync<TestUIElement>("third").AsTask();

            await _service.ShowAsync(first).AsTask();
            var showSecond = _service.ShowAsync(second).AsTask();
            var showThird = _service.ShowAsync(third).AsTask();
            await Task.Yield();

            Assert.That(second.IsVisible, Is.False);
            Assert.That(third.IsVisible, Is.False);

            await _service.CloseAsync(first).AsTask();
            await showSecond;

            Assert.That(second.IsVisible, Is.True);
            Assert.That(third.IsVisible, Is.False);

            await _service.CloseAsync(second).AsTask();
            await showThird;

            Assert.That(third.IsVisible, Is.True);
        }

        [Test]
        public async Task HideAsync_CloseBehavior_ReleasesCreatedResource()
        {
            var prefab = RegisterPrefab(
                "dynamic-overlay",
                UIElementGroup.DynamicOverlayElement,
                UIElementHideBehavior.Close);

            var instance = await _service.CreateAsync<TestUIElement>("dynamic-overlay").AsTask();
            await _service.ShowAsync(instance).AsTask();

            await _service.HideAsync(instance).AsTask();

            Assert.That(instance == null, Is.True);
            Assert.That(_resourceLoader.GetReleaseCount(prefab), Is.EqualTo(1));
        }

        [Test]
        public async Task ShowAsync_OverlayElements_AllowsSeveralVisibleElements()
        {
            RegisterPrefab("first", UIElementGroup.OverlayElement, UIElementHideBehavior.KeepInQueue);
            RegisterPrefab("second", UIElementGroup.OverlayElement, UIElementHideBehavior.KeepInQueue);

            var first = await _service.CreateAsync<TestUIElement>("first").AsTask();
            var second = await _service.CreateAsync<TestUIElement>("second").AsTask();

            await _service.ShowAsync(first).AsTask();
            await _service.ShowAsync(second).AsTask();

            Assert.That(first.IsVisible, Is.True);
            Assert.That(second.IsVisible, Is.True);
        }

        [Test]
        public void CreateAsync_ActivePrefab_RejectsItAndReleasesResource()
        {
            var prefab = RegisterPrefab(
                "active-prefab",
                UIElementGroup.FullScreen,
                UIElementHideBehavior.KeepInQueue);
            prefab.SetActive(true);

            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _service.CreateAsync<TestUIElement>("active-prefab").AsTask());
            Assert.That(_resourceLoader.GetReleaseCount(prefab), Is.EqualTo(1));
        }

        [Test]
        public async Task Dispose_WithCreatedElements_ReleasesEachResourceOnce()
        {
            var firstPrefab = RegisterPrefab(
                "first",
                UIElementGroup.FullScreen,
                UIElementHideBehavior.KeepInQueue);
            var secondPrefab = RegisterPrefab(
                "second",
                UIElementGroup.OverlayElement,
                UIElementHideBehavior.KeepInQueue);

            var first = await _service.CreateAsync<TestUIElement>("first").AsTask();
            var second = await _service.CreateAsync<TestUIElement>("second").AsTask();

            _service.Dispose();
            _service = null;

            Assert.That(first == null, Is.True);
            Assert.That(second == null, Is.True);
            Assert.That(_resourceLoader.GetReleaseCount(firstPrefab), Is.EqualTo(1));
            Assert.That(_resourceLoader.GetReleaseCount(secondPrefab), Is.EqualTo(1));
        }

        private GameObject RegisterPrefab(
            string resourceId,
            UIElementGroup group,
            UIElementHideBehavior hideBehavior)
        {
            var prefabObject = new GameObject(resourceId, typeof(RectTransform), typeof(TestUIElement));
            prefabObject.SetActive(false);
            _objects.Add(prefabObject);

            var element = prefabObject.GetComponent<TestUIElement>();
            element.Configure(new UIElementSettings(group, hideBehavior));
            _resourceLoader.Register(resourceId, prefabObject);
            return prefabObject;
        }

        private UICanvasContext CreateCanvasContext()
        {
            return new UICanvasContext(
                CreateParent(UIElementGroup.Background),
                CreateParent(UIElementGroup.FullScreen),
                CreateParent(UIElementGroup.Popup),
                CreateParent(UIElementGroup.OverlayElement),
                CreateParent(UIElementGroup.DynamicOverlayElement));
        }

        private RectTransform CreateParent(UIElementGroup group)
        {
            var parent = new GameObject(group.ToString(), typeof(RectTransform));
            _objects.Add(parent);
            return parent.GetComponent<RectTransform>();
        }

        private RectTransform GetGroupParent(UIElementGroup group)
        {
            for (var index = 0; index < _objects.Count; index++)
            {
                var candidate = _objects[index];
                if (candidate != null && candidate.name == group.ToString())
                    return candidate.GetComponent<RectTransform>();
            }

            throw new InvalidOperationException($"Parent for group '{group}' was not found.");
        }

        private sealed class FakeResourceLoader : IResourceLoader
        {
            private readonly Dictionary<string, GameObject> _resources = new();
            private readonly Dictionary<GameObject, int> _releaseCounts = new();

            public void Register(string resourceId, GameObject prefab)
            {
                _resources.Add(resourceId, prefab);
            }

            public int GetReleaseCount(GameObject prefab)
            {
                return _releaseCounts.TryGetValue(prefab, out var count) ? count : 0;
            }

            public Task PreloadInCacheAsync<TResource>(string resourceId, CancellationToken token)
            {
                token.ThrowIfCancellationRequested();
                return Task.CompletedTask;
            }

            public TResource LoadResource<TResource>(string resourceId)
            {
                return (TResource)(object)_resources[resourceId];
            }

            public void LoadResource<TResource>(
                string resourceId,
                Action<TResource> onResourceLoaded,
                CancellationToken token)
            {
                token.ThrowIfCancellationRequested();
                onResourceLoaded(LoadResource<TResource>(resourceId));
            }

            public Task<TResource> LoadResourceAsync<TResource>(string resourceId, CancellationToken token)
            {
                token.ThrowIfCancellationRequested();
                return Task.FromResult(LoadResource<TResource>(resourceId));
            }

            public Task<TComponent> LoadAndCreateAsync<TComponent, TParent>(
                string resourceId,
                TParent parent,
                CancellationToken token = default)
            {
                throw new NotSupportedException();
            }

            public void ReleaseResource<TResource>(TResource resource)
            {
                if (resource is not GameObject prefab)
                    throw new InvalidOperationException("The UI service must release the loaded prefab asset.");

                _releaseCounts.TryGetValue(prefab, out var count);
                _releaseCounts[prefab] = count + 1;
            }

            public void ReleaseAllResources()
            {
            }

            public void Dispose()
            {
            }
        }

        private interface ITestUIElement : IUIElement
        {
        }

        private sealed class TestUIElement : MonoBehaviour, ITestUIElement
        {
            [SerializeField]
            private UIElementSettings _settings;

            private bool _hidden;

            public UIElementSettings Settings => _settings;

            public int HideImmediatelyCount { get; private set; }

            public int ShowCount { get; private set; }

            public int HideCount { get; private set; }

            public bool IsVisible { get; private set; }

            public bool WasHiddenWhenEnabled { get; private set; }

            public void Configure(UIElementSettings settings)
            {
                _settings = settings;
            }

            public void HideImmediately()
            {
                HideImmediatelyCount++;
                _hidden = true;
                IsVisible = false;
            }

            public UniTask ShowAsync(CancellationToken token)
            {
                token.ThrowIfCancellationRequested();
                ShowCount++;
                _hidden = false;
                IsVisible = true;
                return UniTask.CompletedTask;
            }

            public UniTask HideAsync(CancellationToken token)
            {
                token.ThrowIfCancellationRequested();
                HideCount++;
                _hidden = true;
                IsVisible = false;
                return UniTask.CompletedTask;
            }

            private void OnEnable()
            {
                WasHiddenWhenEnabled = _hidden;
            }
        }
    }
}
