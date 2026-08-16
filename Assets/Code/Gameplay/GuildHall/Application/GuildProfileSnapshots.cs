using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DungeonTeam.Gameplay.GuildHall.Application
{
    public enum GuildProfileEditKind
    {
        SetLeader = 0,
        AddCompanion = 1,
        RemoveCompanion = 2,
        SetLoadout = 3
    }

    public sealed class GuildProfileEditRequest
    {
        public GuildProfileEditRequest(
            GuildProfileEditKind kind,
            string actorId,
            string loadoutId = null)
        {
            if (!Enum.IsDefined(typeof(GuildProfileEditKind), kind))
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }

            if (string.IsNullOrWhiteSpace(actorId))
            {
                throw new ArgumentException("Actor ID cannot be empty.", nameof(actorId));
            }

            if (kind == GuildProfileEditKind.SetLoadout !=
                !string.IsNullOrWhiteSpace(loadoutId))
            {
                throw new ArgumentException(
                    "Loadout ID is required only for a loadout change.",
                    nameof(loadoutId));
            }

            Kind = kind;
            ActorId = actorId;
            LoadoutId = loadoutId;
        }

        public GuildProfileEditKind Kind { get; }
        public string ActorId { get; }
        public string LoadoutId { get; }
    }

    public sealed class GuildProfileEditResult
    {
        private GuildProfileEditResult(
            bool accepted,
            GuildProfileSnapshot profile,
            GuildTextSnapshot rejection)
        {
            Accepted = accepted;
            Profile = profile;
            Rejection = rejection;
        }

        public bool Accepted { get; }
        public GuildProfileSnapshot Profile { get; }
        public GuildTextSnapshot Rejection { get; }

        public static GuildProfileEditResult Accept(GuildProfileSnapshot profile) =>
            new(true, profile ?? throw new ArgumentNullException(nameof(profile)), null);

        public static GuildProfileEditResult Reject(GuildTextSnapshot rejection) =>
            new(false, null, rejection ?? throw new ArgumentNullException(nameof(rejection)));
    }

    public enum GuildHeroRole
    {
        Leader = 0,
        Companion = 1,
        Available = 2
    }

    public sealed class GuildProfileTextSnapshot
    {
        public GuildProfileTextSnapshot(
            GuildTextSnapshot header,
            GuildTextSnapshot goldLabel,
            GuildTextSnapshot rankLabel,
            GuildTextSnapshot unassignedRank,
            GuildTextSnapshot leaderLabel,
            GuildTextSnapshot leaderExplanation,
            GuildTextSnapshot teamLabel,
            GuildTextSnapshot rosterLabel,
            GuildTextSnapshot availableHeroLabel,
            GuildTextSnapshot levelLabel,
            GuildTextSnapshot healthLabel,
            GuildTextSnapshot speedLabel,
            GuildTextSnapshot primarySkillLabel,
            GuildTextSnapshot activeSkillLabel,
            GuildTextSnapshot close,
            GuildTextSnapshot makeLeader,
            GuildTextSnapshot addCompanion,
            GuildTextSnapshot removeCompanion,
            GuildTextSnapshot loadoutLabel,
            GuildTextSnapshot rejectedTeamSize,
            GuildTextSnapshot rejectedInvalidActor,
            GuildTextSnapshot rejectedInvalidLoadout,
            GuildTextSnapshot rejectedPersistence)
        {
            Header = header ?? throw new ArgumentNullException(nameof(header));
            GoldLabel = goldLabel ?? throw new ArgumentNullException(nameof(goldLabel));
            RankLabel = rankLabel ?? throw new ArgumentNullException(nameof(rankLabel));
            UnassignedRank = unassignedRank ?? throw new ArgumentNullException(nameof(unassignedRank));
            LeaderLabel = leaderLabel ?? throw new ArgumentNullException(nameof(leaderLabel));
            LeaderExplanation = leaderExplanation ??
                throw new ArgumentNullException(nameof(leaderExplanation));
            TeamLabel = teamLabel ?? throw new ArgumentNullException(nameof(teamLabel));
            RosterLabel = rosterLabel ?? throw new ArgumentNullException(nameof(rosterLabel));
            AvailableHeroLabel = availableHeroLabel ??
                throw new ArgumentNullException(nameof(availableHeroLabel));
            LevelLabel = levelLabel ?? throw new ArgumentNullException(nameof(levelLabel));
            HealthLabel = healthLabel ?? throw new ArgumentNullException(nameof(healthLabel));
            SpeedLabel = speedLabel ?? throw new ArgumentNullException(nameof(speedLabel));
            PrimarySkillLabel = primarySkillLabel ??
                throw new ArgumentNullException(nameof(primarySkillLabel));
            ActiveSkillLabel = activeSkillLabel ??
                throw new ArgumentNullException(nameof(activeSkillLabel));
            Close = close ?? throw new ArgumentNullException(nameof(close));
            MakeLeader = makeLeader ?? throw new ArgumentNullException(nameof(makeLeader));
            AddCompanion = addCompanion ?? throw new ArgumentNullException(nameof(addCompanion));
            RemoveCompanion = removeCompanion ?? throw new ArgumentNullException(nameof(removeCompanion));
            LoadoutLabel = loadoutLabel ?? throw new ArgumentNullException(nameof(loadoutLabel));
            RejectedTeamSize = rejectedTeamSize ?? throw new ArgumentNullException(nameof(rejectedTeamSize));
            RejectedInvalidActor = rejectedInvalidActor ?? throw new ArgumentNullException(nameof(rejectedInvalidActor));
            RejectedInvalidLoadout = rejectedInvalidLoadout ?? throw new ArgumentNullException(nameof(rejectedInvalidLoadout));
            RejectedPersistence = rejectedPersistence ?? throw new ArgumentNullException(nameof(rejectedPersistence));
        }

        public GuildTextSnapshot Header { get; }
        public GuildTextSnapshot GoldLabel { get; }
        public GuildTextSnapshot RankLabel { get; }
        public GuildTextSnapshot UnassignedRank { get; }
        public GuildTextSnapshot LeaderLabel { get; }
        public GuildTextSnapshot LeaderExplanation { get; }
        public GuildTextSnapshot TeamLabel { get; }
        public GuildTextSnapshot RosterLabel { get; }
        public GuildTextSnapshot AvailableHeroLabel { get; }
        public GuildTextSnapshot LevelLabel { get; }
        public GuildTextSnapshot HealthLabel { get; }
        public GuildTextSnapshot SpeedLabel { get; }
        public GuildTextSnapshot PrimarySkillLabel { get; }
        public GuildTextSnapshot ActiveSkillLabel { get; }
        public GuildTextSnapshot Close { get; }
        public GuildTextSnapshot MakeLeader { get; }
        public GuildTextSnapshot AddCompanion { get; }
        public GuildTextSnapshot RemoveCompanion { get; }
        public GuildTextSnapshot LoadoutLabel { get; }
        public GuildTextSnapshot RejectedTeamSize { get; }
        public GuildTextSnapshot RejectedInvalidActor { get; }
        public GuildTextSnapshot RejectedInvalidLoadout { get; }
        public GuildTextSnapshot RejectedPersistence { get; }
    }

    public sealed class GuildHeroLoadoutSnapshot
    {
        public GuildHeroLoadoutSnapshot(string loadoutId, string displayText)
        {
            LoadoutId = Require(loadoutId, nameof(loadoutId));
            DisplayText = Require(displayText, nameof(displayText));
        }

        public string LoadoutId { get; }
        public string DisplayText { get; }

        private static string Require(string value, string parameterName) =>
            !string.IsNullOrWhiteSpace(value)
                ? value
                : throw new ArgumentException("Value cannot be empty.", parameterName);
    }

    public sealed class GuildHeroSkillSnapshot
    {
        public GuildHeroSkillSnapshot(string slotId, string slotDisplayText, string displayName, int level)
        {
            SlotId = Require(slotId, nameof(slotId));
            SlotDisplayText = Require(slotDisplayText, nameof(slotDisplayText));
            DisplayName = Require(displayName, nameof(displayName));
            Level = level > 0 ? level : throw new ArgumentOutOfRangeException(nameof(level));
        }

        public string SlotId { get; }
        public string SlotDisplayText { get; }
        public string DisplayName { get; }
        public int Level { get; }

        private static string Require(string value, string parameterName) =>
            !string.IsNullOrWhiteSpace(value)
                ? value
                : throw new ArgumentException("Value cannot be empty.", parameterName);
    }

    public sealed class GuildHeroSnapshot
    {
        private readonly ReadOnlyCollection<GuildHeroSkillSnapshot> _skills;
        private readonly ReadOnlyCollection<GuildHeroLoadoutSnapshot> _allowedLoadouts;

        public GuildHeroSnapshot(
            string actorId,
            string displayName,
            GuildHeroRole role,
            int level,
            int maximumHealth,
            float movementSpeed,
            IReadOnlyList<GuildHeroSkillSnapshot> skills,
            string loadoutId,
            IReadOnlyList<GuildHeroLoadoutSnapshot> allowedLoadouts)
        {
            ActorId = Require(actorId, nameof(actorId));
            DisplayName = Require(displayName, nameof(displayName));
            if (!Enum.IsDefined(typeof(GuildHeroRole), role))
            {
                throw new ArgumentOutOfRangeException(nameof(role));
            }

            Role = role;
            Level = level > 0 ? level : throw new ArgumentOutOfRangeException(nameof(level));
            MaximumHealth = maximumHealth > 0
                ? maximumHealth
                : throw new ArgumentOutOfRangeException(nameof(maximumHealth));
            MovementSpeed = movementSpeed > 0f
                ? movementSpeed
                : throw new ArgumentOutOfRangeException(nameof(movementSpeed));

            if (skills == null)
            {
                throw new ArgumentNullException(nameof(skills));
            }

            var copy = new GuildHeroSkillSnapshot[skills.Count];
            for (var index = 0; index < copy.Length; index++)
            {
                copy[index] = skills[index] ?? throw new ArgumentException(
                    $"Skill at index {index} is missing.",
                    nameof(skills));
            }

            _skills = Array.AsReadOnly(copy);
            LoadoutId = Require(loadoutId, nameof(loadoutId));
            _allowedLoadouts = CopyLoadouts(allowedLoadouts, nameof(allowedLoadouts));
        }

        public string ActorId { get; }
        public string DisplayName { get; }
        public GuildHeroRole Role { get; }
        public int Level { get; }
        public int MaximumHealth { get; }
        public float MovementSpeed { get; }
        public IReadOnlyList<GuildHeroSkillSnapshot> Skills => _skills;
        public string LoadoutId { get; }
        public IReadOnlyList<GuildHeroLoadoutSnapshot> AllowedLoadouts => _allowedLoadouts;

        private static ReadOnlyCollection<GuildHeroLoadoutSnapshot> CopyLoadouts(
            IReadOnlyList<GuildHeroLoadoutSnapshot> source,
            string parameterName)
        {
            if (source == null || source.Count == 0)
            {
                throw new ArgumentException("At least one loadout is required.", parameterName);
            }

            var copy = new GuildHeroLoadoutSnapshot[source.Count];
            for (var index = 0; index < copy.Length; index++)
            {
                copy[index] = source[index] ?? throw new ArgumentException(
                    $"Loadout at index {index} is missing.", parameterName);
            }

            return Array.AsReadOnly(copy);
        }

        private static string Require(string value, string parameterName) =>
            !string.IsNullOrWhiteSpace(value)
                ? value
                : throw new ArgumentException("Value cannot be empty.", parameterName);
    }

    public sealed class GuildProfileSnapshot
    {
        private readonly ReadOnlyCollection<GuildHeroSnapshot> _companions;
        private readonly ReadOnlyCollection<GuildHeroSnapshot> _roster;

        public GuildProfileSnapshot(
            long gold,
            string rankDisplayText,
            GuildHeroSnapshot leader,
            IReadOnlyList<GuildHeroSnapshot> companions,
            IReadOnlyList<GuildHeroSnapshot> roster,
            GuildProfileTextSnapshot text)
        {
            Gold = gold >= 0 ? gold : throw new ArgumentOutOfRangeException(nameof(gold));
            RankDisplayText = !string.IsNullOrWhiteSpace(rankDisplayText)
                ? rankDisplayText
                : throw new ArgumentException("Rank display cannot be empty.", nameof(rankDisplayText));
            Leader = leader ?? throw new ArgumentNullException(nameof(leader));
            _companions = Copy(companions, nameof(companions));
            _roster = Copy(roster, nameof(roster));
            Text = text ?? throw new ArgumentNullException(nameof(text));
        }

        public long Gold { get; }
        public string RankDisplayText { get; }
        public GuildHeroSnapshot Leader { get; }
        public IReadOnlyList<GuildHeroSnapshot> Companions => _companions;
        public IReadOnlyList<GuildHeroSnapshot> Roster => _roster;
        public GuildProfileTextSnapshot Text { get; }

        private static ReadOnlyCollection<GuildHeroSnapshot> Copy(
            IReadOnlyList<GuildHeroSnapshot> source,
            string parameterName)
        {
            if (source == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            var copy = new GuildHeroSnapshot[source.Count];
            for (var index = 0; index < copy.Length; index++)
            {
                copy[index] = source[index] ?? throw new ArgumentException(
                    $"Hero at index {index} is missing.",
                    parameterName);
            }

            return Array.AsReadOnly(copy);
        }
    }
}
