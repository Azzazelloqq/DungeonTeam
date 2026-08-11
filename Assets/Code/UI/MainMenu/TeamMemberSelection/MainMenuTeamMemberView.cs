using System.Threading;
using System.Threading.Tasks;
using Azzazelloqq.MVVM.ReactiveLibrary;
using Code.UI.MainMenu.TeamMemberSelection.Base;
using UnityEngine;
using UnityEngine.UI;

namespace Code.UI.MainMenu.TeamMemberSelection
{
    public sealed class MainMenuTeamMemberView : MainMenuTeamMemberViewBase
    {
        [SerializeField]
        private Text _label;

        [SerializeField]
        private Button _leaderButton;

        [SerializeField]
        private Button _companionButton;

        [SerializeField]
        private Text _companionButtonLabel;

        [SerializeField]
        private Text _levelLabel;

        [SerializeField]
        private Button _decreaseLevelButton;

        [SerializeField]
        private Button _increaseLevelButton;

        protected override void OnInitialize()
        {
            _leaderButton.onClick.AddListener(OnLeaderClicked);
            _companionButton.onClick.AddListener(OnCompanionClicked);
            _decreaseLevelButton.onClick.AddListener(OnDecreaseLevelClicked);
            _increaseLevelButton.onClick.AddListener(OnIncreaseLevelClicked);
            viewModel.Label.Subscribe(SetLabel).AddTo(compositeDisposable);
            viewModel.IsLeader.Subscribe(SetLeader).AddTo(compositeDisposable);
            viewModel.IsCompanion.Subscribe(SetCompanion).AddTo(compositeDisposable);
            viewModel.CanToggleCompanion.Subscribe(SetCanToggleCompanion).AddTo(compositeDisposable);
            viewModel.LevelLabel.Subscribe(SetLevelLabel).AddTo(compositeDisposable);
            viewModel.CanDecreaseLevel.Subscribe(SetCanDecreaseLevel).AddTo(compositeDisposable);
            viewModel.CanIncreaseLevel.Subscribe(SetCanIncreaseLevel).AddTo(compositeDisposable);
        }

        protected override ValueTask OnInitializeAsync(CancellationToken token)
        {
            return default;
        }

        protected override void OnDispose()
        {
            _leaderButton.onClick.RemoveListener(OnLeaderClicked);
            _companionButton.onClick.RemoveListener(OnCompanionClicked);
            _decreaseLevelButton.onClick.RemoveListener(OnDecreaseLevelClicked);
            _increaseLevelButton.onClick.RemoveListener(OnIncreaseLevelClicked);
        }

        protected override ValueTask OnDisposeAsync(CancellationToken token)
        {
            return default;
        }

        private void SetLabel(string value)
        {
            _label.text = value;
        }

        private void SetLeader(bool isLeader)
        {
            _leaderButton.interactable = !isLeader;
        }

        private void SetCompanion(bool isCompanion)
        {
            _companionButtonLabel.text = isCompanion ? "REMOVE" : "ADD";
        }

        private void SetCanToggleCompanion(bool canToggle)
        {
            _companionButton.interactable = canToggle;
        }

        private void OnLeaderClicked()
        {
            viewModel.SelectLeaderCommand.Execute();
        }

        private void OnCompanionClicked()
        {
            viewModel.ToggleCompanionCommand.Execute();
        }

        private void SetLevelLabel(string value)
        {
            _levelLabel.text = value;
        }

        private void SetCanDecreaseLevel(bool value)
        {
            _decreaseLevelButton.interactable = value;
        }

        private void SetCanIncreaseLevel(bool value)
        {
            _increaseLevelButton.interactable = value;
        }

        private void OnDecreaseLevelClicked()
        {
            viewModel.DecreaseLevelCommand.Execute();
        }

        private void OnIncreaseLevelClicked()
        {
            viewModel.IncreaseLevelCommand.Execute();
        }
    }
}
