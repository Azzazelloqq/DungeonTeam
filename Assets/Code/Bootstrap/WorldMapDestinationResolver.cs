using System;
using DungeonTeam.Gameplay.Contracts.Domain;
using DungeonTeam.Gameplay.DungeonRun.Application;
using DungeonTeam.Gameplay.DungeonRun.Runtime;
using DungeonTeam.UI.WorldMap;

namespace Code.ApplicationRoot
{
    internal sealed class WorldMapDestinationResolver
    {
        public static bool MatchesContractTerminalResult(
            DungeonRunResult result,
            string activeContractId,
            DungeonTeam.Gameplay.Contracts.Domain.ContractCatalog contracts,
            ContractState contractState,
            WorldMapCatalog locations,
            DungeonRunLaunchPresetCatalog presets)
        {
            if (result.Outcome != DungeonRunOutcome.Completed ||
                string.IsNullOrWhiteSpace(activeContractId) ||
                contracts == null ||
                contractState == null ||
                locations == null ||
                presets == null ||
                !string.Equals(contractState.ActiveContractId, activeContractId, StringComparison.Ordinal))
            {
                return false;
            }

            var contract = contracts.Require(activeContractId);
            var location = locations.Require(contract.LocationId);
            if (location.DestinationKind != WorldLocationDestinationKind.DungeonRun)
            {
                return false;
            }

            var preset = presets.Require(location.DestinationId);
            return string.Equals(preset.DungeonId, result.DungeonId, StringComparison.Ordinal);
        }

        private readonly WorldMapCatalog _locations;
        private readonly DungeonTeam.Gameplay.Contracts.Domain.ContractCatalog _contracts;
        private readonly ContractState _contractState;
        private readonly DungeonRunLaunchPresetCatalog _presets;
        private readonly DungeonRunTeamSelection _team;

        public WorldMapDestinationResolver(
            WorldMapCatalog locations,
            DungeonTeam.Gameplay.Contracts.Domain.ContractCatalog contracts,
            ContractState contractState,
            DungeonRunLaunchPresetCatalog presets,
            DungeonRunTeamSelection team)
        {
            _locations = locations ?? throw new ArgumentNullException(nameof(locations));
            _contracts = contracts ?? throw new ArgumentNullException(nameof(contracts));
            _contractState = contractState ?? throw new ArgumentNullException(nameof(contractState));
            _presets = presets ?? throw new ArgumentNullException(nameof(presets));
            _team = team ?? throw new ArgumentNullException(nameof(team));
        }

        public WorldMapDestination Resolve(string locationId)
        {
            var location = _locations.Require(locationId);
            if (!location.IsAvailable)
            {
                return new WorldMapDestination(WorldMapDestinationKind.Unavailable, null);
            }

            if (location.DestinationKind == WorldLocationDestinationKind.GuildHall)
            {
                return new WorldMapDestination(WorldMapDestinationKind.GuildHall, null);
            }

            var contractId = _contractState.ActiveContractId;
            if (contractId == null)
            {
                throw new InvalidOperationException("A Dungeon Run location requires a persisted active contract.");
            }

            var contract = _contracts.Require(contractId);
            if (!contract.IsAuthoredAvailable || contract.LocationId != location.LocationId)
                throw new InvalidOperationException("The persisted active contract is unavailable or does not match the selected World Map location.");
            return new WorldMapDestination(
                WorldMapDestinationKind.DungeonRun,
                _presets.CreateRequest(location.DestinationId, null, _team, contractId));
        }
    }

    internal enum WorldMapDestinationKind
    {
        Unavailable,
        GuildHall,
        DungeonRun
    }

    internal sealed class WorldMapDestination
    {
        public WorldMapDestination(WorldMapDestinationKind kind, DungeonRunStartRequest request)
        {
            if (kind == WorldMapDestinationKind.DungeonRun && request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (kind != WorldMapDestinationKind.DungeonRun && request != null)
            {
                throw new ArgumentException(
                    "Only a Dungeon Run destination can carry a start request.",
                    nameof(request));
            }

            Kind = kind;
            Request = request;
        }

        public WorldMapDestinationKind Kind { get; }
        public bool IsGuildHall => Kind == WorldMapDestinationKind.GuildHall;
        public bool IsUnavailable => Kind == WorldMapDestinationKind.Unavailable;
        public DungeonRunStartRequest Request { get; }
    }
}
