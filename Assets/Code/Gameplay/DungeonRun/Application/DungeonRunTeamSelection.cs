using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DungeonTeam.Gameplay.DungeonRun.Application
{
    public sealed class DungeonRunTeamSelection
    {
        private readonly ReadOnlyCollection<string> _companionActorIds;

        public DungeonRunTeamSelection(
            string leaderActorId,
            IReadOnlyList<string> companionActorIds)
        {
            LeaderActorId = RequireId(leaderActorId, nameof(leaderActorId));
            if (companionActorIds == null)
            {
                throw new ArgumentNullException(nameof(companionActorIds));
            }

            var copiedActorIds = new string[companionActorIds.Count];
            var uniqueIds = new HashSet<string>(StringComparer.Ordinal)
            {
                LeaderActorId
            };
            for (var index = 0; index < companionActorIds.Count; index++)
            {
                var actorId = RequireId(
                    companionActorIds[index],
                    nameof(companionActorIds));
                if (!uniqueIds.Add(actorId))
                {
                    throw new ArgumentException(
                        $"Actor ID '{actorId}' is selected more than once.",
                        nameof(companionActorIds));
                }

                copiedActorIds[index] = actorId;
            }

            _companionActorIds = Array.AsReadOnly(copiedActorIds);
        }

        public string LeaderActorId { get; }

        public IReadOnlyList<string> CompanionActorIds => _companionActorIds;

        public int MemberCount => _companionActorIds.Count + 1;

        private static string RequireId(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Actor ID cannot be empty.", parameterName);
            }

            return value;
        }
    }
}
