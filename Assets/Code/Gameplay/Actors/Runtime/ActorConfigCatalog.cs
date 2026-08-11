using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DungeonTeam.Gameplay.Actors.Runtime
{
    public sealed class ActorConfigCatalog
    {
        private readonly Dictionary<string, ActorConfigDefinition> _definitions;
        private readonly ReadOnlyCollection<ActorConfigDefinition> _allDefinitions;

        public ActorConfigCatalog(ActorDefinitionConfig[] definitions)
        {
            if (definitions == null)
            {
                throw new ArgumentNullException(nameof(definitions));
            }

            _definitions = new Dictionary<string, ActorConfigDefinition>(
                definitions.Length,
                StringComparer.Ordinal);
            var allDefinitions = new ActorConfigDefinition[definitions.Length];
            for (var index = 0; index < definitions.Length; index++)
            {
                var definition = definitions[index] ?? throw new ArgumentException(
                    $"Actor definition at index {index} is missing.",
                    nameof(definitions));
                definition.Validate(index);
                var immutableDefinition = definition.ToDefinition();
                if (!_definitions.TryAdd(immutableDefinition.ActorId, immutableDefinition))
                {
                    throw new ArgumentException(
                        $"Actor ID '{immutableDefinition.ActorId}' is configured more than once.",
                        nameof(definitions));
                }

                allDefinitions[index] = immutableDefinition;
            }

            _allDefinitions = Array.AsReadOnly(allDefinitions);
        }

        public IReadOnlyList<ActorConfigDefinition> Definitions => _allDefinitions;

        public ActorConfigDefinition Require(string actorId)
        {
            if (string.IsNullOrWhiteSpace(actorId))
            {
                throw new ArgumentException("Actor ID cannot be empty.", nameof(actorId));
            }

            if (!_definitions.TryGetValue(actorId, out var definition))
            {
                throw new InvalidOperationException(
                    $"Actor config does not contain actor ID '{actorId}'.");
            }

            return definition;
        }

        public ActorRuntimeDefinition Resolve(string actorId, int level)
        {
            return Require(actorId).RequireLevel(level);
        }
    }
}
