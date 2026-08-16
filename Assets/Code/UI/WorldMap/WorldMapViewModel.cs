using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DungeonTeam.UI.WorldMap
{
    public sealed class WorldMapLocationItemViewModel
    {
        private readonly Action<string> _selected;

        internal WorldMapLocationItemViewModel(WorldLocationSnapshot location, Action<string> selected)
        {
            Location = location ?? throw new ArgumentNullException(nameof(location));
            _selected = selected ?? throw new ArgumentNullException(nameof(selected));
        }

        public WorldLocationSnapshot Location { get; }
        public string LocationId => Location.LocationId;
        public string Title => Location.Title.DisplayText;
        public string Description => Location.Description.DisplayText;
        public bool IsAvailable => Location.IsAvailable;
        public string DisabledReason => Location.DisabledReason?.DisplayText;

        public void Select()
        {
            if (IsAvailable)
            {
                _selected(Location.LocationId);
            }
        }
    }

    public sealed class WorldMapViewModel : IDisposable
    {
        private readonly Action<string> _locationSelected;
        private readonly Action _backRequested;
        private bool _isInteractionBlocked;
        private bool _isDisposed;

        public WorldMapViewModel(
            WorldMapStartContext context,
            Action<string> locationSelected,
            Action backRequested)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            _locationSelected = locationSelected ?? throw new ArgumentNullException(nameof(locationSelected));
            _backRequested = backRequested ?? throw new ArgumentNullException(nameof(backRequested));
            var items = new WorldMapLocationItemViewModel[context.Locations.Count];
            for (var index = 0; index < items.Length; index++)
            {
                items[index] = new WorldMapLocationItemViewModel(context.Locations[index], Select);
            }

            Items = new ReadOnlyCollection<WorldMapLocationItemViewModel>(items);
        }

        public WorldMapStartContext Context { get; }
        public IReadOnlyList<WorldMapLocationItemViewModel> Items { get; }
        public bool IsInteractionBlocked => _isInteractionBlocked;

        public void Select(string locationId)
        {
            if (_isDisposed || _isInteractionBlocked)
            {
                return;
            }

            for (var index = 0; index < Context.Locations.Count; index++)
            {
                var location = Context.Locations[index];
                if (location.LocationId == locationId && location.IsAvailable)
                {
                    _isInteractionBlocked = true;
                    _locationSelected(locationId);
                    return;
                }
            }
        }

        public void RequestBack()
        {
            if (_isDisposed || _isInteractionBlocked)
            {
                return;
            }

            _isInteractionBlocked = true;
            _backRequested();
        }

        public bool RestoreInteraction()
        {
            if (_isDisposed)
            {
                return false;
            }

            _isInteractionBlocked = false;
            return true;
        }

        public void Dispose() => _isDisposed = true;
    }
}
