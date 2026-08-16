using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DungeonTeam.Gameplay.ContextActions.Runtime;
using DungeonTeam.Gameplay.ContextActions.Runtime.Base;
using DungeonTeam.Gameplay.GuildHall.Application;
using DungeonTeam.Gameplay.GuildHall.Runtime.Input;
using DungeonTeam.Gameplay.GuildHall.Runtime.Interaction;
using DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.Gameplay.GuildHall;
using DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.Gameplay.GuildHall.Base;
using DungeonTeam.Gameplay.AmbientNpc.Application;
using DungeonTeam.Gameplay.AmbientNpc.Runtime;
using DungeonTeam.Gameplay.AmbientNpc.Runtime.Presentation.UI.Dialogue;
using DungeonTeam.Gameplay.AmbientNpc.Runtime.Presentation.UI.Dialogue.Base;
using DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.UI.NoticeBoard;
using DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.UI.RunSummary;
using DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.UI.GuildProfile;
using DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.UI.GuildProfile.Base;
using RootPattern;
using TickHandler;

namespace DungeonTeam.Gameplay.GuildHall.Runtime.Composition
{
    public sealed class GuildHallRoot : Root
    {
        private readonly GuildHallWorldLoader _worldLoader;
        private readonly GuildHallStartContext _startContext;
        private readonly GuildHallCatalog _catalog;
        private readonly AmbientNpcProfileCatalog _ambientProfiles;
        private readonly DialogueCatalog _dialogues;
        private readonly ITickHandler _tickHandler;
        private readonly Action<GuildHallInteractionRequest> _interactionRequested;
        private readonly Action _worldMapRequested;
        private readonly Action<string> _contractSelected;
        private readonly Func<GuildProfileEditRequest, GuildProfileEditResult> _profileEditRequested;

        private IGuildHallInput _pendingInput;
        private GuildHallWorldLease _worldLease;
        private GuildHallPresenter _presenter;
        private GuildHallModel _pendingModel;
        private GuildHallInteractionController _interactionController;
        private ContextActionsViewModel _contextActionsViewModel;
        private ContextActionsViewBase _contextActionsView;
        private AmbientNpcSet _ambientNpcSet;
        private DialogueViewBase _dialogueView;
        private DialogueModel _dialogueModel;
        private DialogueViewModel _dialogueViewModel;
        private readonly DialogueLineSelector _dialogueLineSelector = new(new Random());
        private string _activeDialogueNpcId;
        private NoticeBoardViewBase _noticeBoardView;
        private NoticeBoardModel _noticeBoardModel;
        private NoticeBoardViewModel _noticeBoardViewModel;
        private RunSummaryViewBase _runSummaryView;
        private RunSummaryModel _runSummaryModel;
        private RunSummaryViewModel _runSummaryViewModel;
        private bool _runSummaryViewed;
        private GuildProfileViewBase _guildProfileView;
        private GuildProfileModel _guildProfileModel;
        private GuildProfileViewModel _guildProfileViewModel;

        public GuildHallRoot(
            GuildHallWorldLoader worldLoader,
            GuildHallStartContext startContext,
            GuildHallCatalog catalog,
            ITickHandler tickHandler,
            IGuildHallInput input,
            Action<GuildHallInteractionRequest> interactionRequested,
            Action worldMapRequested,
            Func<GuildProfileEditRequest, GuildProfileEditResult> profileEditRequested = null)
            : this(
                worldLoader,
                startContext,
                catalog,
                new AmbientNpcProfileCatalog(Array.Empty<AmbientNpcProfileSnapshot>()),
                new DialogueCatalog(Array.Empty<DialoguePoolSnapshot>()),
                tickHandler,
                input,
                interactionRequested,
                worldMapRequested,
                null,
                profileEditRequested)
        {
        }

