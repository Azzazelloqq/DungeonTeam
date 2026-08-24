using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DungeonTeam.Gameplay.AmbientNpc.Application;

namespace DungeonTeam.Gameplay.GuildHall.Application
{
    public sealed class GuildTextSnapshot
    {
        public GuildTextSnapshot(string textId, string displayText)
        {
            TextId = GuildId.Require(textId, nameof(textId));
            if (string.IsNullOrWhiteSpace(displayText))
            {
                throw new ArgumentException("Display text cannot be empty.", nameof(displayText));
            }

            DisplayText = displayText;
        }

        public string TextId { get; }
        public string DisplayText { get; }
    }

    public sealed class NoticeBoardOfferSnapshot
    {
        public enum OfferStatus
        {
            Available = 0,
            Disabled = 1,
            Active = 2,
            Completed = 3
        }

        public NoticeBoardOfferSnapshot(
            string contractId,
            GuildTextSnapshot title,
            GuildTextSnapshot summary,
            string locationId,
            bool isAvailable,
            GuildTextSnapshot disabledReason,
            string minimumRankId = null)
            : this(
                contractId,
                title,
                summary,
                locationId,
                isAvailable,
                disabledReason,
                minimumRankId,
                isAvailable ? OfferStatus.Available : OfferStatus.Disabled,
                null)
        {
        }

        public NoticeBoardOfferSnapshot(
            string contractId,
            GuildTextSnapshot title,
            GuildTextSnapshot summary,
            string locationId,
            bool isAvailable,
            GuildTextSnapshot disabledReason,
            string minimumRankId,
            OfferStatus status,
            GuildTextSnapshot statusText)
        {
            ContractId = GuildId.Require(contractId, nameof(contractId));
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Summary = summary ?? throw new ArgumentNullException(nameof(summary));
            LocationId = GuildId.Require(locationId, nameof(locationId));
            if (status == OfferStatus.Available && !isAvailable)
            {
                throw new ArgumentException(
                    "An available status requires an available offer.",
                    nameof(isAvailable));
            }

            if (status == OfferStatus.Disabled && isAvailable)
            {
                throw new ArgumentException(
                    "A disabled status requires an unavailable offer.",
                    nameof(isAvailable));
            }

            if (status == OfferStatus.Available && disabledReason != null)
            {
                throw new ArgumentException(
                    "An available offer cannot have a disabled reason.",
                    nameof(disabledReason));
            }

            if (status == OfferStatus.Disabled && disabledReason == null)
            {
                throw new ArgumentException(
                    "An unavailable offer requires a disabled reason.",
                    nameof(disabledReason));
            }

            IsAvailable = isAvailable;
            DisabledReason = disabledReason;
            MinimumRankId = string.IsNullOrWhiteSpace(minimumRankId)
                ? null
                : GuildId.Require(minimumRankId, nameof(minimumRankId));
            Status = status;
            StatusText = statusText;
        }

        public string ContractId { get; }
        public GuildTextSnapshot Title { get; }
        public GuildTextSnapshot Summary { get; }
        public string LocationId { get; }
        public bool IsAvailable { get; }
        public GuildTextSnapshot DisabledReason { get; }
        public string MinimumRankId { get; }
        public OfferStatus Status { get; }
        public GuildTextSnapshot StatusText { get; }
        public GuildTextSnapshot ActionText => StatusText;
        public bool CanAccept => IsAvailable && Status == OfferStatus.Available;
        public bool IsActive => Status == OfferStatus.Active;
        public bool IsCompleted => Status == OfferStatus.Completed;
    }

    public sealed class QuestBoardEntrySnapshot
    {
        public enum EntryStatus
        {
            Available = 0,
            Accepted = 1,
            Completed = 2,
            Locked = 3
        }

        public QuestBoardEntrySnapshot(
            string questId,
            GuildTextSnapshot title,
            GuildTextSnapshot summary,
            GuildTextSnapshot objective,
            GuildTextSnapshot progress,
            GuildTextSnapshot statusText,
            bool canAccept,
            EntryStatus status)
        {
            QuestId = GuildId.Require(questId, nameof(questId));
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Summary = summary ?? throw new ArgumentNullException(nameof(summary));
            Objective = objective ?? throw new ArgumentNullException(nameof(objective));
            Progress = progress ?? throw new ArgumentNullException(nameof(progress));
            StatusText = statusText ?? throw new ArgumentNullException(nameof(statusText));
            if (status == EntryStatus.Available && !canAccept)
                throw new ArgumentException("An available quest must be acceptable.", nameof(canAccept));
            if (status != EntryStatus.Available && canAccept)
                throw new ArgumentException("An accepted or completed quest cannot be acceptable.", nameof(canAccept));
            CanAccept = canAccept;
            Status = status;
        }

