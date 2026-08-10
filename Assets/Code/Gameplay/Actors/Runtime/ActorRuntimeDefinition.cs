using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DungeonTeam.Gameplay.Actors.Runtime
{
    public sealed class ActorConfigDefinition
    {
        private readonly ReadOnlyCollection<ActorRuntimeDefinition> _levels;
        private readonly Dictionary<int, ActorRuntimeDefinition> _levelsByNumber;

        public ActorConfigDefinition(
            string actorId,
            string displayName,
            IReadOnlyList<ActorRuntimeDefinition> levels)
        {
            ActorId = !string.IsNullOrWhiteSpace(actorId)
                ? actorId
                : throw new ArgumentException("Actor ID cannot be empty.", nameof(actorId));
            DisplayName = !string.IsNullOrWhiteSpace(displayName)
                ? displayName
                : throw new ArgumentException(
                    "Display name cannot be empty.",
                    nameof(displayName));
            if (levels == null || levels.Count == 0)
            {
                throw new ArgumentException("Actor levels are required.", nameof(levels));
            }

            var copiedLevels = new ActorRuntimeDefinition[levels.Count];
            _levelsByNumber = new Dictionary<int, ActorRuntimeDefinition>(levels.Count);
            for (var index = 0; index < levels.Count; index++)
            {
                var level = levels[index];
                if (!string.Equals(level.ActorId, ActorId, StringComparison.Ordinal))
                {
                    throw new ArgumentException(
                        "Actor level belongs to another actor.",
                        nameof(levels));
                }

                if (!_levelsByNumber.TryAdd(level.Level, level))
                {
                    throw new ArgumentException(
                        $"Actor '{ActorId}' contains level {level.Level} more than once.",
                        nameof(levels));
                }

                copiedLevels[index] = level;
            }

            Array.Sort(copiedLevels, (first, second) => first.Level.CompareTo(second.Level));
            _levels = Array.AsReadOnly(copiedLevels);
        }

        public string ActorId { get; }
        public string DisplayName { get; }
        public IReadOnlyList<ActorRuntimeDefinition> Levels => _levels;

        public ActorRuntimeDefinition RequireLevel(int level)
        {
            if (!_levelsByNumber.TryGetValue(level, out var definition))
            {
                throw new InvalidOperationException(
                    $"Actor '{ActorId}' does not contain level {level}.");
            }

            return definition;
        }
    }

    public readonly struct ActorRuntimeDefinition
    {
        public ActorRuntimeDefinition(
            string actorId,
            int level,
            int maximumHealth,
            float movementSpeed)
        {
            ActorId = !string.IsNullOrWhiteSpace(actorId)
                ? actorId
                : throw new ArgumentException("Actor ID cannot be empty.", nameof(actorId));
            Level = level > 0 ? level : throw new ArgumentOutOfRangeException(nameof(level));
            MaximumHealth = maximumHealth > 0
                ? maximumHealth
                : throw new ArgumentOutOfRangeException(nameof(maximumHealth));
            MovementSpeed = movementSpeed > 0f
                ? movementSpeed
                : throw new ArgumentOutOfRangeException(nameof(movementSpeed));
        }

        public string ActorId { get; }
        public int Level { get; }
        public int MaximumHealth { get; }
        public float MovementSpeed { get; }
    }
}
