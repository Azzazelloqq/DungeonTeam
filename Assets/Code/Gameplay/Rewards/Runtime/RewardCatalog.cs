using System;
using System.Collections.Generic;

namespace DungeonTeam.Gameplay.Rewards.Runtime
{
    public sealed class RewardCatalog
    {
        private readonly Dictionary<string, RewardDefinition> _definitions;

        public RewardCatalog(RewardDefinition[] definitions)
        {
            if (definitions == null)
            {
                throw new ArgumentNullException(nameof(definitions));
            }

            _definitions = new Dictionary<string, RewardDefinition>(
                definitions.Length,
                StringComparer.Ordinal);
            for (var index = 0; index < definitions.Length; index++)
            {
                var definition = definitions[index] ?? throw new ArgumentException(
                    $"Reward definition at index {index} is missing.",
                    nameof(definitions));
                if (!_definitions.TryAdd(definition.RewardId, definition))
                {
                    throw new ArgumentException(
                        $"Reward ID '{definition.RewardId}' is configured more than once.",
                        nameof(definitions));
                }
            }
        }

        public RewardDefinition Require(string rewardId)
        {
            if (string.IsNullOrWhiteSpace(rewardId))
            {
                throw new ArgumentException("Reward ID cannot be empty.", nameof(rewardId));
            }

            if (!_definitions.TryGetValue(rewardId, out var definition))
            {
                throw new InvalidOperationException(
                    $"Reward catalog does not contain reward ID '{rewardId}'.");
            }

            return definition;
        }
    }
}
