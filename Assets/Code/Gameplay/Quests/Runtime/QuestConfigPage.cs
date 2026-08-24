using System;
using Code.Configuration;
using DungeonTeam.Gameplay.Quests.Domain;
using UnityEngine;

namespace DungeonTeam.Gameplay.Quests.Runtime
{
    [CreateAssetMenu(menuName = "DungeonTeam/Gameplay/Quest Config", fileName = "QuestConfig")]
    public sealed class QuestConfigPage : ConfigPage
    {
        [SerializeField] private QuestDefinitionConfig[] _quests = Array.Empty<QuestDefinitionConfig>();
        [SerializeField] private QuestChainDefinitionConfig[] _chains = Array.Empty<QuestChainDefinitionConfig>();
        public QuestCatalog CreateCatalog()
        {
            var source = _quests ?? throw new InvalidOperationException("Quest definitions cannot be null.");
            var values = new QuestDefinition[source.Length];
            for (var index = 0; index < values.Length; index++) values[index] = (source[index] ?? throw new InvalidOperationException($"Quest at index {index} is missing.")).ToDefinition();
            var chainSource = _chains ?? throw new InvalidOperationException("Quest chains cannot be null.");
            var chains = new QuestChainDefinition[chainSource.Length];
            for (var index = 0; index < chains.Length; index++)
            {
                var chain = chainSource[index] ?? throw new InvalidOperationException($"Quest chain at index {index} is missing.");
                chains[index] = chain.ToDefinition();
            }
            return new QuestCatalog(values, chains);
        }
    }
    [Serializable]
    public sealed class QuestDefinitionConfig
    {
        [SerializeField] private string _questId;
        [SerializeField] private string _titleId;
        [SerializeField] private string _title;
        [SerializeField] private string _summaryId;
        [SerializeField] private string _summary;
        [SerializeField] private string _objectiveId;
        [SerializeField] private string _objective;
        [SerializeField] private QuestObjectiveKind _kind;
        [SerializeField] private string _targetId;
        [SerializeField] private int _requiredProgress = 1;
        [SerializeField] private string _chainId;
        [SerializeField] private QuestRewardDefinitionConfig _reward;
        internal QuestDefinition ToDefinition() => new(_questId, new QuestText(_titleId, _title), new QuestText(_summaryId, _summary), new QuestText(_objectiveId, _objective), new QuestObjective(_kind, _targetId, _requiredProgress), _chainId, _reward?.ToDefinition());
    }

    [Serializable]
    public sealed class QuestRewardDefinitionConfig
    {
        [SerializeField, Min(0)] private long _goldAmount;
        [SerializeField] private QuestRewardResourceConfig[] _resources = Array.Empty<QuestRewardResourceConfig>();
        [SerializeField] private QuestRewardClaimPointKind _claimPointKind;
        [SerializeField] private string _npcId;
        [SerializeField] private string _claimHintId;
        [SerializeField] private string _claimHint;

        internal QuestRewardDefinition ToDefinition()
        {
            if (_goldAmount == 0 &&
                (_resources == null || _resources.Length == 0) &&
                string.IsNullOrWhiteSpace(_npcId) &&
                string.IsNullOrWhiteSpace(_claimHintId) &&
                string.IsNullOrWhiteSpace(_claimHint))
            {
                return null;
            }

            var resources = _resources ?? throw new InvalidOperationException("Quest reward resources cannot be null.");
            var values = new QuestRewardResource[resources.Length];
            for (var index = 0; index < values.Length; index++)
                values[index] = (resources[index] ?? throw new InvalidOperationException($"Quest reward resource at index {index} is missing.")).ToDefinition();
            var point = _claimPointKind switch
            {
                QuestRewardClaimPointKind.Reception => QuestRewardClaimPoint.Reception,
                QuestRewardClaimPointKind.Npc => QuestRewardClaimPoint.Npc(_npcId),
                _ => throw new ArgumentOutOfRangeException(nameof(_claimPointKind), _claimPointKind, null)
            };
            return new QuestRewardDefinition(_goldAmount, values, point, new QuestText(_claimHintId, _claimHint));
        }
    }

    [Serializable]
    public sealed class QuestRewardResourceConfig
    {
        [SerializeField] private string _definitionId;
        [SerializeField, Min(1)] private int _amount = 1;
        internal QuestRewardResource ToDefinition() => new(_definitionId, _amount);
    }

    [Serializable]
    public sealed class QuestChainDefinitionConfig
    {
        [SerializeField] private string _chainId;
        [SerializeField] private string _titleId;
        [SerializeField] private string _title;
        [SerializeField] private string[] _questIds = Array.Empty<string>();

        internal QuestChainDefinition ToDefinition() => new(
            _chainId,
            new QuestText(_titleId, _title),
            _questIds ?? throw new InvalidOperationException("Quest chain steps cannot be null."));
    }
}
