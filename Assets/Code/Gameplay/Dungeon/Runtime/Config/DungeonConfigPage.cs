using System;
using Code.Configuration;
using UnityEngine;

namespace DungeonTeam.Gameplay.Dungeon.Runtime.Config
{
    [CreateAssetMenu(
        menuName = "DungeonTeam/Gameplay/Dungeon Config",
        fileName = "DungeonConfig")]
    public sealed class DungeonConfigPage : ConfigPage
    {
        [SerializeField]
        private AuthoredDungeonDefinition[] _authoredDungeons =
            Array.Empty<AuthoredDungeonDefinition>();

        [SerializeField]
        private DungeonScenarioDefinition[] _scenarios =
            Array.Empty<DungeonScenarioDefinition>();

        [SerializeField]
        private DungeonDifficultyDefinition[] _difficulties =
            Array.Empty<DungeonDifficultyDefinition>();

        internal AuthoredDungeonDefinition RequireAuthoredDungeon(string dungeonId)
        {
            return RequireById(
                _authoredDungeons,
                dungeonId,
                definition => definition.DungeonId,
                "authored dungeon");
        }

        internal DungeonScenarioDefinition RequireScenario(string scenarioId)
        {
            return RequireById(
                _scenarios,
                scenarioId,
                definition => definition.ScenarioId,
                "scenario");
        }

        internal DungeonDifficultyDefinition RequireDifficulty(string difficultyId)
        {
            return RequireById(
                _difficulties,
                difficultyId,
                definition => definition.DifficultyId,
                "difficulty");
        }

        private TDefinition RequireById<TDefinition>(
            TDefinition[] definitions,
            string id,
            Func<TDefinition, string> getId,
            string definitionName)
            where TDefinition : class
        {
            if (definitions == null)
            {
                throw new InvalidOperationException(
                    $"Dungeon config '{name}' has no {definitionName} array.");
            }

            TDefinition match = null;
            for (var index = 0; index < definitions.Length; index++)
            {
                var definition = definitions[index];
                if (definition == null)
                {
                    throw new InvalidOperationException(
                        $"Dungeon config '{name}' has an empty {definitionName} at index {index}.");
                }

                if (!string.Equals(getId(definition), id, StringComparison.Ordinal))
                {
                    continue;
                }

                if (match != null)
                {
                    throw new InvalidOperationException(
                        $"Dungeon config '{name}' contains duplicate {definitionName} ID '{id}'.");
                }

                match = definition;
            }

            return match ?? throw new InvalidOperationException(
                $"Dungeon config '{name}' does not contain {definitionName} ID '{id}'.");
        }
    }
}
