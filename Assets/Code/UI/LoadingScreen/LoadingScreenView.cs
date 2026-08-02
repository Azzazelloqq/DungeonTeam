using System.Threading;
using System.Threading.Tasks;
using Azzazelloqq.MVVM.ReactiveLibrary;
using Code.UIService;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Code.UI.LoadingScreen
{
    public sealed class LoadingScreenView : LoadingScreenViewBase, IUIElement
    {
        [SerializeField]
        private UIElementSettings _settings = new(UIElementGroup.FullScreen, UIElementHideBehavior.KeepInQueue);

        [SerializeField]
        private CanvasGroup _canvasGroup = null;

        [SerializeField]
        private Text _statusText = null;

        public UIElementSettings Settings => _settings;

        public void HideImmediately()
        {
            SetVisible(false);
        }

        public UniTask ShowAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            SetVisible(true);
            return UniTask.CompletedTask;
        }

        public UniTask HideAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            SetVisible(false);
            return UniTask.CompletedTask;
        }

        protected override void OnInitialize()
        {
            viewModel.StatusText.Subscribe(SetStatusText).AddTo(compositeDisposable);
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

        private void SetStatusText(string statusText)
        {
            _statusText.text = statusText;
        }

        private void SetVisible(bool isVisible)
        {
            _canvasGroup.alpha = isVisible ? 1f : 0f;
            _canvasGroup.interactable = isVisible;
            _canvasGroup.blocksRaycasts = isVisible;
        }
    }
}
