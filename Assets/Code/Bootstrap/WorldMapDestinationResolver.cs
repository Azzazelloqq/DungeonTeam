using System;
using DungeonTeam.Gameplay.DungeonRun.Application;
using DungeonTeam.Gameplay.GuildHall.Application;
using DungeonTeam.UI.WorldMap;

namespace Code.ApplicationRoot
{
    internal sealed class WorldMapDestinationResolver
    {
        private readonly WorldMapCatalog _locations;
        private readonly ContractCatalog _contracts;
        private readonly GuildSessionState _session;
        private readonly DungeonRunLaunchPresetCatalog _presets;
        private readonly DungeonRunTeamSelection _team;

        public WorldMapDestinationResolver(WorldMapCatalog locations, ContractCatalog contracts, GuildSessionState session, DungeonRunLaunchPresetCatalog presets, DungeonRunTeamSelection team)
        {
            _locations = locations ?? throw new ArgumentNullException(nameof(locations));
            _contracts = contracts ?? throw new ArgumentNullException(nameof(contracts));
            _session = session ?? throw new ArgumentNullException(nameof(session));
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

            var contractId = _session.SelectedContractId ?? throw new InvalidOperationException("A Dungeon Run location requires a selected contract.");
            var contract = _contracts.Require(contractId);
            if (!contract.IsAvailable || contract.LocationId != location.LocationId)
                throw new InvalidOperationException("The selected contract is unavailable or does not match the selected World Map location.");
            return new WorldMapDestination(
                WorldMapDestinationKind.DungeonRun,
                _presets.CreateRequest(location.DestinationId, null, _team));
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
