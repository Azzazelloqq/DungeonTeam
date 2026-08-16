using System;
using Code.Configuration;
using UnityEngine;

namespace DungeonTeam.UI.WorldMap
{
    [CreateAssetMenu(
        menuName = "DungeonTeam/UI/World Map Config",
        fileName = "WorldMapConfig")]
    public sealed class WorldMapConfigPage : ConfigPage
    {
        [SerializeField]
        private WorldLocationDefinitionConfig[] _locations =
            Array.Empty<WorldLocationDefinitionConfig>();

        [SerializeField]
        private WorldMapUiTextDefinitionConfig _texts = new();

        public WorldMapCatalog CreateCatalog()
        {
            if (_locations == null)
            {
                throw new InvalidOperationException("World Map locations cannot be null.");
            }

            var locations = new WorldLocationSnapshot[_locations.Length];
            for (var index = 0; index < _locations.Length; index++)
            {
                locations[index] = (_locations[index] ?? throw new InvalidOperationException(
                        $"World Map location at index {index} is missing."))
                    .ToSnapshot(index);
            }

            return new WorldMapCatalog(locations, (_texts ?? throw new InvalidOperationException(
                "World Map UI texts cannot be null.")).ToSnapshot());
        }
    }

    [Serializable]
    public sealed class WorldLocationDefinitionConfig
    {
        [SerializeField]
        private string _locationId;

        [SerializeField]
        private WorldMapTextDefinitionConfig _title = new();

        [SerializeField]
        private WorldMapTextDefinitionConfig _description = new();

        [SerializeField]
        private bool _isAvailable = true;

        [SerializeField]
        private WorldMapTextDefinitionConfig _disabledReason;

        [SerializeField]
        private WorldLocationDestinationKind _destinationKind;

        [SerializeField]
        private string _destinationId;

        internal WorldLocationSnapshot ToSnapshot(int index)
        {
            var location = $"World Map location at index {index}";
            return new WorldLocationSnapshot(
                _locationId,
                (_title ?? throw new InvalidOperationException(
                    $"{location} has no title.")).ToSnapshot($"{location} title"),
                (_description ?? throw new InvalidOperationException(
                    $"{location} has no description.")).ToSnapshot($"{location} description"),
                _isAvailable,
                _isAvailable
                    ? null
                    : (_disabledReason ?? throw new InvalidOperationException(
                        $"{location} is unavailable but has no reason."))
                    .ToSnapshot($"{location} disabled reason"),
                _destinationKind,
                string.IsNullOrWhiteSpace(_destinationId) ? null : _destinationId);
        }
    }

    [Serializable]
    public sealed class WorldMapTextDefinitionConfig
    {
        [SerializeField]
        private string _textId;

        [SerializeField, TextArea]
        private string _fallbackRu;

        internal WorldMapTextSnapshot ToSnapshot(string location)
        {
            try
            {
                return new WorldMapTextSnapshot(_textId, _fallbackRu);
            }
            catch (ArgumentException exception)
            {
                throw new InvalidOperationException($"{location}: {exception.Message}", exception);
            }
        }
    }

    [Serializable]
    public sealed class WorldMapUiTextDefinitionConfig
    {
        [SerializeField] private WorldMapTextDefinitionConfig _title = new();
        [SerializeField] private WorldMapTextDefinitionConfig _back = new();
        [SerializeField] private WorldMapTextDefinitionConfig _empty = new();

        internal WorldMapUiTextSnapshot ToSnapshot()
        {
            return new WorldMapUiTextSnapshot(
                (_title ?? throw new InvalidOperationException("World Map title text is missing.")).ToSnapshot("World Map title"),
                (_back ?? throw new InvalidOperationException("World Map back text is missing.")).ToSnapshot("World Map back"),
                (_empty ?? throw new InvalidOperationException("World Map empty text is missing.")).ToSnapshot("World Map empty state"));
        }
    }
}
