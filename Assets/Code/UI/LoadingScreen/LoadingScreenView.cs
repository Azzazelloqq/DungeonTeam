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

        [SerializeField, Min(0.01f)]
        private float _visibilityTransitionDuration = 0.2f;

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
            CanvasGroupTween.Kill(_canvasGroup);
            _canvasGroup.alpha = isVisible ? 1f : 0f;
            SetInputEnabled(isVisible);
        }

        private void SetInputEnabled(bool isEnabled)
        {
            _canvasGroup.interactable = isEnabled;
            _canvasGroup.blocksRaycasts = isEnabled;
        }
    }
}
