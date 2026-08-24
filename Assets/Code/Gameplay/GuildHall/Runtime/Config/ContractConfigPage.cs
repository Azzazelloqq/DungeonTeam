using System;
using Code.Configuration;
using DungeonTeam.Gameplay.GuildHall.Application;
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

        public ContractCatalog CreateCatalog()
        {
            if (_contracts == null)
            {
                throw new InvalidOperationException("Guild contracts cannot be null.");
            }

            var offers = new NoticeBoardOfferSnapshot[_contracts.Length];
            for (var index = 0; index < _contracts.Length; index++)
            {
                offers[index] = (_contracts[index] ?? throw new InvalidOperationException(
                        $"Guild contract at index {index} is missing."))
                    .ToSnapshot(index);
            }

            return new ContractCatalog(offers);
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

        internal NoticeBoardOfferSnapshot ToSnapshot(int index)
        {
            var location = $"Guild contract at index {index}";
            return new NoticeBoardOfferSnapshot(
                _contractId,
                (_title ?? throw new InvalidOperationException(
                    $"{location} has no title.")).ToSnapshot($"{location} title"),
                (_summary ?? throw new InvalidOperationException(
                    $"{location} has no summary.")).ToSnapshot($"{location} summary"),
                _locationId,
                _isAvailable,
                _isAvailable
                    ? null
                    : (_disabledReason ?? throw new InvalidOperationException(
                        $"{location} is unavailable but has no reason."))
                    .ToSnapshot($"{location} disabled reason"),
                _minimumRankId);
        }
    }
}
