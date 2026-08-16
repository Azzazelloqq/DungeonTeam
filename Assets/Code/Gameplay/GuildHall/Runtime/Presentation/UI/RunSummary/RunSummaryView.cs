using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azzazelloqq.MVVM.ReactiveLibrary;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.UI.RunSummary
{
    public sealed class RunSummaryView : RunSummaryViewBase
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private TMP_Text _headerText;
        [SerializeField] private TMP_Text _outcomeText;
        [SerializeField] private TMP_Text _dungeonText;
        [SerializeField] private TMP_Text _rewardsLabelText;
        [SerializeField] private TMP_Text _emptyRewardsText;
        [SerializeField] private RectTransform _rowsContainer;
        [SerializeField] private TMP_Text _rowTemplate;
        [SerializeField] private Button _closeButton;
        [SerializeField] private TMP_Text _closeText;

        private readonly List<TMP_Text> _rows = new();
        private UnityAction _closeRequested;

        public override void ValidateBindings()
        {
            if (_panel == null || _headerText == null || _outcomeText == null ||
                _dungeonText == null || _rewardsLabelText == null || _emptyRewardsText == null ||
                _rowsContainer == null || _rowTemplate == null || _closeButton == null || _closeText == null)
            {
                throw new InvalidOperationException("Run Summary view requires all serialized bindings.");
            }

            if (_rowTemplate.gameObject.activeSelf)
            {
                throw new InvalidOperationException("Run Summary row template must be inactive.");
            }
        }

        internal int RowCount => _rows.Count;
        internal Button CloseButton => _closeButton;

        protected override void OnInitialize()
        {
            ValidateBindings();
            var summary = viewModel.Summary;
            _headerText.SetText(summary.Text.Header.DisplayText);
            _outcomeText.SetText(summary.Outcome.DisplayText);
            _dungeonText.SetText($"{summary.Text.DungeonLabel.DisplayText}: {summary.Dungeon.DisplayText}");
            _rewardsLabelText.SetText(summary.Text.RewardsLabel.DisplayText);
            _emptyRewardsText.SetText(summary.Text.EmptyRewards.DisplayText);
            _closeText.SetText(summary.Text.Close.DisplayText);
            _emptyRewardsText.gameObject.SetActive(summary.RewardLines.Count == 0);
            CreateRows();
            _closeRequested = () => viewModel.CloseCommand.Execute(null);
            _closeButton.onClick.AddListener(_closeRequested);
            viewModel.IsVisible.Subscribe(SetVisible).AddTo(compositeDisposable);
        }

        protected override ValueTask OnInitializeAsync(CancellationToken token) => default;

        protected override void OnDispose()
        {
            if (_closeButton != null && _closeRequested != null)
            {
                _closeButton.onClick.RemoveListener(_closeRequested);
            }

            _closeRequested = null;
            for (var index = _rows.Count - 1; index >= 0; index--)
            {
                if (_rows[index] != null)
                {
                    Destroy(_rows[index].gameObject);
                }
            }

            _rows.Clear();
        }

        protected override ValueTask OnDisposeAsync(CancellationToken token)
        {
            OnDispose();
            return default;
        }

        private void CreateRows()
        {
            var lines = viewModel.Summary.RewardLines;
            for (var index = 0; index < lines.Count; index++)
            {
                var row = Instantiate(_rowTemplate, _rowsContainer);
                row.SetText(lines[index].DisplayText);
                row.gameObject.SetActive(true);
                _rows.Add(row);
            }
        }

        private void SetVisible(bool isVisible) => _panel.SetActive(isVisible);
    }
}
