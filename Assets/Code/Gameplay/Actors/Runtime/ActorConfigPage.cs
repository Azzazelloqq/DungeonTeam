using System;
using Code.Configuration;
using UnityEngine;

namespace DungeonTeam.Gameplay.Actors.Runtime
{
    [CreateAssetMenu(
        menuName = "DungeonTeam/Gameplay/Actor Config",
        fileName = "ActorConfig")]
    public sealed class ActorConfigPage : ConfigPage
    {
        [SerializeField]
        private ActorDefinitionConfig[] _actors = Array.Empty<ActorDefinitionConfig>();

        public ActorConfigCatalog CreateCatalog()
        {
            return new ActorConfigCatalog(_actors);
        }
    }

    [Serializable]
    public sealed class ActorDefinitionConfig
    {
        [SerializeField]
        private string _actorId;

        [SerializeField]
        private string _displayName;

        [SerializeField, Min(1)]
        private int _maximumHealth = 1;

        [SerializeField, Min(0.1f)]
        private float _movementSpeed = 1f;

        public ActorDefinitionConfig(
            string actorId,
            string displayName,
            int maximumHealth,
            float movementSpeed)
        {
            _actorId = actorId;
            _displayName = displayName;
            _maximumHealth = maximumHealth;
            _movementSpeed = movementSpeed;
        }

        public string ActorId => _actorId;

        public string DisplayName => _displayName;

        internal int MaximumHealth => _maximumHealth;

        internal float MovementSpeed => _movementSpeed;

        internal void Validate(int index)
        {
            var location = $"Actor definition at index {index}";
            if (string.IsNullOrWhiteSpace(_actorId))
            {
                throw new ArgumentException($"{location} has an empty actor ID.");
            }

            if (string.IsNullOrWhiteSpace(_displayName))
            {
                throw new ArgumentException(
                    $"{location} ('{_actorId}') has an empty display name.");
            }

            if (_maximumHealth <= 0)
            {
                throw new ArgumentException(
                    $"{location} ('{_actorId}') requires positive health.");
            }

            if (_movementSpeed <= 0f)
            {
                throw new ArgumentException(
                    $"{location} ('{_actorId}') requires positive movement speed.");
            }
        }
    }
}
