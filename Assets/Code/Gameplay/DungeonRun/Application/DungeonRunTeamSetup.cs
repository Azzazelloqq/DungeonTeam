using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DungeonTeam.Gameplay.DungeonRun.Application
{
    public readonly struct DungeonRunTeamMemberOption
    {
        public DungeonRunTeamMemberOption(string actorId, string displayName)
        {
            ActorId = RequireValue(actorId, nameof(actorId));
            DisplayName = RequireValue(displayName, nameof(displayName));
        }

        public string ActorId { get; }

        public string DisplayName { get; }

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
        private readonly HashSet<string> _allowedActorIds;

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
            _allowedActorIds = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < members.Count; index++)
            {
                var member = members[index];
                if (!_allowedActorIds.Add(member.ActorId))
                {
                    throw new ArgumentException(
                        $"Actor ID '{member.ActorId}' is configured more than once in the team roster.",
                        nameof(members));
                }

                copiedMembers[index] = member;
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
            return GetValidationError(selection) == null;
        }

        public void RequireValid(DungeonRunTeamSelection selection)
        {
            var error = GetValidationError(selection);
            if (error != null)
            {
                throw new ArgumentException(error, nameof(selection));
            }
        }

        private string GetValidationError(DungeonRunTeamSelection selection)
        {
            if (selection == null)
            {
                return "Team selection is required.";
            }

            if (selection.MemberCount < MinimumTeamSize ||
                selection.MemberCount > MaximumTeamSize)
            {
                return $"Team size {selection.MemberCount} is outside the configured range " +
                       $"{MinimumTeamSize}..{MaximumTeamSize}.";
            }

            if (!_allowedActorIds.Contains(selection.LeaderActorId))
            {
                return $"Actor ID '{selection.LeaderActorId}' is not available for this run.";
            }

            for (var index = 0; index < selection.CompanionActorIds.Count; index++)
            {
                var actorId = selection.CompanionActorIds[index];
                if (!_allowedActorIds.Contains(actorId))
                {
                    return $"Actor ID '{actorId}' is not available for this run.";
                }
            }

            return null;
        }
    }
}