        public string QuestId { get; }
        public GuildTextSnapshot Title { get; }
        public GuildTextSnapshot Summary { get; }
        public GuildTextSnapshot Objective { get; }
        public GuildTextSnapshot Progress { get; }
        public GuildTextSnapshot StatusText { get; }
        public bool CanAccept { get; }
        public EntryStatus Status { get; }
        public bool IsAccepted => Status == EntryStatus.Accepted;
        public bool IsCompleted => Status == EntryStatus.Completed;
    }

    public sealed class NoticeBoardTextSnapshot
    {
        public NoticeBoardTextSnapshot(
            GuildTextSnapshot header,
            GuildTextSnapshot select,
            GuildTextSnapshot selected,
            GuildTextSnapshot close,
            GuildTextSnapshot empty)
            : this(header, select, selected, close, empty, select, selected, selected, selected)
        {
        }

        public NoticeBoardTextSnapshot(
            GuildTextSnapshot header,
            GuildTextSnapshot select,
            GuildTextSnapshot selected,
            GuildTextSnapshot close,
            GuildTextSnapshot empty,
            GuildTextSnapshot questAccept,
            GuildTextSnapshot questAccepted,
            GuildTextSnapshot questCompleted,
            GuildTextSnapshot questLocked)
        {
            Header = header ?? throw new ArgumentNullException(nameof(header));
            Select = select ?? throw new ArgumentNullException(nameof(select));
            Selected = selected ?? throw new ArgumentNullException(nameof(selected));
            Close = close ?? throw new ArgumentNullException(nameof(close));
            Empty = empty ?? throw new ArgumentNullException(nameof(empty));
            Completed = Selected;
            QuestAccept = questAccept ?? throw new ArgumentNullException(nameof(questAccept));
            QuestAccepted = questAccepted ?? throw new ArgumentNullException(nameof(questAccepted));
            QuestCompleted = questCompleted ?? throw new ArgumentNullException(nameof(questCompleted));
            QuestLocked = questLocked ?? throw new ArgumentNullException(nameof(questLocked));
        }

        public GuildTextSnapshot Header { get; }
        public GuildTextSnapshot Select { get; }
        public GuildTextSnapshot Selected { get; }
        public GuildTextSnapshot Close { get; }
        public GuildTextSnapshot Empty { get; }
        public GuildTextSnapshot Completed { get; }
        public GuildTextSnapshot QuestAccept { get; }
        public GuildTextSnapshot QuestAccepted { get; }
        public GuildTextSnapshot QuestCompleted { get; }
        public GuildTextSnapshot QuestLocked { get; }
    }

    public sealed class GuildRunSummarySnapshot
    {
        public GuildRunSummarySnapshot(
            GuildTextSnapshot outcome,
            GuildTextSnapshot dungeon,
            IReadOnlyList<GuildTextSnapshot> rewardLines,
            GuildRunSummaryTextSnapshot text)
        {
            Outcome = outcome ?? throw new ArgumentNullException(nameof(outcome));
            Dungeon = dungeon ?? throw new ArgumentNullException(nameof(dungeon));
            RewardLines = Snapshot.Copy(rewardLines, nameof(rewardLines));
            Text = text ?? throw new ArgumentNullException(nameof(text));
        }

        public GuildTextSnapshot Outcome { get; }
        public GuildTextSnapshot Dungeon { get; }
        public IReadOnlyList<GuildTextSnapshot> RewardLines { get; }
        public GuildRunSummaryTextSnapshot Text { get; }
    }

    public sealed class GuildRunSummaryTextSnapshot
    {
        public GuildRunSummaryTextSnapshot(
            GuildTextSnapshot header,
            GuildTextSnapshot completedOutcome,
            GuildTextSnapshot defeatedOutcome,
            GuildTextSnapshot dungeonLabel,
            GuildTextSnapshot rewardsLabel,
            string rewardLineFormat,
            GuildTextSnapshot emptyRewards,
            GuildTextSnapshot close)
        {
            Header = header ?? throw new ArgumentNullException(nameof(header));
            CompletedOutcome = completedOutcome ?? throw new ArgumentNullException(nameof(completedOutcome));
            DefeatedOutcome = defeatedOutcome ?? throw new ArgumentNullException(nameof(defeatedOutcome));
            DungeonLabel = dungeonLabel ?? throw new ArgumentNullException(nameof(dungeonLabel));
            RewardsLabel = rewardsLabel ?? throw new ArgumentNullException(nameof(rewardsLabel));
            if (string.IsNullOrWhiteSpace(rewardLineFormat))
            {
                throw new ArgumentException("Reward line format cannot be empty.", nameof(rewardLineFormat));
            }

            var displayNameProbe = new FormatProbe("__DISPLAY_NAME__");
            var amountProbe = new FormatProbe("__AMOUNT__");
            string formattedProbe;
            try
            {
                formattedProbe = string.Format(
                    rewardLineFormat,
                    displayNameProbe,
                    amountProbe);
            }
            catch (FormatException exception)
            {
                throw new ArgumentException(
                    "Reward line format must accept display name and amount.",
                    nameof(rewardLineFormat),
                    exception);
            }

            if (formattedProbe.IndexOf(displayNameProbe.Marker, StringComparison.Ordinal) < 0 ||
                formattedProbe.IndexOf(amountProbe.Marker, StringComparison.Ordinal) < 0)
            {
                throw new ArgumentException(
                    "Reward line format must include display name ({0}) and amount ({1}).",
                    nameof(rewardLineFormat));
            }

            RewardLineFormat = rewardLineFormat;
            EmptyRewards = emptyRewards ?? throw new ArgumentNullException(nameof(emptyRewards));
            Close = close ?? throw new ArgumentNullException(nameof(close));
        }

