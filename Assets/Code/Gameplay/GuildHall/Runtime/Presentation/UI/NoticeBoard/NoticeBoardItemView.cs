using System;
using Azzazelloqq.MVVM.ReactiveLibrary;
using DungeonTeam.Gameplay.GuildHall.Application;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.UI.NoticeBoard
{
    public sealed class NoticeBoardItemView : MonoBehaviour, IDisposable
    {
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _summaryText;
        [SerializeField] private TMP_Text _disabledReasonText;
        [SerializeField] private TMP_Text _selectLabel;
        [SerializeField] private GameObject _selectedMarker;
        [SerializeField] private Button _selectButton;

        private readonly Disposable.CompositeDisposable _subscriptions = new();
        private UnityAction _selectRequested;
        private NoticeBoardTextSnapshot _text;
        private bool _isAvailable;
        private bool _isCompleted;
        private string _statusText;

        internal Button SelectButton => _selectButton;

        public void Initialize(NoticeBoardItemViewModel viewModel, NoticeBoardTextSnapshot text)
        {
            if (viewModel == null)
            {
                throw new ArgumentNullException(nameof(viewModel));
            }

            _text = text ?? throw new ArgumentNullException(nameof(text));
            ValidateBindings();
            _isAvailable = viewModel.CanAccept;
            _isCompleted = viewModel.IsCompleted;
            _statusText = viewModel.StatusText;
            _titleText.SetText(viewModel.Title);
            _summaryText.SetText(viewModel.Summary);
            _disabledReasonText.SetText(
                _isCompleted || viewModel.IsActive ? viewModel.StatusText : viewModel.DisabledReason);
            _disabledReasonText.gameObject.SetActive(!_isAvailable);
            _selectButton.interactable = _isAvailable;
            _selectRequested = () => viewModel.SelectCommand.Execute(null);
            _selectButton.onClick.AddListener(_selectRequested);
            viewModel.IsSelected.Subscribe(SetSelected).AddTo(_subscriptions);
        }

        public void Dispose()
        {
            if (_selectButton != null && _selectRequested != null)
            {
                _selectButton.onClick.RemoveListener(_selectRequested);
            }

            _selectRequested = null;
            _subscriptions.Dispose();
        }

        private void ValidateBindings()
        {
            if (_titleText == null || _summaryText == null || _disabledReasonText == null ||
                _selectLabel == null || _selectedMarker == null || _selectButton == null)
            {
                throw new InvalidOperationException(
                    "Notice Board item view requires all text, marker and button bindings.");
            }
        }

        private void SetSelected(bool isSelected)
        {
            _selectedMarker.SetActive(isSelected);
            _selectLabel.SetText(
                _isCompleted || !string.IsNullOrWhiteSpace(_statusText)
                    ? _statusText
                    : isSelected ? _text.Selected.DisplayText : _text.Select.DisplayText);
        }
    }
}
