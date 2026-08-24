using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Azzazelloqq.MVVM.Core;
using Azzazelloqq.MVVM.ReactiveLibrary;
using DungeonTeam.Gameplay.GuildHall.Application;

namespace DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.UI.NoticeBoard
{
    public sealed class NoticeBoardModel : ModelBase
    {
        private readonly ReactiveProperty<bool> _isVisible = new(false);
        private readonly ReactiveProperty<string> _selectedContractId = new();
        private readonly IReadOnlyList<NoticeBoardOfferSnapshot> _offers;

        public NoticeBoardModel(
            IReadOnlyList<NoticeBoardOfferSnapshot> offers,
            string selectedContractId,
            NoticeBoardTextSnapshot text,
            IReadOnlyList<QuestBoardEntrySnapshot> quests = null)
        {
            if (offers == null)
            {
                throw new ArgumentNullException(nameof(offers));
            }

            var copy = new NoticeBoardOfferSnapshot[offers.Count];
            var offerIds = new HashSet<string>(StringComparer.Ordinal);
            var selectedExists = selectedContractId == null;
            for (var index = 0; index < offers.Count; index++)
            {
                var offer = offers[index] ?? throw new ArgumentException(
                    $"Notice Board offer at index {index} is missing.", nameof(offers));
                if (!offerIds.Add(offer.ContractId))
                {
                    throw new ArgumentException(
                        $"Notice Board offer ID '{offer.ContractId}' is duplicated.", nameof(offers));
                }

                copy[index] = offer;
                selectedExists |= offer.ContractId == selectedContractId;
            }

            if (!selectedExists)
            {
                throw new ArgumentException(
                    $"Selected contract '{selectedContractId}' is absent from Notice Board offers.",
                    nameof(selectedContractId));
            }

            _offers = new ReadOnlyCollection<NoticeBoardOfferSnapshot>(copy);
            var questCopy = quests ?? Array.Empty<QuestBoardEntrySnapshot>();
            var questIds = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < questCopy.Count; index++)
            {
                var quest = questCopy[index] ?? throw new ArgumentException(
                    $"Quest Board entry at index {index} is missing.", nameof(quests));
                if (!questIds.Add(quest.QuestId))
                    throw new ArgumentException(
                        $"Quest Board entry ID '{quest.QuestId}' is duplicated.", nameof(quests));
            }

            var questSnapshot = new QuestBoardEntrySnapshot[questCopy.Count];
            for (var index = 0; index < questSnapshot.Length; index++) questSnapshot[index] = questCopy[index];
            Quests = new ReadOnlyCollection<QuestBoardEntrySnapshot>(questSnapshot);
            Text = text ?? throw new ArgumentNullException(nameof(text));
            _selectedContractId.SetValue(selectedContractId);
            _isVisible.AddTo(compositeDisposable);
            _selectedContractId.AddTo(compositeDisposable);
        }

        public IReadOnlyList<NoticeBoardOfferSnapshot> Offers => _offers;
        public NoticeBoardTextSnapshot Text { get; }
        public IReadOnlyList<QuestBoardEntrySnapshot> Quests { get; }
        public IReadOnlyReactiveProperty<bool> IsVisible => _isVisible;
        public IReadOnlyReactiveProperty<string> SelectedContractId => _selectedContractId;

        public void Show() => _isVisible.SetValue(true);
        public void Hide() => _isVisible.SetValue(false);

        public bool TrySelect(string contractId)
        {
            if (!CanSelect(contractId))
            {
                return false;
            }

            ApplyAcceptedSelection(contractId);
            return true;
        }

        public bool CanSelect(string contractId)
        {
            if (string.IsNullOrWhiteSpace(contractId) || contractId == _selectedContractId.Value)
            {
                return false;
            }

            for (var index = 0; index < _offers.Count; index++)
            {
                var offer = _offers[index];
                if (offer.ContractId != contractId)
                {
                    continue;
                }

                if (!offer.CanAccept)
                {
                    return false;
                }

                return true;
            }

            return false;
        }

        internal void ApplyAcceptedSelection(string contractId)
        {
            if (!CanSelect(contractId))
            {
                throw new InvalidOperationException(
                    "Notice Board selection must be accepted before it is applied.");
            }

            _selectedContractId.SetValue(contractId);
        }

        protected override void OnInitialize() { }
        protected override ValueTask OnInitializeAsync(CancellationToken token) => default;
        protected override void OnDispose() { }
        protected override ValueTask OnDisposeAsync(CancellationToken token) => default;
    }
}
