using System;
using System.Collections.Generic;
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

        [SerializeField]
        private ActorLevelDefinitionConfig[] _levels =
            Array.Empty<ActorLevelDefinitionConfig>();

        public ActorDefinitionConfig(
            string actorId,
            string displayName,
            ActorLevelDefinitionConfig[] levels)
        {
            _actorId = actorId;
            _displayName = displayName;
            _levels = levels;
        }

        public string ActorId => _actorId;

        public string DisplayName => _displayName;

        internal ActorConfigDefinition ToDefinition()
        {
            var levels = new ActorRuntimeDefinition[_levels.Length];
            for (var index = 0; index < _levels.Length; index++)
            {
                var definition = _levels[index];
                levels[index] = new ActorRuntimeDefinition(
                    _actorId,
                    definition.Level,
                    definition.MaximumHealth,
                    definition.MovementSpeed);
            }

            return new ActorConfigDefinition(
                _actorId,
                _displayName,
                levels);
        }

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

            if (_levels == null || _levels.Length == 0)
            {
                throw new ArgumentException(
                    $"{location} ('{_actorId}') requires at least one level.");
            }

            var levels = new HashSet<int>();
            for (var levelIndex = 0; levelIndex < _levels.Length; levelIndex++)
            {
                var level = _levels[levelIndex] ?? throw new ArgumentException(
                    $"{location} ('{_actorId}') has a missing level at index {levelIndex}.");
                level.Validate(_actorId);
                if (!levels.Add(level.Level))
                {
                    throw new ArgumentException(
                        $"{location} ('{_actorId}') contains level {level.Level} more than once.");
                }
            }
        }
    }

    [Serializable]
    public sealed class ActorLevelDefinitionConfig
    {
        [SerializeField, Min(1)]
        private int _level = 1;

        [SerializeField, Min(1)]
        private int _maximumHealth = 1;

        [SerializeField, Min(0.1f)]
        private float _movementSpeed = 1f;

        public ActorLevelDefinitionConfig(
            int level,
            int maximumHealth,
            float movementSpeed)
        {
            _level = level;
            _maximumHealth = maximumHealth;
            _movementSpeed = movementSpeed;
        }

        public int Level => _level;
        public int MaximumHealth => _maximumHealth;
        public float MovementSpeed => _movementSpeed;
        internal void Validate(string actorId)
        {
            if (_level <= 0 ||
                _maximumHealth <= 0 ||
                _movementSpeed <= 0f)
            {
                throw new ArgumentException(
                    $"Actor '{actorId}' level values must be positive.");
            }
        }
    }
}
