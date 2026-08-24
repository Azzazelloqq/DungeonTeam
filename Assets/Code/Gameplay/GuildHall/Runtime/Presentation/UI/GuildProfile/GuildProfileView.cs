using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Azzazelloqq.MVVM.ReactiveLibrary;
using DungeonTeam.Gameplay.GuildHall.Application;
using DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.UI.GuildProfile.Base;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.UI.GuildProfile
{
    public sealed class GuildProfileView : GuildProfileViewBase
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private TMP_Text _headerText;
        [SerializeField] private TMP_Text _goldText;
        [SerializeField] private TMP_Text _rankText;
        [SerializeField] private TMP_Text _leaderLabelText;
        [SerializeField] private TMP_Text _leaderExplanationText;
        [SerializeField] private TMP_Text _teamLabelText;
        [SerializeField] private TMP_Text _rosterLabelText;
        [SerializeField] private TMP_Text _leaderCardText;
        [SerializeField] private RectTransform _teamRowsContainer;
        [SerializeField] private TMP_Text _teamRowTemplate;
        [SerializeField] private RectTransform _rosterRowsContainer;
        [SerializeField] private Button _rosterRowTemplate;
        [SerializeField] private TMP_Text _detailsText;
        [SerializeField] private RectTransform _skillRowsContainer;
        [SerializeField] private TMP_Text _skillRowTemplate;
        [SerializeField] private Button _closeButton;
        [SerializeField] private TMP_Text _closeText;

        private readonly List<TMP_Text> _teamRows = new();
        private readonly List<Button> _rosterRows = new();
        private readonly List<UnityAction> _rosterActions = new();
        private readonly List<TMP_Text> _skillRows = new();
        private UnityAction _closeRequested;
        private GuildTextSnapshot _rejection;

        public override void ValidateBindings()
        {
            if (_panel == null || _headerText == null || _goldText == null ||
                _rankText == null || _leaderLabelText == null ||
                _leaderExplanationText == null || _teamLabelText == null ||
                _rosterLabelText == null || _leaderCardText == null ||
                _teamRowsContainer == null || _teamRowTemplate == null ||
                _rosterRowsContainer == null || _rosterRowTemplate == null ||
                _detailsText == null || _skillRowsContainer == null ||
                _skillRowTemplate == null || _closeButton == null || _closeText == null)
            {
                throw new InvalidOperationException(
                    "Guild Profile view requires all serialized bindings.");
            }

            if (_teamRowTemplate.gameObject.activeSelf ||
                _rosterRowTemplate.gameObject.activeSelf ||
                _skillRowTemplate.gameObject.activeSelf)
            {
                throw new InvalidOperationException(
                    "Guild Profile row templates must be inactive.");
            }

            if (_rosterRowTemplate.GetComponentInChildren<TMP_Text>(true) == null)
            {
                throw new InvalidOperationException(
                    "Guild Profile roster row template requires a TMP label.");
            }
        }

        protected override void OnInitialize()
        {
            ValidateBindings();
            Refresh(viewModel.Profile);
            viewModel.SelectedHero.Subscribe(hero =>
            {
                RebuildForSelectedHero(hero);
                ShowDetails(hero);
            }).AddTo(compositeDisposable);
            viewModel.CurrentProfile.Subscribe(Refresh).AddTo(compositeDisposable);
            viewModel.Rejection.Subscribe(rejection =>
            {
                _rejection = rejection;
                ShowDetails(viewModel.SelectedHero.Value);
            }).AddTo(compositeDisposable);
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
            DestroyRosterRows();
            DestroyRows(_teamRows);
            DestroyRows(_skillRows);
            _rosterRows.Clear();
            _rosterActions.Clear();
        }

        protected override ValueTask OnDisposeAsync(CancellationToken token)
        {
            OnDispose();
            return default;
        }

        private void CreateTeamRows()
        {
            for (var index = 0; index < viewModel.Profile.Companions.Count; index++)
            {
                var row = Instantiate(_teamRowTemplate, _teamRowsContainer);
                row.SetText(GetHeroTitle(viewModel.Profile.Companions[index]));
                row.gameObject.SetActive(true);
                _teamRows.Add(row);
            }
        }

        private void CreateRosterRows()
        {
            for (var index = 0; index < viewModel.Profile.Roster.Count; index++)
            {
                var hero = viewModel.Profile.Roster[index];
                var row = Instantiate(_rosterRowTemplate, _rosterRowsContainer);
                var label = row.GetComponentInChildren<TMP_Text>(true);
                label.SetText($"{GetRoleText(hero.Role)} \u2014 {GetHeroTitle(hero)}");
                var actorId = hero.ActorId;
                UnityAction action = () => viewModel.SelectHeroCommand.Execute(actorId);
                row.onClick.AddListener(action);
                row.gameObject.SetActive(true);
                _rosterRows.Add(row);
                _rosterActions.Add(action);
            }
        }

        private void CreateEditRows(GuildHeroSnapshot hero)
        {
            var text = viewModel.Profile.Text;
            var rank = viewModel.Profile.Rank;
            if (rank != null && rank.NextRankDisplayName != null && rank.PromotionCost.HasValue)
            {
                var promoteLabel = text.PromoteRank ?? text.RankLabel;
                CreateEditRow(
                    $"{promoteLabel.DisplayText}: {rank.NextRankDisplayName} ({rank.PromotionCost.Value})",
                    () => viewModel.PromoteRankCommand.Execute(null),
                    rank.CanPromote);
            }

            if (hero.Role != GuildHeroRole.Leader)
            {
                CreateEditRow(text.MakeLeader.DisplayText, () =>
                    viewModel.SetLeaderCommand.Execute(null), true);
            }

            if (hero.Role == GuildHeroRole.Available)
            {
                CreateEditRow(text.AddCompanion.DisplayText, () =>
                    viewModel.AddCompanionCommand.Execute(null), true);
            }
            else if (hero.Role == GuildHeroRole.Companion)
            {
                CreateEditRow(text.RemoveCompanion.DisplayText, () =>
                    viewModel.RemoveCompanionCommand.Execute(null), true);
            }

            for (var index = 0; index < hero.AllowedLoadouts.Count; index++)
            {
                var option = hero.AllowedLoadouts[index];
                var loadoutId = option.LoadoutId;
                CreateEditRow(option.DisplayText, () =>
                    viewModel.SetLoadoutCommand.Execute(loadoutId),
                    !string.Equals(loadoutId, hero.LoadoutId, StringComparison.Ordinal));
            }

            for (var index = 0; index < hero.Equipment.Count; index++)
            {
                var equipment = hero.Equipment[index];
                var equipmentSlot = equipment.Slot;
                if (!string.IsNullOrWhiteSpace(equipment.InstanceId))
                {
                    CreateEditRow(
                        $"{equipment.DisplayText}: {equipment.ItemDisplayName} — Unequip",
                        () => viewModel.UnequipItemCommand.Execute(equipmentSlot),
                        true);
                }
            }

            for (var index = 0; index < hero.InventoryItems.Count; index++)
            {
                var item = hero.InventoryItems[index];
                if (!item.IsEquipped && item.CanEquip)
                {
                    var instanceId = item.InstanceId;
                    CreateEditRow(
                        $"Equip: {item.DisplayText}",
                        () => viewModel.EquipItemCommand.Execute(instanceId),
                        true);
                }

                if (!item.IsEquipped)
                {
                    var instanceId = item.InstanceId;
                    CreateEditRow(
                        $"Sell: {item.DisplayText} +{item.SaleValue}",
                        () => viewModel.SellUniqueItemCommand.Execute(instanceId),
                        true);
                }
            }

            for (var index = 0; index < viewModel.Profile.Resources.Count; index++)
            {
                var resource = viewModel.Profile.Resources[index];
                var definitionId = resource.DefinitionId;
                CreateEditRow(
                    $"Sell: {resource.DisplayText} x{resource.Quantity} +{resource.SaleValue}",
                    () => viewModel.SellResourceCommand.Execute(definitionId),
                    true);
            }
        }

        private void CreateEditRow(
            string displayText,
            UnityAction action,
            bool isInteractable)
        {
            var row = Instantiate(_rosterRowTemplate, _rosterRowsContainer);
            row.GetComponentInChildren<TMP_Text>(true).SetText(displayText);
            row.onClick.AddListener(action);
            row.interactable = isInteractable;
            row.gameObject.SetActive(true);
            _rosterRows.Add(row);
            _rosterActions.Add(action);
        }

        private void ShowDetails(GuildHeroSnapshot hero)
        {
            var text = viewModel.Profile.Text;
            _detailsText.SetText(
                $"{hero.DisplayName}\n" +
                $"{text.LevelLabel.DisplayText}: {hero.Level}\n" +
                $"{text.HealthLabel.DisplayText}: {hero.MaximumHealth}\n" +
                $"{text.SpeedLabel.DisplayText}: " +
                hero.MovementSpeed.ToString("0.##", CultureInfo.InvariantCulture));

            for (var index = 0; index < hero.Equipment.Count; index++)
            {
                var equipment = hero.Equipment[index];
                _detailsText.SetText(
                    $"{_detailsText.text}\n{equipment.DisplayText}: " +
                    (string.IsNullOrWhiteSpace(equipment.ItemDisplayName) ? "—" : equipment.ItemDisplayName));
            }

            if (_rejection != null)
            {
                _detailsText.SetText($"{_detailsText.text}\n{_rejection.DisplayText}");
            }

            DestroyRows(_skillRows);
            for (var index = 0; index < hero.Skills.Count; index++)
            {
                var skill = hero.Skills[index];
                var row = Instantiate(_skillRowTemplate, _skillRowsContainer);
                row.SetText(
                    $"{skill.SlotDisplayText}: {skill.DisplayName} \u2014 " +
                    $"{text.LevelLabel.DisplayText} {skill.Level}");
                row.gameObject.SetActive(true);
                _skillRows.Add(row);
            }
        }

        private void Refresh(GuildProfileSnapshot profile)
        {
            var text = profile.Text;
            _headerText.SetText(text.Header.DisplayText);
            _goldText.SetText($"{text.GoldLabel.DisplayText}: {profile.Gold}");
            var rankDisplay = $"{text.CurrentRankLabel.DisplayText}: {profile.RankDisplayText}";
            if (profile.Rank?.NextRankDisplayName != null && profile.Rank.PromotionCost.HasValue)
            {
                rankDisplay +=
                    $" | {text.NextRankLabel.DisplayText}: {profile.Rank.NextRankDisplayName} " +
                    $"({profile.Rank.PromotionCost.Value})";
            }
            else if (profile.Rank != null && text.TerminalRank != null)
            {
                rankDisplay += $" — {text.TerminalRank.DisplayText}";
            }

            _rankText.SetText(rankDisplay);
            _leaderLabelText.SetText(text.LeaderLabel.DisplayText);
            _leaderExplanationText.SetText(text.LeaderExplanation.DisplayText);
            _teamLabelText.SetText(text.TeamLabel.DisplayText);
            _rosterLabelText.SetText(text.RosterLabel.DisplayText);
            _leaderCardText.SetText(GetHeroTitle(profile.Leader));
            _closeText.SetText(text.Close.DisplayText);
            DestroyRows(_teamRows);
            DestroyRosterRows();
            CreateTeamRows();
            CreateRosterRows();
            CreateEditRows(viewModel.SelectedHero.Value);
            ShowDetails(viewModel.SelectedHero.Value);
        }

        private void DestroyRosterRows()
        {
            for (var index = _rosterRows.Count - 1; index >= 0; index--)
            {
                var row = _rosterRows[index];
                if (row != null)
                {
                    row.onClick.RemoveListener(_rosterActions[index]);
                    Destroy(row.gameObject);
                }
            }

            _rosterRows.Clear();
            _rosterActions.Clear();
        }

        private void RebuildForSelectedHero(GuildHeroSnapshot hero)
        {
            DestroyRosterRows();
            CreateRosterRows();
            CreateEditRows(hero);
        }

        private string GetRoleText(GuildHeroRole role)
        {
            var text = viewModel.Profile.Text;
            return role switch
            {
                GuildHeroRole.Leader => text.LeaderLabel.DisplayText,
                GuildHeroRole.Companion => text.TeamLabel.DisplayText,
                GuildHeroRole.Available => text.AvailableHeroLabel.DisplayText,
                _ => throw new ArgumentOutOfRangeException(nameof(role), role, null)
            };
        }

        private string GetHeroTitle(GuildHeroSnapshot hero) =>
            $"{hero.DisplayName}  {viewModel.Profile.Text.LevelLabel.DisplayText}: {hero.Level}";

        private void SetVisible(bool isVisible) => _panel.SetActive(isVisible);

        private static void DestroyRows(List<TMP_Text> rows)
        {
            for (var index = rows.Count - 1; index >= 0; index--)
            {
                if (rows[index] != null)
                {
                    Destroy(rows[index].gameObject);
                }
            }

            rows.Clear();
        }
    }
}
