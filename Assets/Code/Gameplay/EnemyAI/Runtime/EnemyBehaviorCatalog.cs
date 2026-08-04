using System;
using System.Collections.Generic;

namespace DungeonTeam.Gameplay.EnemyAI.Runtime
{
    public sealed class EnemyBehaviorCatalog
    {
        private readonly Dictionary<string, EnemyAiSettings> _settingsById;

        public EnemyBehaviorCatalog(EnemyBehaviorDefinition[] definitions)
        {
            if (definitions == null)
            {
                throw new ArgumentNullException(nameof(definitions));
            }

            _settingsById = new Dictionary<string, EnemyAiSettings>(
                definitions.Length,
                StringComparer.Ordinal);
            for (var index = 0; index < definitions.Length; index++)
            {
                var definition = definitions[index] ?? throw new ArgumentException(
                    $"Enemy behavior definition at index {index} is missing.",
                    nameof(definitions));
                definition.Validate(index);
                if (!_settingsById.TryAdd(definition.BehaviorId, definition.Settings))
                {
                    throw new ArgumentException(
                        $"Enemy behavior ID '{definition.BehaviorId}' is configured more than once.",
                        nameof(definitions));
                }
            }
        }

        public EnemyAiSettings Require(string behaviorId)
        {
            if (string.IsNullOrWhiteSpace(behaviorId))
            {
                throw new ArgumentException(
                    "Enemy behavior ID cannot be empty.",
                    nameof(behaviorId));
            }

            if (!_settingsById.TryGetValue(behaviorId, out var settings))
            {
                throw new InvalidOperationException(
                    $"Enemy behavior config does not contain behavior ID '{behaviorId}'.");
            }

            return settings;
        }
    }
}
