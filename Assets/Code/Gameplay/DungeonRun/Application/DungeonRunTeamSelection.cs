using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DungeonTeam.Gameplay.DungeonRun.Application
{
    public readonly struct DungeonRunActorSelection
    {
        public DungeonRunActorSelection(string actorId, int level)
        {
            ActorId = !string.IsNullOrWhiteSpace(actorId)
                ? actorId
                : throw new ArgumentException("Actor ID cannot be empty.", nameof(actorId));
            Level = level > 0
                ? level
                : throw new ArgumentOutOfRangeException(nameof(level));
        }

        public string ActorId { get; }
        public int Level { get; }
    }

    public sealed class DungeonRunTeamSelection
    {
        private readonly ReadOnlyCollection<DungeonRunActorSelection> _companions;
        private readonly ReadOnlyCollection<string> _companionActorIds;

        public DungeonRunTeamSelection(
            DungeonRunActorSelection leader,
            IReadOnlyList<DungeonRunActorSelection> companions)
        {
            Leader = leader;
            if (companions == null)
            {
                throw new ArgumentNullException(nameof(companions));
            }

            var copiedSelections = new DungeonRunActorSelection[companions.Count];
            var uniqueIds = new HashSet<string>(StringComparer.Ordinal)
            {
                Leader.ActorId
            };
            for (var index = 0; index < companions.Count; index++)
            {
                var selection = companions[index];
                if (!uniqueIds.Add(selection.ActorId))
                {
                    throw new ArgumentException(
                        $"Actor ID '{selection.ActorId}' is selected more than once.",
                        nameof(companions));
                }

                copiedSelections[index] = selection;
            }

            _companions = Array.AsReadOnly(copiedSelections);
            var copiedIds = new string[copiedSelections.Length];
            for (var index = 0; index < copiedSelections.Length; index++)
            {
                copiedIds[index] = copiedSelections[index].ActorId;
            }

            _companionActorIds = Array.AsReadOnly(copiedIds);
        }

        public DungeonRunTeamSelection(
            string leaderActorId,
            IReadOnlyList<string> companionActorIds)
            : this(
                new DungeonRunActorSelection(leaderActorId, 1),
                ToLevelOneSelections(companionActorIds))
        {
        }

        public DungeonRunActorSelection Leader { get; }

        public IReadOnlyList<DungeonRunActorSelection> Companions => _companions;

        public IReadOnlyList<string> CompanionActorIds => _companionActorIds;

        public string LeaderActorId => Leader.ActorId;

        public int MemberCount => _companions.Count + 1;

        private static IReadOnlyList<DungeonRunActorSelection> ToLevelOneSelections(
            IReadOnlyList<string> actorIds)
        {
            if (actorIds == null)
            {
                throw new ArgumentNullException(nameof(actorIds));
            }

            var selections = new DungeonRunActorSelection[actorIds.Count];
            for (var index = 0; index < actorIds.Count; index++)
            {
                selections[index] = new DungeonRunActorSelection(actorIds[index], 1);
            }

            return selections;
        }
    }
}
