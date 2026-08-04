using System.Threading;
using System.Threading.Tasks;
using DungeonTeam.Gameplay.Chests.Runtime.Presentation.Gameplay.Chest.Base;

namespace DungeonTeam.Gameplay.Chests.Runtime.Presentation.Gameplay.Chest
{
    public sealed class ChestModel : ChestModelBase
    {
        private bool _isOpened;

        public override bool IsOpened => _isOpened;

        public override bool TryOpen()
        {
            if (_isOpened)
            {
                return false;
            }

            _isOpened = true;
            return true;
        }

        protected override void OnInitialize()
        {
        }

        protected override ValueTask OnInitializeAsync(CancellationToken token)
        {
            return default;
        }

        protected override void OnDispose()
        {
        }

        protected override ValueTask OnDisposeAsync(CancellationToken token)
        {
            return default;
        }
    }
}
