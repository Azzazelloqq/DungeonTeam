using System;
using Code.Configuration;
using UnityEngine;

namespace DungeonTeam.Gameplay.Rewards.Runtime
{
    [CreateAssetMenu(
        menuName = "DungeonTeam/Gameplay/Reward Config",
        fileName = "RewardConfig")]
    public sealed class RewardConfigPage : ConfigPage
    {
        [SerializeField]
        private RewardDefinitionConfig[] _rewards = Array.Empty<RewardDefinitionConfig>();

        public RewardCatalog CreateCatalog()
        {
            if (_rewards == null)
            {
                throw new InvalidOperationException(
                    $"Reward config '{name}' has no reward definitions array.");
            }

            var definitions = new RewardDefinition[_rewards.Length];
            for (var index = 0; index < _rewards.Length; index++)
            {
                var definition = _rewards[index] ?? throw new InvalidOperationException(
                    $"Reward config '{name}' has an empty definition at index {index}.");
                definitions[index] = definition.ToRuntime();
            }

            return new RewardCatalog(definitions);
        }
    }

    [Serializable]
    public sealed class RewardDefinitionConfig
    {
        [SerializeField]
        private string _rewardId;

        [SerializeField]
        private string _displayName;

        internal RewardDefinition ToRuntime()
        {
            return new RewardDefinition(_rewardId, _displayName);
        }
    }
}