        public GuildTextSnapshot Header { get; }
        public GuildTextSnapshot CompletedOutcome { get; }
        public GuildTextSnapshot DefeatedOutcome { get; }
        public GuildTextSnapshot DungeonLabel { get; }
        public GuildTextSnapshot RewardsLabel { get; }
        public string RewardLineFormat { get; }
        public GuildTextSnapshot EmptyRewards { get; }
        public GuildTextSnapshot Close { get; }

        private sealed class FormatProbe : IFormattable
        {
            public FormatProbe(string marker) => Marker = marker;
            public string Marker { get; }
            public string ToString(string format, IFormatProvider formatProvider) => Marker;
            public override string ToString() => Marker;
        }
    }

    public sealed class GuildHallStartContext
    {
        public GuildHallStartContext(
            IReadOnlyList<AmbientNpcSnapshot> npcs,
            IReadOnlyList<NoticeBoardOfferSnapshot> offers,
            string selectedContractId,
            GuildRunSummarySnapshot lastRunSummary,
            GuildProfileSnapshot profile = null,
            IReadOnlyList<QuestBoardEntrySnapshot> quests = null)
        {
            Npcs = Snapshot.Copy(npcs, nameof(npcs));
            Offers = Snapshot.Copy(offers, nameof(offers));
            QuestEntries = Snapshot.Copy(quests ?? Array.Empty<QuestBoardEntrySnapshot>(), nameof(quests));
            SelectedContractId = selectedContractId == null
                ? null
                : GuildId.Require(selectedContractId, nameof(selectedContractId));
            LastRunSummary = lastRunSummary;
            Profile = profile;

            if (SelectedContractId != null)
            {
                var exists = false;
                for (var index = 0; index < Offers.Count; index++)
                {
                    if (Offers[index].ContractId == SelectedContractId)
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    throw new ArgumentException(
                        $"Selected contract '{SelectedContractId}' is absent from the offers snapshot.",
                        nameof(selectedContractId));
                }
            }
        }

        public IReadOnlyList<AmbientNpcSnapshot> Npcs { get; }
        public IReadOnlyList<NoticeBoardOfferSnapshot> Offers { get; }
        public IReadOnlyList<QuestBoardEntrySnapshot> QuestEntries { get; }
        public string SelectedContractId { get; }
        public GuildRunSummarySnapshot LastRunSummary { get; }
        public GuildProfileSnapshot Profile { get; }
    }

    public sealed class GuildSessionState
    {
        public string SelectedContractId { get; private set; }
        public GuildRunSummarySnapshot LastRunSummary { get; private set; }

        public void SelectContract(string contractId)
        {
            SelectedContractId = GuildId.Require(contractId, nameof(contractId));
        }

        public void ClearSelectedContract()
        {
            SelectedContractId = null;
        }

        public void SetLastRunSummary(GuildRunSummarySnapshot summary)
        {
            LastRunSummary = summary ?? throw new ArgumentNullException(nameof(summary));
        }

        public void ClearLastRunSummary()
        {
            LastRunSummary = null;
        }
    }

    internal static class GuildId
    {
        public static string Require(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Stable ID cannot be empty.", parameterName);
            }

            return value;
        }
    }

    internal static class Snapshot
    {
        public static IReadOnlyList<T> Copy<T>(IReadOnlyList<T> source, string parameterName)
            where T : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            var copy = new T[source.Count];
            for (var index = 0; index < source.Count; index++)
            {
                copy[index] = source[index] ?? throw new ArgumentException(
                    $"Entry at index {index} is missing.",
                    parameterName);
            }

            return new ReadOnlyCollection<T>(copy);
        }
    }
}
