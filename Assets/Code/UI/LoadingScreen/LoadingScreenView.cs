using System.Threading;
using System.Threading.Tasks;
using Azzazelloqq.MVVM.ReactiveLibrary;
using Code.UIService;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Code.UI.LoadingScreen
{
    public sealed class LoadingScreenView : LoadingScreenViewBase, IUIElement
    {
        [SerializeField]
        private UIElementSettings _settings = new(UIElementGroup.FullScreen, UIElementHideBehavior.KeepInQueue);

        [SerializeField]
        private CanvasGroup _canvasGroup = null;

        [SerializeField]
        private TMP_Text _statusText = null;

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
