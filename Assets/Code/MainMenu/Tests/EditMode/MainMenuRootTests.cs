using System;
using System.Threading;
using System.Threading.Tasks;
using Azzazelloqq.MVVM.Core;
using Code.Addressables.Generated;
using Code.UI.MainMenu;
using Code.UIService;
using Cysharp.Threading.Tasks;
using DungeonTeam.Gameplay.DungeonRun.Application;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Code.MainMenu.Tests
{
    public sealed class MainMenuRootTests
    {
        [Test]
        public async Task InitializeAsync_LoadsBindsAndShowsMenu()
        {
            var gameObject = new GameObject("MainMenuView", typeof(FakeMainMenuView));
            var view = gameObject.GetComponent<FakeMainMenuView>();
            var uiService = new FakeUiService(view);
            var playRequestCount = 0;
            var quitRequestCount = 0;
            using var root = new MainMenuRoot(
                uiService,
                CreateTeamSetup(),
                _ => playRequestCount++,
                () => { },
                () => quitRequestCount++);

            try
            {
                await root.InitializeAsync(CancellationToken.None).AsTask();

                Assert.That(uiService.CreatedAddressableId, Is.EqualTo(AddressableIds.UI.WindowsMainMainMenuPrefab));
                Assert.That(uiService.HideOnCreate, Is.True);
                Assert.That(view.WasInitialized, Is.True);
                Assert.That(uiService.WasInitializedWhenShown, Is.True);

                var viewModel = (MainMenuViewModel)view.BoundViewModel;
                viewModel.PlayCommand.Execute();
                viewModel.RequestQuitCommand.Execute();
                viewModel.ConfirmQuitCommand.Execute();

                Assert.That(playRequestCount, Is.EqualTo(1));
                Assert.That(quitRequestCount, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public async Task Dispose_AfterInitialization_DisposesViewModelWithoutDestroyingServiceOwnedView()
        {
            var gameObject = new GameObject("MainMenuView", typeof(FakeMainMenuView));
            var view = gameObject.GetComponent<FakeMainMenuView>();
            var uiService = new FakeUiService(view);
            var root = new MainMenuRoot(
                uiService,
                CreateTeamSetup(),
                _ => { },
                () => { },
                () => { });

            try
            {
                await root.InitializeAsync(CancellationToken.None).AsTask();
                var viewModel = (MainMenuViewModel)view.BoundViewModel;

                root.Dispose();

                Assert.That(viewModel.IsDisposed, Is.True);
                Assert.That(view.IsDisposed, Is.False);
                Assert.That(view.gameObject, Is.Not.Null);
            }
            finally
            {
                root.Dispose();
                Object.DestroyImmediate(gameObject);
            }
        }

        private sealed class FakeUiService : IUiService
        {
            private readonly FakeMainMenuView _view;

            public FakeUiService(FakeMainMenuView view)
            {
                _view = view;
            }

            public string CreatedAddressableId { get; private set; }

            public bool HideOnCreate { get; private set; }

            public bool WasInitializedWhenShown { get; private set; }

            public UniTask<TUI> CreateAsync<TUI>(
                string addressableId,
                bool hideOnCreate = true,
                CancellationToken token = default)
                where TUI : class, IUIElement
            {
                token.ThrowIfCancellationRequested();
                CreatedAddressableId = addressableId;
                HideOnCreate = hideOnCreate;

                return UniTask.FromResult(_view as TUI);
            }

            public UniTask ShowAsync(IUIElement element, CancellationToken token = default)
            {
                token.ThrowIfCancellationRequested();
                WasInitializedWhenShown = _view.WasInitialized;
                return UniTask.CompletedTask;
            }

            public UniTask HideAsync(IUIElement element, CancellationToken token = default)
            {
                token.ThrowIfCancellationRequested();
                return UniTask.CompletedTask;
            }

            public UniTask CloseAsync(IUIElement element, CancellationToken token = default)
            {
                token.ThrowIfCancellationRequested();
                return UniTask.CompletedTask;
            }

            public void Dispose()
            {
            }
        }

        private static DungeonRunTeamSetup CreateTeamSetup()
        {
            return new DungeonRunTeamSetup(
                new[]
                {
                    new DungeonRunTeamMemberOption(
                        "actor.king",
                        "KING",
                        new[] { 1 },
                        new[] { "loadout.king" }),
                    new DungeonRunTeamMemberOption(
                        "actor.druid",
                        "DRUID",
                        new[] { 1 },
                        new[] { "loadout.druid" })
                },
                2,
                2,
                new DungeonRunTeamSelection(
                    new DungeonRunActorSelection("actor.king", 1, "loadout.king"),
                    new[]
                    {
                        new DungeonRunActorSelection("actor.druid", 1, "loadout.druid")
                    }));
        }

        private sealed class FakeMainMenuView : MainMenuViewBase
        {
            public override UIElementSettings Settings { get; } = new(
                UIElementGroup.FullScreen,
                UIElementHideBehavior.KeepInQueue);

            public IViewModel BoundViewModel { get; private set; }

            public bool WasInitialized { get; private set; }

            public override void HideImmediately()
            {
            }

            public override UniTask ShowAsync(CancellationToken token)
            {
                token.ThrowIfCancellationRequested();
                return UniTask.CompletedTask;
            }

            public override UniTask HideAsync(CancellationToken token)
            {
                token.ThrowIfCancellationRequested();
                return UniTask.CompletedTask;
            }

            protected override void OnInitialize()
            {
                BoundViewModel = viewModel;
                WasInitialized = true;
            }

            protected override ValueTask OnInitializeAsync(CancellationToken token)
            {
                return default;
            }

            protected override void OnDispose()
            {
            }

            protected override ValueTask OnDisposeAsync(CancellationToken token)
            {
                return default;
            }
        }
    }
}
