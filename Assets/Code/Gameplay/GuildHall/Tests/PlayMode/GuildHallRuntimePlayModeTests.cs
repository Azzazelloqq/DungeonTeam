using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using Code.Addressables.Generated;
using Cysharp.Threading.Tasks;
using DungeonTeam.Gameplay.ContextActions.Runtime;
using DungeonTeam.Gameplay.ContextActions.Runtime.Base;
using DungeonTeam.Gameplay.GuildHall.Application;
using DungeonTeam.Gameplay.AmbientNpc.Application;
using DungeonTeam.Gameplay.GuildHall.Runtime.Composition;
using DungeonTeam.Gameplay.GuildHall.Runtime.Input;
using DungeonTeam.Gameplay.GuildHall.Runtime.Interaction;
using DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.Gameplay.GuildHall;
using DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.Gameplay.GuildHall.Base;
using DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.UI.NoticeBoard;
using DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.UI.RunSummary;
using NUnit.Framework;
using ResourceLoader;
using ResourceLoader.AddressableResourceLoader;
using TickHandler;
using TickHandler.UnityTickHandler;
using UnityEngine;
using UnityEngine.TestTools;

namespace DungeonTeam.Gameplay.GuildHall.Tests.PlayMode
{
    public sealed class GuildHallRuntimePlayModeTests
    {
        private readonly System.Collections.Generic.List<GameObject> _objects = new();

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
        public IEnumerator Root_InitializeThenDispose_LoadsAndReleasesWorldExactlyOnce()
        {
            var prefab = CreateTestPrefab(Array.Empty<GuildHallInteractionPoint>());
            var loader = new FakeResourceLoader(prefab);
            var input = new FakeGuildHallInput();
            var tickHandler = CreateTickHandler();
            var root = new GuildHallRoot(
                new GuildHallWorldLoader(loader),
                CreateStartContext(),
                CreateCatalog(),
                tickHandler,
                input,
                _ => { },
                () => { },
                _ => GuildProfileEditResult.Accept(CreateProfile()));

            yield return root.InitializeAsync(CancellationToken.None).ToCoroutine();
            root.Dispose();
            root.Dispose();

            Assert.That(loader.LoadedId, Is.EqualTo(AddressableIds.GuildHall.GuildHallGraybox));
            Assert.That(loader.ReleaseCount, Is.EqualTo(1));
            Assert.That(input.EnableCount, Is.EqualTo(1));
            Assert.That(input.DisposeCount, Is.EqualTo(1));
            tickHandler.Dispose();
        }

        [UnityTest]
        public IEnumerator Reception_SummaryThenProfile_BlocksAndRestoresInput()
        {
            var prefab = CreateTestPrefab(Array.Empty<GuildHallInteractionPoint>());
            var resourceLoader = new FakeResourceLoader(prefab);
            var tickHandler = CreateTickHandler();
            var runSummary = CreateRunSummary();
            var input = new FakeGuildHallInput();
            var root = new GuildHallRoot(
                new GuildHallWorldLoader(resourceLoader),
                new GuildHallStartContext(
                    Array.Empty<AmbientNpcSnapshot>(),
                    Array.Empty<NoticeBoardOfferSnapshot>(),
                    null,
                    runSummary, CreateProfile()),
                CreateCatalog(),
                tickHandler,
                input,
                _ => { },
                () => { },
                _ => GuildProfileEditResult.Accept(CreateProfile()));

            yield return root.InitializeAsync(CancellationToken.None).ToCoroutine();
            root.HandleInteraction(new GuildHallInteractionRequest(
                "reception.main", GuildInteractionKind.Reception));
            Assert.That(root.IsWorldInputBlocked, Is.True);
            Assert.That(root.RunSummaryViewModel.IsVisible.Value, Is.True);
            root.RunSummaryViewModel.Close();
            Assert.That(root.IsWorldInputBlocked, Is.False);

            root.HandleInteraction(new GuildHallInteractionRequest(
                "reception.main", GuildInteractionKind.Reception));
            Assert.That(root.RunSummaryViewModel.IsVisible.Value, Is.False);
            Assert.That(root.GuildProfileViewModel.IsVisible.Value, Is.True);
            Assert.That(root.IsWorldInputBlocked, Is.True);
            root.GuildProfileViewModel.SelectHeroCommand.Execute("companion");
            Assert.That(root.GuildProfileViewModel.SelectedHero.Value.ActorId, Is.EqualTo("companion"));
            root.GuildProfileViewModel.Close();
            Assert.That(root.IsWorldInputBlocked, Is.False);

            root.Dispose(); root.Dispose();
            tickHandler.Dispose();
            resourceLoader.Dispose();
            yield return null;
        }

