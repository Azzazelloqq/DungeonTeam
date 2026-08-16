using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Code.UIService;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using ResourceLoader.AddressableResourceLoader;
using UnityEngine;
using UnityEngine.TestTools;

namespace DungeonTeam.UI.WorldMap.Tests.PlayMode
{
    public sealed class WorldMapAddressablePlayModeTests
    {
        private readonly List<GameObject> _objects = new();

        [TearDown]
        public void TearDown()
        {
            for (var index = _objects.Count - 1; index >= 0; index--)
            {
                if (_objects[index] != null)
                {
                    UnityEngine.Object.DestroyImmediate(_objects[index]);
                }
            }

            _objects.Clear();
        }

        [UnityTest]
        public IEnumerator AddressableRoot_TwoCycles_CreateItemsAndReleaseView()
        {
            var resourceLoader = new AddressableResourceLoader();
            var uiService = new Code.UIService.UIService(resourceLoader, CreateCanvasContext());
            var selectedIds = new List<string>();

            try
            {
                for (var cycle = 0; cycle < 2; cycle++)
                {
                    var root = new WorldMapRoot(
                        uiService,
                        CreateContext(),
                        selectedIds.Add,
                        () => { });

                    yield return root.InitializeAsync(CancellationToken.None).ToCoroutine();
                    yield return root.ShowAsync(CancellationToken.None).ToCoroutine();

                    var view = UnityEngine.Object.FindAnyObjectByType<WorldMapView>();
                    Assert.That(view, Is.Not.Null);
                    Assert.That(
                        view.GetComponentsInChildren<WorldMapLocationItemView>(includeInactive: false),
                        Has.Length.EqualTo(3));

                    root.ViewModel.Items[cycle].Select();
                    yield return root.CloseAsync(CancellationToken.None).ToCoroutine();
                    root.Dispose();
                    yield return null;

                    Assert.That(UnityEngine.Object.FindAnyObjectByType<WorldMapView>(), Is.Null);
                }

                Assert.That(selectedIds, Is.EqualTo(new[] { "location.0", "location.1" }));
            }
            finally
            {
                uiService.Dispose();
                resourceLoader.Dispose();
            }
        }

        private UICanvasContext CreateCanvasContext()
        {
            return new UICanvasContext(
                CreateParent("Background"),
                CreateParent("FullScreen"),
                CreateParent("Popup"),
                CreateParent("Overlay"),
                CreateParent("DynamicOverlay"));
        }

        private RectTransform CreateParent(string name)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            _objects.Add(gameObject);
            return gameObject.GetComponent<RectTransform>();
        }

        private static WorldMapStartContext CreateContext()
        {
            var locations = new WorldLocationSnapshot[3];
            for (var index = 0; index < locations.Length; index++)
            {
                locations[index] = new WorldLocationSnapshot(
                    $"location.{index}",
                    Text($"location.{index}.title"),
                    Text($"location.{index}.description"),
                    true,
                    null,
                    WorldLocationDestinationKind.GuildHall,
                    null);
            }

            return new WorldMapStartContext(
                locations,
                new WorldMapUiTextSnapshot(
                    Text("world-map.title"),
                    Text("world-map.back"),
                    Text("world-map.empty")));
        }

        private static WorldMapTextSnapshot Text(string id)
        {
            return new WorldMapTextSnapshot(id, id);
        }
    }
}