        public GuildHallRoot(
            GuildHallWorldLoader worldLoader,
            GuildHallStartContext startContext,
            GuildHallCatalog catalog,
            AmbientNpcProfileCatalog ambientProfiles,
            DialogueCatalog dialogues,
            ITickHandler tickHandler,
            IGuildHallInput input,
            Action<GuildHallInteractionRequest> interactionRequested,
            Action worldMapRequested,
            Action<string> contractSelected = null,
            Func<GuildProfileEditRequest, GuildProfileEditResult> profileEditRequested = null)
        {
            _worldLoader = worldLoader ?? throw new ArgumentNullException(nameof(worldLoader));
            _startContext = startContext ?? throw new ArgumentNullException(nameof(startContext));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _ambientProfiles = ambientProfiles ?? throw new ArgumentNullException(nameof(ambientProfiles));
            _dialogues = dialogues ?? throw new ArgumentNullException(nameof(dialogues));
            _tickHandler = tickHandler ?? throw new ArgumentNullException(nameof(tickHandler));
            _pendingInput = input ?? throw new ArgumentNullException(nameof(input));
            _interactionRequested = interactionRequested ??
                throw new ArgumentNullException(nameof(interactionRequested));
            _worldMapRequested = worldMapRequested ?? throw new ArgumentNullException(
                nameof(worldMapRequested));
            _contractSelected = contractSelected ?? (_ => { });
            _profileEditRequested = profileEditRequested;
        }

        internal NoticeBoardViewBase NoticeBoardView => _noticeBoardView;
        internal NoticeBoardViewModel NoticeBoardViewModel => _noticeBoardViewModel;
        internal RunSummaryViewBase RunSummaryView => _runSummaryView;
        internal RunSummaryViewModel RunSummaryViewModel => _runSummaryViewModel;
        internal GuildProfileViewModel GuildProfileViewModel => _guildProfileViewModel;
        internal bool IsWorldInputBlocked => _pendingModel?.IsWorldInputBlocked ??
            _presenter?.IsWorldInputBlocked ?? false;

        public void SetWorldInputBlocked(bool isBlocked)
        {
            _presenter?.SetWorldInputBlocked(isBlocked);
        }

        protected override async UniTask OnInitializeAsync(CancellationToken token)
        {
            try
            {
                ValidateStartContext();
                _worldLease = await _worldLoader.LoadAsync(token);
                token.ThrowIfCancellationRequested();

                var view = _worldLease.View;
                view.ValidateBindings();

                _noticeBoardView = view.NoticeBoardView ?? throw new InvalidOperationException(
                    "Guild Hall prefab has no Notice Board view binding.");
                _noticeBoardModel = new NoticeBoardModel(
                    _startContext.Offers,
                    _startContext.SelectedContractId,
                    _catalog.NoticeBoardText);
                _noticeBoardViewModel = new NoticeBoardViewModel(
                    _noticeBoardModel,
                    HandleContractSelected,
                    CloseNoticeBoard);
                _noticeBoardViewModel.Initialize();
                _noticeBoardView.Initialize(_noticeBoardViewModel, disposeWithViewModel: false);

                if (_startContext.LastRunSummary != null)
                {
                    _runSummaryView = view.RunSummaryView ?? throw new InvalidOperationException(
                        "Guild Hall prefab has no Run Summary view binding.");
                    _runSummaryModel = new RunSummaryModel(_startContext.LastRunSummary);
                    _runSummaryViewModel = new RunSummaryViewModel(_runSummaryModel, CloseRunSummary);
                    _runSummaryViewModel.Initialize();
                    _runSummaryView.Initialize(_runSummaryViewModel, disposeWithViewModel: false);
                }
                if (_startContext.Profile != null)
                {
                    _guildProfileView = view.GuildProfileView ?? throw new InvalidOperationException(
                        "Guild Hall prefab has no Guild Profile view binding.");
                    _guildProfileModel = new GuildProfileModel(_startContext.Profile);
                    _guildProfileViewModel = new GuildProfileViewModel(
                        _guildProfileModel,
                        CloseGuildProfile,
                        _profileEditRequested ?? throw new InvalidOperationException(
                            "Guild Profile editing callback is required when a profile is present."));
                    _guildProfileViewModel.Initialize();
                    _guildProfileView.Initialize(
                        _guildProfileViewModel,
                        disposeWithViewModel: false);
                }

                if (_startContext.Npcs.Count > 0)
                {
                    _ambientNpcSet = new AmbientNpcSet(
                        _startContext.Npcs,
                        _ambientProfiles,
                        view.AmbientNpcViews,
                        view.AmbientNpcVignettes);
                    _ambientNpcSet.Initialize();
                    _dialogueView = view.DialogueView ?? throw new InvalidOperationException(
                        "Guild Hall prefab has no Dialogue view binding.");
                    _dialogueView.ValidateBindings();
                    _dialogueModel = new DialogueModel();
                    _dialogueViewModel = new DialogueViewModel(_dialogueModel, CloseDialogue);
                    _dialogueViewModel.Initialize();
                    _dialogueView.Initialize(_dialogueViewModel, disposeWithViewModel: false);
                }

                var contextActionsModel = new ContextActionsModel();
                _contextActionsViewModel = new ContextActionsViewModel(contextActionsModel);
                _contextActionsViewModel.Initialize();
                _contextActionsView = view.ContextActionsView ?? throw new InvalidOperationException(
                    "Guild Hall prefab has no ContextActions view binding.");
                _contextActionsView.Initialize(
                    _contextActionsViewModel,
                    disposeWithViewModel: false);

                _pendingModel = new GuildHallModel();
                _interactionController = new GuildHallInteractionController(
                    view.PlayerTransform,
                    view.InteractionPoints,
                    contextActionsModel,
                    _catalog,
                    HandleInteraction,
                    _worldMapRequested,
                    _pendingModel.SetCurrentInteraction);
                _presenter = new GuildHallPresenter(
                    view,
                    _pendingModel,
                    _pendingInput,
                    _tickHandler,
                    _interactionController,
                    _catalog.Movement,
                    _ambientNpcSet);
                _pendingInput = null;
                _pendingModel = null;
                _presenter.Initialize();
                _worldLease.Activate();
            }
            catch
            {
                OnDispose();
                throw;
            }
        }

