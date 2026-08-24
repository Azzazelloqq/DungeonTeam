using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azzazelloqq.MVVM.ReactiveLibrary;
using DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.UI.QuestRewardCollection.Base;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.UI.QuestRewardCollection
{
    public sealed class QuestRewardCollectionView : QuestRewardCollectionViewBase
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private TMP_Text _headerText;
        [SerializeField] private RectTransform _rowsContainer;
        [SerializeField] private Button _rowTemplate;
        [SerializeField] private Button _closeButton;
        [SerializeField] private TMP_Text _closeText;

        private readonly List<Button> _rows = new();
        private readonly List<UnityAction> _rowActions = new();
        private UnityAction _closeRequested;

        public override void ValidateBindings()
        {
            if (_panel == null || _headerText == null || _rowsContainer == null ||
                _rowTemplate == null || _closeButton == null || _closeText == null)
                throw new InvalidOperationException("Quest Reward Collection view requires all serialized bindings.");
            if (_rowTemplate.gameObject.activeSelf)
                throw new InvalidOperationException("Quest Reward Collection row template must be inactive.");
            if (_rowTemplate.GetComponentInChildren<TMP_Text>(true) == null)
                throw new InvalidOperationException("Quest Reward Collection row template requires a TMP label.");
        }

        protected override void OnInitialize()
        {
            ValidateBindings();
            _headerText.SetText(viewModel.Header.DisplayText);
            _closeText.SetText(viewModel.CloseText.DisplayText);
            RebuildRows();
            _closeRequested = () => viewModel.CloseCommand.Execute(null);
            _closeButton.onClick.AddListener(_closeRequested);
            viewModel.IsVisible.Subscribe(SetVisible).AddTo(compositeDisposable);
            viewModel.Revision.Subscribe(_ => RebuildRows()).AddTo(compositeDisposable);
        }

        protected override ValueTask OnInitializeAsync(CancellationToken token) => default;

        protected override void OnDispose()
        {
            if (_closeButton != null && _closeRequested != null)
                _closeButton.onClick.RemoveListener(_closeRequested);
            _closeRequested = null;
            DestroyRows();
        }

        protected override ValueTask OnDisposeAsync(CancellationToken token)
        {
            OnDispose();
            return default;
        }

        private void RebuildRows()
        {
            DestroyRows();
            for (var index = 0; index < viewModel.Entries.Count; index++)
            {
                var entry = viewModel.Entries[index];
                var row = Instantiate(_rowTemplate, _rowsContainer);
                var label = row.GetComponentInChildren<TMP_Text>(true);
                var builder = new StringBuilder(entry.Title.DisplayText);
                for (var line = 0; line < entry.RewardLines.Count; line++)
                    builder.Append('\n').Append(entry.RewardLines[line].DisplayText);
                builder.Append('\n').Append(entry.SourceHint.DisplayText);
                builder.Append('\n').Append(entry.ReceiveText.DisplayText);
                label.SetText(builder.ToString());
                var questId = entry.QuestId;
                UnityAction action = () => viewModel.ReceiveCommand.Execute(questId);
                row.onClick.AddListener(action);
                row.gameObject.SetActive(true);
                _rows.Add(row);
                _rowActions.Add(action);
            }
        }

        private void DestroyRows()
        {
            for (var index = _rows.Count - 1; index >= 0; index--)
            {
                if (_rows[index] != null) Destroy(_rows[index].gameObject);
            }
            _rows.Clear();
            _rowActions.Clear();
        }

        private void SetVisible(bool visible) => _panel.SetActive(visible);
    }
}
