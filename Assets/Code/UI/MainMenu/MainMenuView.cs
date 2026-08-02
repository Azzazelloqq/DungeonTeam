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

        [SerializeField]
        private Button _playButton = null;

        [SerializeField]
        private Button _quitButton = null;

        [SerializeField]
        private Button _confirmQuitButton = null;

        [SerializeField]
        private Button _cancelQuitButton = null;

        [SerializeField]
        private GameObject _quitConfirmation = null;

        public override UIElementSettings Settings => _settings;

        public override void HideImmediately()
        {
            SetVisible(false);
        }

        public override UniTask ShowAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            SetVisible(true);
            return UniTask.CompletedTask;
        }

        public override UniTask HideAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            SetVisible(false);
            return UniTask.CompletedTask;
        }

        protected override void OnInitialize()
        {
            _playButton.onClick.AddListener(OnPlayClicked);
            _quitButton.onClick.AddListener(OnQuitClicked);
            _confirmQuitButton.onClick.AddListener(OnConfirmQuitClicked);
            _cancelQuitButton.onClick.AddListener(OnCancelQuitClicked);
            viewModel.IsQuitConfirmationVisible.Subscribe(SetQuitConfirmationVisible).AddTo(compositeDisposable);
        }

        protected override ValueTask OnInitializeAsync(CancellationToken token)
        {
            return default;
        }

        protected override void OnDispose()
        {
            _playButton.onClick.RemoveListener(OnPlayClicked);
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
            _playButton.interactable = !isVisible;
            _quitButton.interactable = !isVisible;
        }

        private void SetVisible(bool isVisible)
        {
            _canvasGroup.alpha = isVisible ? 1f : 0f;
            _canvasGroup.interactable = isVisible;
            _canvasGroup.blocksRaycasts = isVisible;
        }

        private void OnPlayClicked()
        {
            viewModel.PlayCommand.Execute();
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
    }
}