        protected override void OnDispose()
        {
            CloseNoticeBoard();
            _noticeBoardView?.Dispose();
            _noticeBoardView = null;

            _noticeBoardViewModel?.Dispose();
            _noticeBoardViewModel = null;
            _noticeBoardModel = null;

            CloseRunSummary();
            _runSummaryView?.Dispose();
            _runSummaryView = null;
            _runSummaryViewModel?.Dispose();
            _runSummaryViewModel = null;
            _runSummaryModel = null;
            CloseGuildProfile();
            _guildProfileView?.Dispose();
            _guildProfileView = null;
            _guildProfileViewModel?.Dispose();
            _guildProfileViewModel = null;
            _guildProfileModel = null;

            CloseDialogue();
            _dialogueView?.Dispose();
            _dialogueView = null;

            _dialogueViewModel?.Dispose();
            _dialogueViewModel = null;
            _dialogueModel = null;

            _presenter?.Dispose();
            _presenter = null;

            _pendingInput?.Dispose();
            _pendingInput = null;

            _interactionController?.Dispose();
            _interactionController = null;

            _contextActionsView?.Dispose();
            _contextActionsView = null;

            _contextActionsViewModel?.Dispose();
            _contextActionsViewModel = null;

            _ambientNpcSet?.Dispose();
            _ambientNpcSet = null;

            _pendingModel?.Dispose();
            _pendingModel = null;

            _worldLease?.Dispose();
            _worldLease = null;
        }

        private void ValidateStartContext()
        {
            for (var index = 0; index < _startContext.Npcs.Count; index++)
            {
                _catalog.RequireNpc(_startContext.Npcs[index].NpcId);
                _ambientProfiles.Require(_startContext.Npcs[index].AmbientProfileId);
                _dialogues.Require(_startContext.Npcs[index].DialoguePoolId);
            }
        }

