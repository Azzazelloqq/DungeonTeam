using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DungeonTeam.Gameplay.DungeonRun.Application
{
    public enum DungeonRunTeamValidationFailure
    {
        None = 0,
        SelectionMissing = 1,
        TeamSizeOutOfRange = 2,
        ActorUnavailable = 3,
        LevelUnavailable = 4,
        LoadoutUnavailable = 5
    }

    public readonly struct DungeonRunTeamMemberOption
    {
        private readonly ReadOnlyCollection<int> _availableLevels;
        private readonly ReadOnlyCollection<string> _availableLoadoutIds;

        public DungeonRunTeamMemberOption(
            string actorId,
            string displayName,
            IReadOnlyList<int> availableLevels,
            IReadOnlyList<string> availableLoadoutIds)
        {
            ActorId = RequireValue(actorId, nameof(actorId));
            DisplayName = RequireValue(displayName, nameof(displayName));
            if (availableLevels == null || availableLevels.Count == 0)
            {
                throw new ArgumentException(
                    "At least one actor level is required.",
                    nameof(availableLevels));
            }

            var copiedLevels = new int[availableLevels.Count];
            var uniqueLevels = new HashSet<int>();
            for (var index = 0; index < availableLevels.Count; index++)
            {
                var level = availableLevels[index];
                if (level <= 0 || !uniqueLevels.Add(level))
                {
                    throw new ArgumentException(
                        "Actor levels must be positive and unique.",
                        nameof(availableLevels));
                }

                copiedLevels[index] = level;
            }

            Array.Sort(copiedLevels);
            _availableLevels = Array.AsReadOnly(copiedLevels);
            if (availableLoadoutIds == null || availableLoadoutIds.Count == 0)
            {
                throw new ArgumentException(
                    "At least one combat loadout is required.",
                    nameof(availableLoadoutIds));
            }

            var copiedLoadoutIds = new string[availableLoadoutIds.Count];
            var uniqueLoadoutIds = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < copiedLoadoutIds.Length; index++)
            {
                var loadoutId = availableLoadoutIds[index];
                if (string.IsNullOrWhiteSpace(loadoutId) || !uniqueLoadoutIds.Add(loadoutId))
                {
                    throw new ArgumentException(
                        "Combat loadout IDs must be non-empty and unique.",
                        nameof(availableLoadoutIds));
                }

                copiedLoadoutIds[index] = loadoutId;
            }

            _availableLoadoutIds = Array.AsReadOnly(copiedLoadoutIds);
        }

        public string ActorId { get; }

        public string DisplayName { get; }

        public IReadOnlyList<int> AvailableLevels => _availableLevels;
        public IReadOnlyList<string> AvailableLoadoutIds => _availableLoadoutIds;

        public bool SupportsLevel(int level)
        {
            for (var index = 0; index < _availableLevels.Count; index++)
            {
                if (_availableLevels[index] == level)
                {
                    return true;
                }
            }

            return false;
        }

        public bool SupportsLoadout(string loadoutId)
        {
            for (var index = 0; index < _availableLoadoutIds.Count; index++)
            {
                if (string.Equals(
                        _availableLoadoutIds[index],
                        loadoutId,
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static string RequireValue(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be empty.", parameterName);
            }

            return value;
        }
    }

    public sealed class DungeonRunTeamSetup
    {
        private readonly ReadOnlyCollection<DungeonRunTeamMemberOption> _members;
        private readonly Dictionary<string, DungeonRunTeamMemberOption> _membersByActorId;

        public DungeonRunTeamSetup(
            IReadOnlyList<DungeonRunTeamMemberOption> members,
            int minimumTeamSize,
            int maximumTeamSize,
            DungeonRunTeamSelection defaultSelection)
        {
            if (members == null)
            {
                throw new ArgumentNullException(nameof(members));
            }

            if (members.Count == 0)
            {
                throw new ArgumentException(
                    "At least one team member option is required.",
                    nameof(members));
            }

            if (minimumTeamSize <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(minimumTeamSize),
                    "Minimum team size must be positive.");
            }

            if (maximumTeamSize < minimumTeamSize || maximumTeamSize > members.Count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumTeamSize),
                    "Maximum team size must be within the configured roster and not less than the minimum.");
            }

            var copiedMembers = new DungeonRunTeamMemberOption[members.Count];
            _membersByActorId = new Dictionary<string, DungeonRunTeamMemberOption>(
                members.Count,
                StringComparer.Ordinal);
            for (var index = 0; index < members.Count; index++)
            {
                var member = members[index];
                if (_membersByActorId.ContainsKey(member.ActorId))
                {
                    throw new ArgumentException(
                        $"Actor ID '{member.ActorId}' is configured more than once in the team roster.",
                        nameof(members));
                }

                copiedMembers[index] = member;
                _membersByActorId.Add(member.ActorId, member);
            }

            _members = Array.AsReadOnly(copiedMembers);

            MinimumTeamSize = minimumTeamSize;
            MaximumTeamSize = maximumTeamSize;
            DefaultSelection = defaultSelection ??
                throw new ArgumentNullException(nameof(defaultSelection));
            RequireValid(DefaultSelection);
        }

        public IReadOnlyList<DungeonRunTeamMemberOption> Members => _members;

        public int MinimumTeamSize { get; }

        public int MaximumTeamSize { get; }

        public DungeonRunTeamSelection DefaultSelection { get; }

        public bool IsValid(DungeonRunTeamSelection selection)
        {
            return GetValidationError(selection, out _) == null;
        }

        public bool TryValidate(
            DungeonRunTeamSelection selection,
            out DungeonRunTeamValidationFailure failure)
        {
            return GetValidationError(selection, out failure) == null;
        }

        public void RequireValid(DungeonRunTeamSelection selection)
        {
            var error = GetValidationError(selection, out _);
            if (error != null)
            {
                throw new ArgumentException(error, nameof(selection));
            }
        }

        private string GetValidationError(
            DungeonRunTeamSelection selection,
            out DungeonRunTeamValidationFailure failure)
        {
            if (selection == null)
            {
                failure = DungeonRunTeamValidationFailure.SelectionMissing;
                return "Team selection is required.";
            }

            if (selection.MemberCount < MinimumTeamSize ||
                selection.MemberCount > MaximumTeamSize)
            {
                failure = DungeonRunTeamValidationFailure.TeamSizeOutOfRange;
                return $"Team size {selection.MemberCount} is outside the configured range " +
                       $"{MinimumTeamSize}..{MaximumTeamSize}.";
            }

            if (!IsAllowed(selection.Leader, out var leaderError, out failure))
            {
                return leaderError;
            }

            for (var index = 0; index < selection.Companions.Count; index++)
            {
                if (!IsAllowed(
                        selection.Companions[index],
                        out var companionError,
                        out failure))
                {
                    return companionError;
                }
            }

            failure = DungeonRunTeamValidationFailure.None;
            return null;
        }

        private bool IsAllowed(
            DungeonRunActorSelection selection,
            out string error,
            out DungeonRunTeamValidationFailure failure)
        {
            if (!_membersByActorId.TryGetValue(selection.ActorId, out var member))
            {
                error = $"Actor ID '{selection.ActorId}' is not available for this run.";
                failure = DungeonRunTeamValidationFailure.ActorUnavailable;
                return false;
            }

            if (!member.SupportsLevel(selection.Level))
            {
                error = $"Actor ID '{selection.ActorId}' level {selection.Level} is not " +
                        "available for this run.";
                failure = DungeonRunTeamValidationFailure.LevelUnavailable;
                return false;
            }

            if (!member.SupportsLoadout(selection.LoadoutId))
            {
                error = $"Loadout ID '{selection.LoadoutId}' is not available for actor " +
                        $"'{selection.ActorId}' in this run.";
                failure = DungeonRunTeamValidationFailure.LoadoutUnavailable;
                return false;
            }

            error = null;
            failure = DungeonRunTeamValidationFailure.None;
            return true;
        }
    }
}
