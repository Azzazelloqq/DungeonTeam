using System;
using System.Threading;
using System.Threading.Tasks;
using Azzazelloqq.MVVM.ReactiveLibrary;
using DungeonTeam.Gameplay.GuildHall.Application;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.UI.NoticeBoard
{
    public sealed class QuestBoardItemView : MonoBehaviour, IDisposable
    {
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _summaryText;
        [SerializeField] private TMP_Text _progressText;
        [SerializeField] private TMP_Text _acceptLabel;
        [SerializeField] private GameObject _completedMarker;
        [SerializeField] private Button _acceptButton;

        private readonly Disposable.CompositeDisposable _subscriptions = new();
        private UnityAction _acceptRequested;

        public void Initialize(QuestBoardItemViewModel viewModel, NoticeBoardTextSnapshot text)
        {
            if (viewModel == null) throw new ArgumentNullException(nameof(viewModel));
            if (text == null) throw new ArgumentNullException(nameof(text));
            ValidateBindings();
            _titleText.SetText(viewModel.Title);
            _summaryText.SetText($"{viewModel.Summary}\n{viewModel.Objective}");
            _progressText.SetText(viewModel.IsCompleted ? viewModel.StatusText : viewModel.Progress);
            _acceptButton.interactable = viewModel.CanAccept;
            _acceptLabel.SetText(viewModel.StatusText);
            _completedMarker.SetActive(viewModel.IsCompleted);
            _acceptRequested = () => viewModel.AcceptCommand.Execute(null);
            _acceptButton.onClick.AddListener(_acceptRequested);
            viewModel.IsAccepted.Subscribe(_ => Refresh(viewModel)).AddTo(_subscriptions);
        }

        public void Dispose()
        {
            if (_acceptButton != null && _acceptRequested != null)
                _acceptButton.onClick.RemoveListener(_acceptRequested);
            _acceptRequested = null;
            _subscriptions.Dispose();
        }

        private void ValidateBindings()
        {
            if (_titleText == null || _summaryText == null || _progressText == null ||
                _acceptLabel == null || _completedMarker == null || _acceptButton == null)
                throw new InvalidOperationException("Quest Board item view requires title, summary, progress, status and button bindings.");
        }

        private void Refresh(QuestBoardItemViewModel viewModel)
        {
            _acceptButton.interactable = viewModel.CanAccept;
            _acceptLabel.SetText(viewModel.CanAccept ? viewModel.StatusText : viewModel.StatusText);
            _completedMarker.SetActive(viewModel.IsCompleted);
        }
    }
}
