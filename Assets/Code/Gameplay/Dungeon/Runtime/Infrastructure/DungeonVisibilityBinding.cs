using System;
using DungeonTeam.Gameplay.Dungeon.Application;
using DungeonTeam.Gameplay.Dungeon.Runtime.Authoring;

namespace DungeonTeam.Gameplay.Dungeon.Runtime.Infrastructure
{
    internal sealed class DungeonVisibilityBinding : IDungeonVisibilityBinding
    {
        private readonly DungeonVisibilityAuthoring _authoring;
        private bool _initialized;

        public DungeonVisibilityBinding(DungeonVisibilityAuthoring authoring)
        {
            _authoring = authoring ?? throw new ArgumentNullException(nameof(authoring));
        }

        public void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            _authoring.ClosedDoor.SetActive(true);
            _authoring.UnrevealedVeil.SetActive(true);
        }

        public void RevealDoor(int doorIndex)
        {
            if (!_initialized)
            {
                throw new InvalidOperationException("Visibility binding was not initialized.");
            }

            if (doorIndex != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(doorIndex));
            }

            _authoring.ClosedDoor.SetActive(false);
            _authoring.UnrevealedVeil.SetActive(false);
        }
    }
}
