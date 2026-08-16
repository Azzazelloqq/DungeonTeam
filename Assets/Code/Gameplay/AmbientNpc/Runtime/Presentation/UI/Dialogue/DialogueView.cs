using System;
using System.Threading;
using System.Threading.Tasks;
using Azzazelloqq.MVVM.ReactiveLibrary;
using DungeonTeam.Gameplay.AmbientNpc.Runtime.Presentation.UI.Dialogue.Base;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DungeonTeam.Gameplay.AmbientNpc.Runtime.Presentation.UI.Dialogue
{
    public sealed class DialogueView : DialogueViewBase
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private TMP_Text _speakerText;
        [SerializeField] private TMP_Text _lineText;
        [SerializeField] private Button _closeButton;
        private UnityAction _closeRequested;

        public override void ValidateBindings()
        {
            if (_canvasGroup == null || _speakerText == null || _lineText == null || _closeButton == null)
            {
                throw new InvalidOperationException("Dialogue view requires canvas group, speaker, line and close button.");
            }
        }

        protected override void OnInitialize()
        {
            ValidateBindings();
            viewModel.Speaker.Subscribe(value => _speakerText.SetText(value)).AddTo(compositeDisposable);
            viewModel.Line.Subscribe(value => _lineText.SetText(value)).AddTo(compositeDisposable);
            viewModel.IsVisible.Subscribe(SetVisible).AddTo(compositeDisposable);
            _closeRequested = () => viewModel.CloseCommand.Execute(null);
            _closeButton.onClick.AddListener(_closeRequested);
        }

        protected override ValueTask OnInitializeAsync(CancellationToken token) => default;
        protected override void OnDispose()
        {
            if (_closeButton != null && _closeRequested != null)
            {
                _closeButton.onClick.RemoveListener(_closeRequested);
            }

            _closeRequested = null;
        }

        protected override ValueTask OnDisposeAsync(CancellationToken token)
        {
            OnDispose();
            return default;
        }

        private void SetVisible(bool isVisible)
        {
            _canvasGroup.alpha = isVisible ? 1f : 0f;
            _canvasGroup.interactable = isVisible;
            _canvasGroup.blocksRaycasts = isVisible;
        }
    }
}