        internal void HandleInteraction(GuildHallInteractionRequest request)
        {
            if (request.Kind == GuildInteractionKind.NoticeBoard)
            {
                OpenNoticeBoard();
                return;
            }

            if (request.Kind == GuildInteractionKind.Reception &&
                _runSummaryViewModel != null &&
                !_runSummaryViewed)
            {
                OpenRunSummary();
                return;
            }
            if (request.Kind == GuildInteractionKind.Reception &&
                _guildProfileViewModel != null)
            {
                OpenGuildProfile();
                return;
            }

            if (request.Kind != GuildInteractionKind.Npc)
            {
                _interactionRequested(request);
                return;
            }

            var npc = _catalog.RequireNpc(request.SemanticId);
            OpenDialogue(npc);
        }

        private void OpenDialogue(AmbientNpcSnapshot npc)
        {
            if (_noticeBoardModel?.IsVisible.Value == true ||
                _runSummaryModel?.IsVisible.Value == true ||
                _guildProfileModel?.IsVisible.Value == true)
            {
                return;
            }

            if (_ambientNpcSet == null || _dialogueView == null)
            {
                throw new InvalidOperationException("Ambient NPC dialogue is not initialized.");
            }

            CloseDialogue();
            var line = _dialogueLineSelector.Select(_dialogues.Require(npc.DialoguePoolId));
            _activeDialogueNpcId = npc.NpcId;
            _ambientNpcSet.PauseAndFace(npc.NpcId, _worldLease.View.PlayerTransform.position);
            SetWorldInputBlocked(true);
            _dialogueModel.Show(npc.DisplayName.DisplayText, line.DisplayText);
        }

        internal void CloseDialogue()
        {
            var activeNpcId = _activeDialogueNpcId;
            _activeDialogueNpcId = null;
            _dialogueModel?.Hide();
            if (activeNpcId != null)
            {
                _ambientNpcSet?.ResumeRoutine(activeNpcId);
                SetWorldInputBlocked(false);
            }
        }

        private void OpenNoticeBoard()
        {
            if (_noticeBoardModel == null || _noticeBoardModel.IsVisible.Value ||
                _activeDialogueNpcId != null ||
                _runSummaryModel?.IsVisible.Value == true ||
                _guildProfileModel?.IsVisible.Value == true)
            {
                return;
            }

            _interactionController?.SetBlocked(true);
            SetWorldInputBlocked(true);
            _noticeBoardModel.Show();
        }

        private void CloseNoticeBoard()
        {
            if (_noticeBoardModel?.IsVisible.Value != true)
            {
                return;
            }

            _noticeBoardModel.Hide();
            SetWorldInputBlocked(false);
        }

        private void HandleContractSelected(string contractId)
        {
            _contractSelected(contractId);
        }

        private void OpenRunSummary()
        {
            if (_runSummaryModel == null || _runSummaryModel.IsVisible.Value ||
                _noticeBoardModel?.IsVisible.Value == true ||
                _activeDialogueNpcId != null ||
                _guildProfileModel?.IsVisible.Value == true)
            {
                return;
            }

            _interactionController?.SetBlocked(true);
            SetWorldInputBlocked(true);
            _runSummaryViewModel.Open();
        }

        private void CloseRunSummary()
        {
            if (_runSummaryModel?.IsVisible.Value != true)
            {
                return;
            }

            _runSummaryModel.Hide();
            _runSummaryViewed = true;
            SetWorldInputBlocked(false);
        }

        private void OpenGuildProfile()
        {
            if (_guildProfileModel == null ||
                _guildProfileModel.IsVisible.Value ||
                _noticeBoardModel?.IsVisible.Value == true ||
                _activeDialogueNpcId != null ||
                _runSummaryModel?.IsVisible.Value == true)
            {
                return;
            }

            _interactionController?.SetBlocked(true);
            SetWorldInputBlocked(true);
            _guildProfileViewModel.Open();
        }

        private void CloseGuildProfile()
        {
            if (_guildProfileModel?.IsVisible.Value != true)
            {
                return;
            }

            _guildProfileModel.Hide();
            SetWorldInputBlocked(false);
        }
    }
}