        [UnityTest]
        public IEnumerator AddressablePrefab_LoadAndRelease_HasValidRootBindings()
        {
            var resourceLoader = new AddressableResourceLoader();
            var loader = new GuildHallWorldLoader(resourceLoader);
            var loadTask = loader.LoadAsync(CancellationToken.None);
            while (!loadTask.IsCompleted)
            {
                yield return null;
            }

            if (loadTask.IsFaulted)
            {
                throw loadTask.Exception?.InnerException ?? loadTask.Exception;
            }

            var lease = loadTask.Result;
            try
            {
                lease.View.ValidateBindings();
                lease.Activate();
                var authoredKinds = new System.Collections.Generic.HashSet<GuildInteractionKind>();
                for (var index = 0; index < lease.View.InteractionPoints.Length; index++)
                {
                    authoredKinds.Add(lease.View.InteractionPoints[index].Kind);
                }

                foreach (GuildInteractionKind requiredKind in
                         Enum.GetValues(typeof(GuildInteractionKind)))
                {
                    Assert.That(authoredKinds, Does.Contain(requiredKind));
                }
            }
            finally
            {
                lease.Dispose();
                resourceLoader.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator AddressablePrefab_TwoNoticeBoardCycles_DoNotDuplicateCallbacksOrListeners()
        {
            var resourceLoader = new AddressableResourceLoader();
            var tickHandler = CreateTickHandler();
            var selectedIds = new System.Collections.Generic.List<string>();
            var sessionState = new GuildSessionState();
            var offers = new[]
            {
                CreateOffer("contract.cycle.0", true),
                CreateOffer("contract.cycle.1", false),
                CreateOffer("contract.cycle.2", true)
            };
            var root = new GuildHallRoot(
                new GuildHallWorldLoader(resourceLoader),
                new GuildHallStartContext(
                    Array.Empty<AmbientNpcSnapshot>(),
                    offers,
                    null,
                    null),
                CreateCatalog(),
                new AmbientNpcProfileCatalog(Array.Empty<AmbientNpcProfileSnapshot>()),
                new DialogueCatalog(Array.Empty<DialoguePoolSnapshot>()),
                tickHandler,
                new FakeGuildHallInput(),
                _ => { },
                () => { },
                contractId =>
                {
                    selectedIds.Add(contractId);
                    sessionState.SelectContract(contractId);
                });

            yield return root.InitializeAsync(CancellationToken.None).ToCoroutine();
            var board = root.NoticeBoardView as NoticeBoardView;
            Assert.That(board, Is.Not.Null);
            Assert.That(board.ItemCount, Is.EqualTo(offers.Length));

            root.HandleInteraction(new GuildHallInteractionRequest(
                "notice-board.test", GuildInteractionKind.NoticeBoard));
            Assert.That(root.IsWorldInputBlocked, Is.True);
            board.GetItem(0).SelectButton.onClick.Invoke();
            board.GetItem(1).SelectButton.onClick.Invoke();
            board.CloseButton.onClick.Invoke();
            Assert.That(root.IsWorldInputBlocked, Is.False);

            root.HandleInteraction(new GuildHallInteractionRequest(
                "notice-board.test", GuildInteractionKind.NoticeBoard));
            Assert.That(root.IsWorldInputBlocked, Is.True);
            board.GetItem(2).SelectButton.onClick.Invoke();
            board.CloseButton.onClick.Invoke();

            Assert.That(selectedIds, Is.EqualTo(new[] { "contract.cycle.0", "contract.cycle.2" }));
            Assert.That(sessionState.SelectedContractId, Is.EqualTo("contract.cycle.2"));
            Assert.That(root.IsWorldInputBlocked, Is.False);

            root.Dispose();
            tickHandler.Dispose();
            resourceLoader.Dispose();
            yield return null;
        }

        [UnityTest]
        public IEnumerator AddressableRoot_FrameTick_MovesAuthoredCharacterController()
        {
            var resourceLoader = new AddressableResourceLoader();
            var tickHandler = CreateTickHandler();
            var input = new FakeGuildHallInput { MovementValue = Vector2.up };
            var root = new GuildHallRoot(
                new GuildHallWorldLoader(resourceLoader),
                CreateStartContext(),
                CreateCatalog(),
                tickHandler,
                input,
                _ => { },
                () => { });

            yield return root.InitializeAsync(CancellationToken.None).ToCoroutine();
            var view = UnityEngine.Object.FindAnyObjectByType<GuildHallView>();
            Assert.That(view, Is.Not.Null);
            var startPosition = view.PlayerTransform.position;

            yield return null;
            yield return null;

            Assert.That(view.PlayerTransform.position.z, Is.GreaterThan(startPosition.z));
            view.Move(Vector3.forward * 20f);
            Assert.That(view.PlayerTransform.position.z, Is.LessThan(5.5f));
            root.Dispose();
            tickHandler.Dispose();
            resourceLoader.Dispose();
            yield return null;
        }

        [Test]
        public void LoadAsync_CancelledAfterResourceAcquired_ReleasesResource()
        {
            var prefab = CreateTestPrefab(Array.Empty<GuildHallInteractionPoint>());
            var loader = new FakeResourceLoader(prefab);
            var worldLoader = new GuildHallWorldLoader(loader);
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            Assert.CatchAsync<OperationCanceledException>(async () =>
                await worldLoader.LoadAsync(cancellation.Token));
            Assert.That(loader.ReleaseCount, Is.EqualTo(1));
        }

        [Test]
        public void Root_InitializationFailsBeforePresenter_DisposeReleasesOwnedInput()
        {
            var prefab = CreateTestPrefab(Array.Empty<GuildHallInteractionPoint>());
            prefab.gameObject.SetActive(true);
            var loader = new FakeResourceLoader(prefab);
            var input = new FakeGuildHallInput();
            var tickHandler = CreateTickHandler();
            var root = new GuildHallRoot(
                new GuildHallWorldLoader(loader),
                CreateStartContext(),
                CreateCatalog(),
                tickHandler,
                input,
                _ => { },
                () => { });

            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await root.InitializeAsync(CancellationToken.None));

            root.Dispose();

            Assert.That(loader.ReleaseCount, Is.EqualTo(1));
            Assert.That(input.EnableCount, Is.Zero);
            Assert.That(input.DisposeCount, Is.EqualTo(1));
            tickHandler.Dispose();
        }

        [UnityTest]
        public IEnumerator Presenter_FrameTick_MovesCameraRelativeAndStopsAfterDispose()
        {
            var viewObject = Track(new GameObject("GuildHallTestView"));
            var view = viewObject.AddComponent<TestGuildHallView>();
            view.Configure(null, Array.Empty<GuildHallInteractionPoint>());
            var input = new FakeGuildHallInput { MovementValue = Vector2.up };
            var tickHandler = CreateTickHandler();
            var actionsModel = new ContextActionsModel();
            var interactions = new GuildHallInteractionController(
                view.PlayerTransform,
                view.InteractionPoints,
                actionsModel,
                CreateCatalog(),
                _ => { },
                () => { });
            var presenter = new GuildHallPresenter(
                view,
                new GuildHallModel(),
                input,
                tickHandler,
                interactions,
                CreateCatalog().Movement);
            presenter.Initialize();

            yield return null;

            Assert.That(view.TotalMovement.z, Is.GreaterThan(0f));
            presenter.SetWorldInputBlocked(true);
            var movementWhileBlocked = view.TotalMovement;
            yield return null;
            Assert.That(view.TotalMovement, Is.EqualTo(movementWhileBlocked));

            presenter.SetWorldInputBlocked(false);
            yield return null;
            Assert.That(view.TotalMovement.z, Is.GreaterThan(movementWhileBlocked.z));
            presenter.Dispose();
            var movementAfterDispose = view.TotalMovement;

            yield return null;

            Assert.That(view.TotalMovement, Is.EqualTo(movementAfterDispose));
            actionsModel.Dispose();
            tickHandler.Dispose();
        }

        [Test]
        public void InteractionScan_MultiplePoints_SelectsNearestAvailablePoint()
        {
            var player = Track(new GameObject("Player"));
            var far = CreatePoint("board", GuildInteractionKind.NoticeBoard, new Vector3(2f, 0f, 0f), 3f);
            var near = CreatePoint("npc.registrar", GuildInteractionKind.Npc, new Vector3(1f, 0f, 0f), 3f);
            var model = new ContextActionsModel();
            GuildHallInteractionRequest executed = null;
            var controller = new GuildHallInteractionController(
                player.transform,
                new[] { far, near },
                model,
                CreateCatalog(),
                request => executed = request,
                () => { });

            controller.Tick(0.2f);
            model.Execute(0);

            Assert.That(model.Labels.Value, Is.EqualTo(new[] { "Поговорить" }));
            Assert.That(executed.SemanticId, Is.EqualTo("npc.registrar"));
            controller.Dispose();
            model.Dispose();
        }

        [Test]
        public void InteractionExecute_PlayerLeftRadius_DoesNotInvokeStaleAction()
        {
            var player = Track(new GameObject("Player"));
            var point = CreatePoint("reception", GuildInteractionKind.Reception, Vector3.right, 2f);
            var model = new ContextActionsModel();
            var executionCount = 0;
            var controller = new GuildHallInteractionController(
                player.transform,
                new[] { point },
                model,
                CreateCatalog(),
                _ => executionCount++,
                () => { });

            controller.Tick(0.2f);
            player.transform.position = Vector3.right * 10f;
            model.Execute(0);

            Assert.That(executionCount, Is.Zero);
            Assert.That(model.Labels.Value, Is.Empty);
            controller.Dispose();
            model.Dispose();
        }

        [Test]
        public void InteractionDispose_ClearsPublishedAction()
        {
            var player = Track(new GameObject("Player"));
            var point = CreatePoint("exit", GuildInteractionKind.Exit, Vector3.zero, 2f);
            var model = new ContextActionsModel();
            var controller = new GuildHallInteractionController(
                player.transform,
                new[] { point },
                model,
                CreateCatalog(),
                _ => { },
                () => { });
            controller.Tick(0.2f);

            controller.Dispose();

            Assert.That(model.Labels.Value, Is.Empty);
            model.Dispose();
        }

        [Test]
        public void ExitInteraction_ValidExecution_RequestsWorldMapOnly()
        {
            var player = Track(new GameObject("Player"));
            var point = CreatePoint("exit.main", GuildInteractionKind.Exit, Vector3.zero, 2f);
            var model = new ContextActionsModel();
            var interactionCount = 0;
            var worldMapCount = 0;
            var controller = new GuildHallInteractionController(
                player.transform,
                new[] { point },
                model,
                CreateCatalog(),
                _ => interactionCount++,
                () => worldMapCount++);
            controller.Tick(0.2f);

            model.Execute(0);

            Assert.That(worldMapCount, Is.EqualTo(1));
            Assert.That(interactionCount, Is.Zero);
            controller.Dispose();
            model.Dispose();
        }

        private TestGuildHallView CreateTestPrefab(
            GuildHallInteractionPoint[] interactionPoints)
        {
            var root = Track(new GameObject("GuildHallPrefab"));
            var view = root.AddComponent<TestGuildHallView>();
            var contextObject = new GameObject("ContextActions", typeof(RectTransform));
            contextObject.transform.SetParent(root.transform, false);
            var contextView = contextObject.AddComponent<TestContextActionsView>();
            var boardObject = new GameObject("NoticeBoard", typeof(RectTransform));
            boardObject.transform.SetParent(root.transform, false);
            var boardView = boardObject.AddComponent<TestNoticeBoardView>();
            var summaryView = new GameObject("Summary").AddComponent<TestRunSummaryView>();
            summaryView.transform.SetParent(root.transform, false);
            var profileView = new GameObject("Profile").AddComponent<TestGuildProfileView>();
            profileView.transform.SetParent(root.transform, false);
            view.Configure(contextView, boardView, summaryView, profileView, interactionPoints);
            root.SetActive(false);
            return view;
        }

        private GuildHallInteractionPoint CreatePoint(
            string semanticId,
            GuildInteractionKind kind,
            Vector3 position,
            float radius)
        {
            var pointObject = Track(new GameObject(semanticId));
            pointObject.transform.position = position;
            var point = pointObject.AddComponent<GuildHallInteractionPoint>();
            point.Configure(semanticId, kind, pointObject.transform, radius);
            return point;
        }

        private UnityTickHandler CreateTickHandler()
        {
            var dispatcherObject = Track(new GameObject("GuildHallTestDispatcher"));
            return new UnityTickHandler(dispatcherObject.AddComponent<UnityDispatcherBehaviour>());
        }

        private GameObject Track(GameObject gameObject)
        {
            _objects.Add(gameObject);
            return gameObject;
        }

        private static GuildHallStartContext CreateStartContext()
        {
            return new GuildHallStartContext(
                Array.Empty<AmbientNpcSnapshot>(),
                Array.Empty<NoticeBoardOfferSnapshot>(),
                null,
                null);
        }

        private static GuildRunSummarySnapshot CreateRunSummary()
        {
            var text = new GuildRunSummaryTextSnapshot(
                new GuildTextSnapshot("summary.header", "Итог"),
                new GuildTextSnapshot("summary.completed", "Завершено"),
                new GuildTextSnapshot("summary.defeated", "Поражение"),
                new GuildTextSnapshot("summary.dungeon", "Данж"),
                new GuildTextSnapshot("summary.rewards", "Награды"),
                "{0} x{1}",
                new GuildTextSnapshot("summary.empty", "Нет"),
                new GuildTextSnapshot("summary.close", "Закрыть"));
            return new GuildRunSummarySnapshot(
                text.CompletedOutcome,
                new GuildTextSnapshot("dungeon.test", "dungeon.test"),
                new[]
                {
                    new GuildTextSnapshot("reward.1", "Один x1"),
                    new GuildTextSnapshot("reward.2", "Два x2"),
                    new GuildTextSnapshot("reward.3", "Три x3")
                },
                text);
        }

        private static GuildProfileSnapshot CreateProfile()
        {
            var leader = new GuildHeroSnapshot(
                "leader", "Leader", GuildHeroRole.Leader, 1, 10, 2f,
                Array.Empty<GuildHeroSkillSnapshot>(), "leader.loadout",
                new[] { new GuildHeroLoadoutSnapshot("leader.loadout", "Leader loadout") });
            var companion = new GuildHeroSnapshot(
                "companion", "Companion", GuildHeroRole.Companion, 2, 12, 3f,
                Array.Empty<GuildHeroSkillSnapshot>(), "companion.loadout",
                new[] { new GuildHeroLoadoutSnapshot("companion.loadout", "Companion loadout") });
            return new GuildProfileSnapshot(
                5,
                "-",
                leader,
                new[] { companion },
                new[] { leader, companion },
                CreateProfileText());
        }

        private static NoticeBoardOfferSnapshot CreateOffer(string id, bool isAvailable)
        {
            return new NoticeBoardOfferSnapshot(
                id,
                new GuildTextSnapshot($"{id}.title", id),
                new GuildTextSnapshot($"{id}.summary", "Описание"),
                "location.test",
                isAvailable,
                isAvailable ? null : new GuildTextSnapshot($"{id}.reason", "Недоступно"));
        }

        private static GuildHallCatalog CreateCatalog()
        {
            return new GuildHallCatalog(
                new[]
                {
                    new AmbientNpcSnapshot(
                        "npc.registrar",
                        new AmbientTextSnapshot("npc.registrar.name", "Регистратор"),
                        "dialogue.registrar",
                        "ambient.stationary"),
                    new AmbientNpcSnapshot(
                        "npc.visitor",
                        new AmbientTextSnapshot("npc.visitor.name", "Гость"),
                        "dialogue.visitor",
                        "ambient.route"),
                    new AmbientNpcSnapshot(
                        "npc.debater",
                        new AmbientTextSnapshot("npc.debater.name", "Спорщик"),
                        "dialogue.debater",
                        "ambient.stationary")
                },
                new GuildHallMovementSettings(4f, 16f, 0.1f),
                new GuildInteractionLabels(
                    new GuildTextSnapshot("interaction.npc", "Поговорить"),
                    new GuildTextSnapshot("interaction.board", "Доска"),
                    new GuildTextSnapshot("interaction.reception", "Стойка"),
                    new GuildTextSnapshot("interaction.exit", "Выйти")),
                new NoticeBoardTextSnapshot(
                    new GuildTextSnapshot("notice.header", "Контракты"),
                    new GuildTextSnapshot("notice.select", "Выбрать"),
                    new GuildTextSnapshot("notice.selected", "Выбрано"),
                    new GuildTextSnapshot("notice.close", "Закрыть"),
                    new GuildTextSnapshot("notice.empty", "Нет контрактов")),
                new GuildRunSummaryTextSnapshot(
                    new GuildTextSnapshot("summary.header", "Итог"),
                    new GuildTextSnapshot("summary.completed", "Завершено"),
                    new GuildTextSnapshot("summary.defeated", "Поражение"),
                    new GuildTextSnapshot("summary.dungeon", "Данж"),
                    new GuildTextSnapshot("summary.rewards", "Награды"),
                    "{0} x{1}",
                    new GuildTextSnapshot("summary.empty", "Нет"),
                    new GuildTextSnapshot("summary.close", "Закрыть")),
                CreateProfileText());
        }

        private static GuildProfileTextSnapshot CreateProfileText() => new(
            Text("profile.header"),
            Text("profile.gold"),
            Text("profile.rank"),
            Text("profile.rank.unassigned"),
            Text("profile.leader"),
            Text("profile.leader.explanation"),
            Text("profile.team"),
            Text("profile.roster"),
            Text("profile.available"),
            Text("profile.level"),
            Text("profile.health"),
            Text("profile.speed"),
            Text("profile.skill.primary"),
            Text("profile.skill.active"),
            Text("profile.close"),
            Text("profile.make-leader"),
            Text("profile.add-companion"),
            Text("profile.remove-companion"),
            Text("profile.loadout"),
            Text("profile.rejection.team-size"),
            Text("profile.rejection.invalid-actor"),
            Text("profile.rejection.invalid-loadout"),
            Text("profile.rejection.persistence"));

        private static GuildTextSnapshot Text(string id) => new(id, id);

        private sealed class FakeGuildHallInput : IGuildHallInput
        {
            public Vector2 MovementValue { get; set; }
            public Vector2 Movement => MovementValue;
            public int EnableCount { get; private set; }
            public int DisposeCount { get; private set; }

            public void Enable()
            {
                EnableCount++;
            }

            public void Dispose()
            {
                DisposeCount++;
            }
        }

        private sealed class FakeResourceLoader : IResourceLoader
        {
            private readonly GameObject _prefab;

            public FakeResourceLoader(TestGuildHallView view)
            {
                _prefab = view.gameObject;
            }

            public string LoadedId { get; private set; }
            public int ReleaseCount { get; private set; }

            public Task PreloadInCacheAsync<TResource>(string resourceId, CancellationToken token)
            {
                throw new NotSupportedException();
            }

            public TResource LoadResource<TResource>(string resourceId)
            {
                throw new NotSupportedException();
            }

            public void LoadResource<TResource>(
                string resourceId,
                Action<TResource> onResourceLoaded,
                CancellationToken token)
            {
                throw new NotSupportedException();
            }

            public Task<TResource> LoadResourceAsync<TResource>(
                string resourceId,
                CancellationToken token)
            {
                LoadedId = resourceId;
                return Task.FromResult((TResource)(object)_prefab);
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
                ReleaseCount++;
            }

            public void ReleaseAllResources()
            {
            }

            public void Dispose()
            {
            }
        }

        private sealed class TestGuildHallView : GuildHallViewBase
        {
            [SerializeField]
            private ContextActionsViewBase _contextActionsView;

            [SerializeField]
            private GuildHallInteractionPoint[] _interactionPoints;

            [SerializeField]
            private NoticeBoardViewBase _noticeBoardView;
            [SerializeField] private RunSummaryViewBase _runSummaryView;
            [SerializeField] private DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.UI.GuildProfile.Base.GuildProfileViewBase _profileView;

            public Vector3 TotalMovement { get; private set; }

            public override Transform PlayerTransform => transform;
            public override Transform CameraTransform => transform;
            public override ContextActionsViewBase ContextActionsView => _contextActionsView;
            public override GuildHallInteractionPoint[] InteractionPoints => _interactionPoints;
            public override NoticeBoardViewBase NoticeBoardView => _noticeBoardView;
            public override RunSummaryViewBase RunSummaryView => _runSummaryView;
            public override DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.UI.GuildProfile.Base.GuildProfileViewBase GuildProfileView => _profileView;

            public void Configure(
                ContextActionsViewBase contextActionsView,
                NoticeBoardViewBase noticeBoardView,
                RunSummaryViewBase runSummaryView,
                DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.UI.GuildProfile.Base.GuildProfileViewBase profileView,
                GuildHallInteractionPoint[] interactionPoints)
            {
                _contextActionsView = contextActionsView;
                _noticeBoardView = noticeBoardView;
                _runSummaryView = runSummaryView;
                _profileView = profileView;
                _interactionPoints = interactionPoints;
            }

            public void Configure(
                ContextActionsViewBase contextActionsView,
                GuildHallInteractionPoint[] interactionPoints)
            {
                Configure(contextActionsView, null, null, null, interactionPoints);
            }

            public override void ValidateBindings()
            {
                if (_interactionPoints == null)
                {
                    throw new InvalidOperationException("Test interaction points are missing.");
                }
            }

            public override void ResetPlayer()
            {
            }

            public override void Move(Vector3 displacement)
            {
                TotalMovement += displacement;
                transform.position += displacement;
            }

            protected override void OnInitialize()
            {
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

        private sealed class TestNoticeBoardView : NoticeBoardViewBase
        {
            public override void ValidateBindings()
            {
            }

            protected override void OnInitialize()
            {
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

        private sealed class TestRunSummaryView : RunSummaryViewBase
        {
            public override void ValidateBindings() { }
            protected override void OnInitialize() { }
            protected override ValueTask OnInitializeAsync(CancellationToken token) => default;
            protected override void OnDispose() { }
            protected override ValueTask OnDisposeAsync(CancellationToken token) => default;
        }

        private sealed class TestGuildProfileView : DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.UI.GuildProfile.Base.GuildProfileViewBase
        {
            public override void ValidateBindings() { }
            protected override void OnInitialize() { }
            protected override ValueTask OnInitializeAsync(CancellationToken token) => default;
            protected override void OnDispose() { }
            protected override ValueTask OnDisposeAsync(CancellationToken token) => default;
        }

        private sealed class TestContextActionsView : ContextActionsViewBase
        {
            protected override void OnInitialize()
            {
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
