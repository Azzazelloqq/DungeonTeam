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
                _minimumRankId);
        }
    }
}
