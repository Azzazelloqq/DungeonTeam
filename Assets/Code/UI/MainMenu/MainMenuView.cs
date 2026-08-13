using System.Threading;
using System.Threading.Tasks;
using Azzazelloqq.MVVM.ReactiveLibrary;
using Code.UIService;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Code.UI.MainMenu
{
    public sealed class MainMenuView : MainMenuViewBase, IUIElement
    {
        [SerializeField]
        private UIElementSettings _settings = new(UIElementGroup.FullScreen, UIElementHideBehavior.KeepInQueue);

        [SerializeField]
        private CanvasGroup _canvasGroup = null;

        [SerializeField, Min(0.01f)]
        private float _visibilityTransitionDuration = 0.2f;

        [SerializeField]
        private GameObject _background = null;

        [SerializeField]
        private GameObject _selectionPanel = null;

        [SerializeField]
        private Button _playButton = null;

        [SerializeField]
        private Button _selectDungeonButton = null;

        [SerializeField]
        private Text _selectedDungeonLabel = null;

        [SerializeField]
        private Button _decreaseSeedButton = null;

        [SerializeField]
        private Button _increaseSeedButton = null;

        [SerializeField]
        private Text _seedLabel = null;

        [SerializeField]
        private RectTransform _teamMembersParent = null;

        [SerializeField]
        private Text _teamSummary = null;

        [SerializeField]
        private Button _quitButton = null;

        [SerializeField]
        private Button _confirmQuitButton = null;

        [SerializeField]
        private Button _cancelQuitButton = null;

        [SerializeField]
        private GameObject _quitConfirmation = null;

        [SerializeField]
        private GameObject _previewPanel = null;

        [SerializeField]
        private Text _previewSummary = null;

        [SerializeField]
        private Button _backButton = null;

        private bool _canPlay;
        private bool _isQuitConfirmationVisibleState;

        public override UIElementSettings Settings => _settings;

        public override void HideImmediately()
        {
            SetVisible(false);
        }

        public override async UniTask ShowAsync(CancellationToken token)
        {
            SetInputEnabled(false);
            try
            {
                await CanvasGroupTween.FadeAsync(
                    _canvasGroup,
                    1f,
                    _visibilityTransitionDuration,
                    token);
                SetInputEnabled(true);
            }
            catch
            {
                SetVisible(false);
                throw;
            }
        }

        public override async UniTask HideAsync(CancellationToken token)
        {
            SetInputEnabled(false);
            try
            {
                await CanvasGroupTween.FadeAsync(
                    _canvasGroup,
                    0f,
                    _visibilityTransitionDuration,
                    token);
            }
            catch
            {
                SetVisible(true);
                throw;
            }
        }

        protected override void OnInitialize()
        {
            _playButton.onClick.AddListener(OnPlayClicked);
            _backButton.onClick.AddListener(OnBackClicked);
            _quitButton.onClick.AddListener(OnQuitClicked);
            _confirmQuitButton.onClick.AddListener(OnConfirmQuitClicked);
            _cancelQuitButton.onClick.AddListener(OnCancelQuitClicked);
            viewModel.IsQuitConfirmationVisible.Subscribe(SetQuitConfirmationVisible).AddTo(compositeDisposable);
            viewModel.IsPreviewVisible.Subscribe(SetPreviewVisible).AddTo(compositeDisposable);
            viewModel.PreviewSummary.Subscribe(SetPreviewSummary).AddTo(compositeDisposable);
            viewModel.CanPlay.Subscribe(SetCanPlay).AddTo(compositeDisposable);
            HideTechnicalControls();
        }

        protected override ValueTask OnInitializeAsync(CancellationToken token)
        {
            return default;
        }

        protected override void OnDispose()
        {
            _playButton.onClick.RemoveListener(OnPlayClicked);
            _backButton.onClick.RemoveListener(OnBackClicked);
            _quitButton.onClick.RemoveListener(OnQuitClicked);
            _confirmQuitButton.onClick.RemoveListener(OnConfirmQuitClicked);
            _cancelQuitButton.onClick.RemoveListener(OnCancelQuitClicked);
        }

        protected override ValueTask OnDisposeAsync(CancellationToken token)
        {
            return default;
        }

        private void SetQuitConfirmationVisible(bool isVisible)
        {
            _quitConfirmation.SetActive(isVisible);
            _isQuitConfirmationVisibleState = isVisible;
            UpdatePlayInteractable();
            _quitButton.interactable = !isVisible;
        }

        private void SetVisible(bool isVisible)
        {
            CanvasGroupTween.Kill(_canvasGroup);
            _canvasGroup.alpha = isVisible ? 1f : 0f;
            SetInputEnabled(isVisible);
        }

        private void SetInputEnabled(bool isEnabled)
        {
            _canvasGroup.interactable = isEnabled;
            _canvasGroup.blocksRaycasts = isEnabled;
        }

        private void SetPreviewVisible(bool isVisible)
        {
            _background.SetActive(!isVisible);
            _selectionPanel.SetActive(!isVisible);
            _previewPanel.SetActive(isVisible);
        }

        private void SetPreviewSummary(string summary)
        {
            _previewSummary.text = summary;
        }

        private void SetCanPlay(bool canPlay)
        {
            _canPlay = canPlay;
            UpdatePlayInteractable();
        }

        private void UpdatePlayInteractable()
        {
            _playButton.interactable = _canPlay && !_isQuitConfirmationVisibleState;
        }

        private void OnPlayClicked()
        {
            viewModel.PlayCommand.Execute();
        }

        private void OnBackClicked()
        {
            viewModel.BackCommand.Execute();
        }

        private void OnQuitClicked()
        {
            viewModel.RequestQuitCommand.Execute();
        }

        private void OnConfirmQuitClicked()
        {
            viewModel.ConfirmQuitCommand.Execute();
        }

        private void OnCancelQuitClicked()
        {
            viewModel.CancelQuitCommand.Execute();
        }

        private void HideTechnicalControls()
        {
            SetInactive(_selectDungeonButton);
            SetInactive(_selectedDungeonLabel);
            SetInactive(_decreaseSeedButton);
            SetInactive(_increaseSeedButton);
            SetInactive(_seedLabel);
            SetInactive(_teamMembersParent);
            SetInactive(_teamSummary);
        }

        private static void SetInactive(Component component)
        {
            if (component != null)
            {
                component.gameObject.SetActive(false);
            }
        }
    }
}
