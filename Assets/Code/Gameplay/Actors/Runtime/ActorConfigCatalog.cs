using System;
using System.Collections.Generic;

namespace DungeonTeam.Gameplay.Actors.Runtime
{
    public sealed class ActorConfigCatalog
    {
        private readonly Dictionary<string, ActorDefinitionConfig> _definitions;

        public ActorConfigCatalog(ActorDefinitionConfig[] definitions)
        {
            if (definitions == null)
            {
                throw new ArgumentNullException(nameof(definitions));
            }

            _definitions = new Dictionary<string, ActorDefinitionConfig>(
                definitions.Length,
                StringComparer.Ordinal);
            for (var index = 0; index < definitions.Length; index++)
            {
                var definition = definitions[index] ?? throw new ArgumentException(
                    $"Actor definition at index {index} is missing.",
                    nameof(definitions));
                definition.Validate(index);
                if (!_definitions.TryAdd(definition.ActorId, definition))
                {
                    throw new ArgumentException(
                        $"Actor ID '{definition.ActorId}' is configured more than once.",
                        nameof(definitions));
                }
            }
        }

        public ActorDefinitionConfig Require(string actorId)
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
    }
}
