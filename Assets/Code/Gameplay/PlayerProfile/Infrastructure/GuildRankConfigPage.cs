using System;
using Code.Configuration;
using DungeonTeam.Gameplay.PlayerProfile.Application;
using UnityEngine;

namespace DungeonTeam.Gameplay.PlayerProfile.Infrastructure
{
    [CreateAssetMenu(
        menuName = "DungeonTeam/Gameplay/Guild Rank Config",
        fileName = "GuildRankConfig")]
    public sealed class GuildRankConfigPage : ConfigPage
    {
        [SerializeField]
        private GuildRankDefinitionConfig[] _ranks = Array.Empty<GuildRankDefinitionConfig>();

        public GuildRankCatalog CreateCatalog()
        {
            if (_ranks == null)
            {
                throw new InvalidOperationException("Guild rank definitions cannot be null.");
            }

            var definitions = new GuildRankDefinition[_ranks.Length];
            for (var index = 0; index < definitions.Length; index++)
            {
                var definition = _ranks[index] ?? throw new InvalidOperationException(
                    $"Guild rank definition at index {index} is missing.");
                definitions[index] = definition.ToDefinition();
            }

            return new GuildRankCatalog(definitions);
        }
    }

    [Serializable]
    public sealed class GuildRankDefinitionConfig
    {
        [SerializeField]
        private string _rankId;

        [SerializeField]
        private string _displayName;

        [SerializeField, Min(0)]
        private long _promotionCost;

        internal GuildRankDefinition ToDefinition() => new(
            _rankId,
            _displayName,
            _promotionCost);
    }
}
