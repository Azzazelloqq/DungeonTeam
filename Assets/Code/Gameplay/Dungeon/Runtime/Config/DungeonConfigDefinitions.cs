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
    public sealed class ProceduralDungeonDefinition
    {
        [SerializeField]
        private string _dungeonId;

        [SerializeField]
        private string _tileSetId;

        [SerializeField, Min(1)]
        private int _width = 1;

        [SerializeField, Min(1)]
        private int _height = 1;

        [SerializeField, Min(3)]
        private int _targetCellCount = 3;

        [SerializeField, Min(2)]
        private int _mainRouteCellCount = 2;

        [SerializeField, Min(1)]
        private int _maxGenerationAttempts = 1;

        public string DungeonId => _dungeonId;
        public string TileSetId => _tileSetId;
        public int Width => _width;
        public int Height => _height;
        public int TargetCellCount => _targetCellCount;
        public int MainRouteCellCount => _mainRouteCellCount;
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

        [SerializeField]
        private DungeonRewardProfileDefinition[] _rewardProfiles =
            Array.Empty<DungeonRewardProfileDefinition>();

        [SerializeField]
        private EnemyRewardRuleDefinition[] _enemyRewardRules =
            Array.Empty<EnemyRewardRuleDefinition>();

        [SerializeField]
        private string _completionRewardProfileId;

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
                    nameof(_requiredObjectives)),
                Convert(
                    _rewardProfiles,
                    definition => definition.ToDomain(),
                    nameof(_rewardProfiles)),
                Convert(
                    _enemyRewardRules,
                    definition => definition.ToDomain(),
                    nameof(_enemyRewardRules)),
                _completionRewardProfileId);
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
    public sealed class DungeonRewardProfileDefinition
    {
        [SerializeField]
        private string _profileId;

        [SerializeField]
        private DungeonRewardEntryDefinition[] _entries =
            Array.Empty<DungeonRewardEntryDefinition>();

        internal DungeonRewardProfile ToDomain()
        {
            if (_entries == null)
            {
                throw new InvalidOperationException(
                    $"Reward profile '{_profileId}' entries cannot be null.");
            }

            var entries = new DungeonRewardEntry[_entries.Length];
            for (var index = 0; index < _entries.Length; index++)
            {
                var entry = _entries[index] ?? throw new InvalidOperationException(
                    $"Reward profile '{_profileId}' has an empty entry at index {index}.");
                entries[index] = entry.ToDomain();
            }

            return new DungeonRewardProfile(_profileId, entries);
        }
    }

    [Serializable]
    public sealed class DungeonRewardEntryDefinition
    {
        [SerializeField]
        private string _rewardId;

        [SerializeField, Min(1)]
        private int _amount = 1;

        internal DungeonRewardEntry ToDomain()
        {
            return new DungeonRewardEntry(_rewardId, _amount);
        }
    }

    [Serializable]
    public sealed class EnemyRewardRuleDefinition
    {
        [SerializeField]
        private string _enemyId;

        [SerializeField]
        private string _rewardProfileId;

        internal EnemyRewardRule ToDomain()
        {
            return new EnemyRewardRule(_enemyId, _rewardProfileId);
        }
    }

    [Serializable]
    public sealed class EnemyCandidateDefinition
    {
        [SerializeField]
        private string _enemyId;

        [SerializeField]
        private string _behaviorId;

        [SerializeField]
        private string _loadoutId;

        [SerializeField, Min(1)]
        private int _actorLevel = 1;

        [SerializeField, Min(1)]
        private int _cost = 1;

        [SerializeField, Min(1)]
        private int _weight = 1;

        [SerializeField]
        private string[] _allowedSlotTags = Array.Empty<string>();

        internal EnemyCandidate ToDomain()
        {
            return new EnemyCandidate(
                _enemyId,
                _behaviorId,
                _loadoutId,
                _actorLevel,
                _cost,
                _weight,
                _allowedSlotTags);
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
