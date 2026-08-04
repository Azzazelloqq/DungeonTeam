using System;
using Code.Configuration;
using UnityEngine;

namespace DungeonTeam.Gameplay.EnemyAI.Runtime
{
    [CreateAssetMenu(
        menuName = "DungeonTeam/Gameplay/Enemy Behavior Config",
        fileName = "EnemyBehaviorConfig")]
    public sealed class EnemyBehaviorConfigPage : ConfigPage
    {
        [SerializeField]
        private EnemyBehaviorDefinition[] _behaviors =
            Array.Empty<EnemyBehaviorDefinition>();

        public EnemyBehaviorCatalog CreateCatalog()
        {
            return new EnemyBehaviorCatalog(_behaviors);
        }
    }

    [Serializable]
    public sealed class EnemyBehaviorDefinition
    {
        [SerializeField]
        private string _behaviorId;

        [SerializeField]
        private EnemyAiSettings _settings = new();

        public EnemyBehaviorDefinition(string behaviorId, EnemyAiSettings settings)
        {
            _behaviorId = behaviorId;
            _settings = settings;
        }

        public string BehaviorId => _behaviorId;

        internal EnemyAiSettings Settings => _settings;

        internal void Validate(int index)
        {
            var location = $"Enemy behavior definition at index {index}";
            if (string.IsNullOrWhiteSpace(_behaviorId))
            {
                throw new ArgumentException($"{location} has an empty behavior ID.");
            }

            if (_settings == null)
            {
                throw new ArgumentException(
                    $"{location} ('{_behaviorId}') has no AI settings.");
            }

            _settings.Validate();
        }
    }
}
