using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DungeonTeam.UI.WorldMap
{
    public sealed class WorldMapTextSnapshot
    {
        public WorldMapTextSnapshot(string textId, string displayText)
        {
            if (string.IsNullOrWhiteSpace(textId))
            {
                throw new ArgumentException("World Map text ID cannot be empty.", nameof(textId));
            }

            if (string.IsNullOrWhiteSpace(displayText))
            {
                throw new ArgumentException("World Map display text cannot be empty.", nameof(displayText));
            }

            TextId = textId;
            DisplayText = displayText;
        }

        public string TextId { get; }
        public string DisplayText { get; }
    }

    public sealed class WorldMapUiTextSnapshot
    {
        public WorldMapUiTextSnapshot(
            WorldMapTextSnapshot title,
            WorldMapTextSnapshot back,
            WorldMapTextSnapshot empty)
        {
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Back = back ?? throw new ArgumentNullException(nameof(back));
            Empty = empty ?? throw new ArgumentNullException(nameof(empty));
        }

        public WorldMapTextSnapshot Title { get; }
        public WorldMapTextSnapshot Back { get; }
        public WorldMapTextSnapshot Empty { get; }
    }

    public sealed class WorldMapStartContext
    {
        public WorldMapStartContext(
            IReadOnlyList<WorldLocationSnapshot> locations,
            WorldMapUiTextSnapshot texts)
        {
            if (locations == null)
            {
                throw new ArgumentNullException(nameof(locations));
            }

            var copy = new WorldLocationSnapshot[locations.Count];
            for (var index = 0; index < locations.Count; index++)
            {
                copy[index] = locations[index] ?? throw new ArgumentException(
                    $"World Map location at index {index} is missing.", nameof(locations));
            }

            Locations = new ReadOnlyCollection<WorldLocationSnapshot>(copy);
            Texts = texts ?? throw new ArgumentNullException(nameof(texts));
        }

        public IReadOnlyList<WorldLocationSnapshot> Locations { get; }
        public WorldMapUiTextSnapshot Texts { get; }
    }

    public enum WorldLocationDestinationKind
    {
        GuildHall = 0,
        DungeonRun = 1
    }

    public sealed class WorldLocationSnapshot
    {
        public WorldLocationSnapshot(
            string locationId,
            WorldMapTextSnapshot title,
            WorldMapTextSnapshot description,
            bool isAvailable,
            WorldMapTextSnapshot disabledReason,
            WorldLocationDestinationKind destinationKind,
            string destinationId)
        {
            if (string.IsNullOrWhiteSpace(locationId))
            {
                throw new ArgumentException("World location ID cannot be empty.", nameof(locationId));
            }

            if (!Enum.IsDefined(typeof(WorldLocationDestinationKind), destinationKind))
            {
                throw new ArgumentOutOfRangeException(nameof(destinationKind), destinationKind, null);
            }

            if (isAvailable && disabledReason != null)
            {
                throw new ArgumentException(
                    "An available world location cannot have a disabled reason.",
                    nameof(disabledReason));
            }

            if (!isAvailable && disabledReason == null)
            {
                throw new ArgumentException(
                    "An unavailable world location requires a disabled reason.",
                    nameof(disabledReason));
            }

            if (destinationKind == WorldLocationDestinationKind.DungeonRun &&
                string.IsNullOrWhiteSpace(destinationId))
            {
                throw new ArgumentException(
                    "A Dungeon Run destination requires a launch preset ID.",
                    nameof(destinationId));
            }

            if (destinationKind == WorldLocationDestinationKind.GuildHall && destinationId != null)
            {
                throw new ArgumentException(
                    "The Guild Hall destination does not use a destination ID.",
                    nameof(destinationId));
            }

            LocationId = locationId;
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            IsAvailable = isAvailable;
            DisabledReason = disabledReason;
            DestinationKind = destinationKind;
            DestinationId = destinationId;
        }

        public string LocationId { get; }
        public WorldMapTextSnapshot Title { get; }
        public WorldMapTextSnapshot Description { get; }
        public bool IsAvailable { get; }
        public WorldMapTextSnapshot DisabledReason { get; }
        public WorldLocationDestinationKind DestinationKind { get; }
        public string DestinationId { get; }
    }

    public sealed class WorldMapCatalog
    {
        private readonly IReadOnlyDictionary<string, WorldLocationSnapshot> _locationsById;

        public WorldMapCatalog(IReadOnlyList<WorldLocationSnapshot> locations, WorldMapUiTextSnapshot texts)
        {
            if (locations == null)
            {
                throw new ArgumentNullException(nameof(locations));
            }

            var snapshot = new WorldLocationSnapshot[locations.Count];
            var byId = new Dictionary<string, WorldLocationSnapshot>(StringComparer.Ordinal);
            var contractLocations = new List<string>();
            for (var index = 0; index < locations.Count; index++)
            {
                var location = locations[index] ?? throw new ArgumentException(
                    $"World location at index {index} is missing.",
                    nameof(locations));
                if (!byId.TryAdd(location.LocationId, location))
                {
                    throw new ArgumentException(
                        $"World location ID '{location.LocationId}' is duplicated.",
                        nameof(locations));
                }

                snapshot[index] = location;
                if (location.DestinationKind == WorldLocationDestinationKind.DungeonRun)
                {
                    contractLocations.Add(location.LocationId);
                }
            }

            Locations = new ReadOnlyCollection<WorldLocationSnapshot>(snapshot);
            Texts = texts ?? throw new ArgumentNullException(nameof(texts));
            ContractDestinationLocationIds = new ReadOnlyCollection<string>(
                contractLocations.ToArray());
            _locationsById = new ReadOnlyDictionary<string, WorldLocationSnapshot>(byId);
        }

        public IReadOnlyList<WorldLocationSnapshot> Locations { get; }
        public WorldMapUiTextSnapshot Texts { get; }
        public IReadOnlyList<string> ContractDestinationLocationIds { get; }

        public WorldLocationSnapshot Require(string locationId)
        {
            if (string.IsNullOrWhiteSpace(locationId))
            {
                throw new ArgumentException("World location ID cannot be empty.", nameof(locationId));
            }

            if (!_locationsById.TryGetValue(locationId, out var location))
            {
                throw new KeyNotFoundException($"Unknown world location ID '{locationId}'.");
            }

            return location;
        }

        public WorldMapStartContext CreateStartContext() => new(Locations, Texts);
    }
}
