using System;
using Code.Configuration;
using DungeonTeam.Gameplay.Contracts.Domain;
using UnityEngine;

namespace DungeonTeam.Gameplay.GuildHall.Runtime.Config
{
    [CreateAssetMenu(
        menuName = "DungeonTeam/Gameplay/Guild Contract Config",
        fileName = "ContractConfig")]
    public sealed class ContractConfigPage : ConfigPage
    {
        [SerializeField]
        private ContractDefinitionConfig[] _contracts = Array.Empty<ContractDefinitionConfig>();

        public DungeonTeam.Gameplay.Contracts.Domain.ContractCatalog CreateCatalog()
        {
            if (_contracts == null)
            {
                throw new InvalidOperationException("Guild contracts cannot be null.");
            }

            var definitions = new ContractDefinition[_contracts.Length];
            for (var index = 0; index < _contracts.Length; index++)
            {
                definitions[index] = (_contracts[index] ?? throw new InvalidOperationException(
                        $"Guild contract at index {index} is missing."))
                    .ToDefinition(index);
            }

            return new DungeonTeam.Gameplay.Contracts.Domain.ContractCatalog(definitions);
        }
    }

    [Serializable]
    public sealed class ContractDefinitionConfig
    {
        [SerializeField]
        private string _contractId;

        [SerializeField]
        private GuildTextDefinitionConfig _title = new();

        [SerializeField]
        private GuildTextDefinitionConfig _summary = new();

        [SerializeField]
        private string _locationId;

        [SerializeField]
        private bool _isAvailable = true;

        [SerializeField]
        private string _minimumRankId;

        [SerializeField]
        private GuildTextDefinitionConfig _disabledReason;

        [SerializeField]
        private ContractRewardDefinitionConfig _reward;

        internal ContractDefinition ToDefinition(int index)
        {
            var location = $"Guild contract at index {index}";
            var title = (_title ?? throw new InvalidOperationException(
                    $"{location} has no title.")).ToContractSnapshot($"{location} title");
            var summary = (_summary ?? throw new InvalidOperationException(
                    $"{location} has no summary.")).ToContractSnapshot($"{location} summary");
            var disabledReason = _isAvailable
                ? null
                : (_disabledReason ?? throw new InvalidOperationException(
                    $"{location} is unavailable but has no reason.")).ToContractSnapshot(
                    $"{location} disabled reason");
            return new ContractDefinition(
                _contractId,
                title,
                summary,
                _locationId,
                _isAvailable,
                disabledReason,
                _minimumRankId,
                _reward?.ToDefinition());
        }
    }

    [Serializable]
    public sealed class ContractRewardDefinitionConfig
    {
        [SerializeField, Min(0)]
        private long _goldAmount;

        [SerializeField]
        private ContractRewardResourceConfig[] _resources = Array.Empty<ContractRewardResourceConfig>();

        [SerializeField]
        private ContractRewardClaimPointKind _claimPointKind;

        [SerializeField]
        private string _npcId;

        [SerializeField]
        private string _claimHintId;

        [SerializeField]
        private string _claimHint;

        internal ContractRewardDefinition ToDefinition()
        {
            if (_goldAmount == 0 &&
                (_resources == null || _resources.Length == 0) &&
                string.IsNullOrWhiteSpace(_npcId) &&
                string.IsNullOrWhiteSpace(_claimHintId) &&
                string.IsNullOrWhiteSpace(_claimHint))
            {
                return null;
            }

            var resources = _resources ?? throw new InvalidOperationException(
                "Contract reward resources cannot be null.");
            var values = new ContractRewardResource[resources.Length];
            for (var index = 0; index < values.Length; index++)
            {
                values[index] = (resources[index] ?? throw new InvalidOperationException(
                    $"Contract reward resource at index {index} is missing.")).ToDefinition();
            }

            var point = _claimPointKind switch
            {
                ContractRewardClaimPointKind.Reception => ContractRewardClaimPoint.Reception,
                ContractRewardClaimPointKind.Npc => ContractRewardClaimPoint.Npc(_npcId),
                _ => throw new ArgumentOutOfRangeException(nameof(_claimPointKind), _claimPointKind, null)
            };

            return new ContractRewardDefinition(
                _goldAmount,
                values,
                point,
                new ContractTextSnapshot(_claimHintId, _claimHint));
        }
    }

    [Serializable]
    public sealed class ContractRewardResourceConfig
    {
        [SerializeField]
        private string _definitionId;

        [SerializeField, Min(1)]
        private int _amount = 1;

        internal ContractRewardResource ToDefinition() =>
            new(_definitionId, _amount);
    }
}
