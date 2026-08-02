using System;
using DungeonTeam.Gameplay.Dungeon.Domain;
using UnityEngine;

namespace DungeonTeam.Gameplay.Dungeon.Runtime.Config
{
    [Serializable]
    public sealed class AuthoredDungeonDefinition
    {
        [SerializeField]
        private string _dungeonId;

        [SerializeField]
        private string _mapAssetId;

        public string DungeonId => _dungeonId;
        public string MapAssetId => _mapAssetId;
    }

    [Serializable]
    public sealed class ChunkedDungeonDefinition
    {
        [SerializeField]
        private string _dungeonId;

        [SerializeField]
        private string _entryChunkId;

        [SerializeField]
        private string _exitChunkId;

        [SerializeField]
        private string[] _mandatoryChunkIds = Array.Empty<string>();

        [SerializeField]
        private string[] _chunkPool = Array.Empty<string>();

        [SerializeField, Min(2)]
        private int _targetChunkCount = 2;

        [SerializeField, Min(1)]
        private int _maxGenerationAttempts = 1;

        public string DungeonId => _dungeonId;
        public string EntryChunkId => _entryChunkId;
        public string ExitChunkId => _exitChunkId;
        public string[] MandatoryChunkIds => _mandatoryChunkIds;
        public string[] ChunkPool => _chunkPool;
        public int TargetChunkCount => _targetChunkCount;
        public int MaxGenerationAttempts => _maxGenerationAttempts;
    }

    [Serializable]
    public sealed class DungeonScenarioDefinition
    {
        [SerializeField]
        private string _scenarioId;

        [SerializeField, Min(0)]
        private int _baseThreatBudget;

        [SerializeField]
        private EnemyCandidateDefinition[] _enemyCandidates =
            Array.Empty<EnemyCandidateDefinition>();

        [SerializeField]
        private InterestPointRuleDefinition[] _interestPointRules =
            Array.Empty<InterestPointRuleDefinition>();

        [SerializeField]
        private string[] _enabledOptionalPlacementIds = Array.Empty<string>();

        [SerializeField]
        private RequiredObjectiveDefinition[] _requiredObjectives =
            Array.Empty<RequiredObjectiveDefinition>();

        public string ScenarioId => _scenarioId;

        internal DungeonScenario ToDomain()
        {
            return new DungeonScenario(
                _scenarioId,
                _baseThreatBudget,
                Convert(_enemyCandidates, definition => definition.ToDomain(), nameof(_enemyCandidates)),
                Convert(
                    _interestPointRules,
                    definition => definition.ToDomain(),
                    nameof(_interestPointRules)),
                Copy(_enabledOptionalPlacementIds, nameof(_enabledOptionalPlacementIds)),
                Convert(
                    _requiredObjectives,
                    definition => definition.ToDomain(),
                    nameof(_requiredObjectives)));
        }

        private static TResult[] Convert<TDefinition, TResult>(
            TDefinition[] definitions,
            Func<TDefinition, TResult> convert,
            string fieldName)
            where TDefinition : class
        {
            if (definitions == null)
            {
                throw new InvalidOperationException($"Scenario field '{fieldName}' cannot be null.");
            }

            var result = new TResult[definitions.Length];
            for (var index = 0; index < definitions.Length; index++)
            {
                var definition = definitions[index] ?? throw new InvalidOperationException(
                    $"Scenario field '{fieldName}' has an empty item at index {index}.");
                result[index] = convert(definition);
            }

            return result;
        }

        private static string[] Copy(string[] source, string fieldName)
        {
            if (source == null)
            {
                throw new InvalidOperationException($"Scenario field '{fieldName}' cannot be null.");
            }

            return (string[])source.Clone();
        }
    }

    [Serializable]
    public sealed class EnemyCandidateDefinition
    {
        [SerializeField]
        private string _enemyId;

        [SerializeField, Min(1)]
        private int _cost = 1;

        [SerializeField, Min(1)]
        private int _weight = 1;

        [SerializeField]
        private string[] _allowedSlotTags = Array.Empty<string>();

        internal EnemyCandidate ToDomain()
        {
            return new EnemyCandidate(_enemyId, _cost, _weight, _allowedSlotTags);
        }
    }

    [Serializable]
    public sealed class InterestPointRuleDefinition
    {
        [SerializeField]
        private string _slotTag;

        [SerializeField, Min(0)]
        private int _minCount;

        [SerializeField, Min(0)]
        private int _maxCount;

        [SerializeField]
        private InterestPointCandidateDefinition[] _candidates =
            Array.Empty<InterestPointCandidateDefinition>();

        internal InterestPointRule ToDomain()
        {
            if (_candidates == null)
            {
                throw new InvalidOperationException("Interest point candidates cannot be null.");
            }

            var candidates = new InterestPointCandidate[_candidates.Length];
            for (var index = 0; index < _candidates.Length; index++)
            {
                var definition = _candidates[index] ?? throw new InvalidOperationException(
                    $"Interest point candidate at index {index} is empty.");
                candidates[index] = definition.ToDomain();
            }

            return new InterestPointRule(_slotTag, _minCount, _maxCount, candidates);
        }
    }

    [Serializable]
    public sealed class InterestPointCandidateDefinition
    {
        [SerializeField]
        private string _interestPointId;

        [SerializeField, Min(1)]
        private int _weight = 1;

        [SerializeField]
        private string _rewardProfileId;

        internal InterestPointCandidate ToDomain()
        {
            return new InterestPointCandidate(_interestPointId, _weight, _rewardProfileId);
        }
    }

    [Serializable]
    public sealed class RequiredObjectiveDefinition
    {
        [SerializeField]
        private string _objectiveId;

        [SerializeField]
        private string _requiredSlotTag;

        internal RequiredObjective ToDomain()
        {
            return new RequiredObjective(_objectiveId, _requiredSlotTag);
        }
    }

    [Serializable]
    public sealed class DungeonDifficultyDefinition
    {
        [SerializeField]
        private string _difficultyId;

        [SerializeField, Min(0f)]
        private float _threatBudgetMultiplier = 1f;

        [SerializeField, Min(0f)]
        private float _interestPointCountMultiplier = 1f;

        [SerializeField, Min(0f)]
        private float _rewardBudgetMultiplier = 1f;

        public string DifficultyId => _difficultyId;

        internal DungeonDifficulty ToDomain()
        {
            return new DungeonDifficulty(
                _difficultyId,
                _threatBudgetMultiplier,
                _interestPointCountMultiplier,
                _rewardBudgetMultiplier);
        }
    }
}
