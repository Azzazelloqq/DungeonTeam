using System.Threading;
using System.Threading.Tasks;
using DungeonTeam.Gameplay.Chests.Runtime.Presentation.Gameplay.Chest.Base;
using UnityEngine;

namespace DungeonTeam.Gameplay.Chests.Runtime.Presentation.Gameplay.Chest
{
    public sealed class ChestPresenter : ChestPresenterBase
    {
        public ChestPresenter(ChestViewBase view, ChestModelBase model)
            : base(view, model)
        {
        }

        public override Vector3 Position => view.Position;

        public override Vector3 RewardPosition => view.RewardPosition;

        public override bool IsOpened => model.IsOpened;

        public override bool TryOpen()
        {
            if (!model.TryOpen())
            {
                return false;
            }

            view.SetOpened(isOpened: true);
            return true;
        }

        protected override void OnInitialize()
        {
            view.SetOpened(model.IsOpened);
        }

        protected override ValueTask OnInitializeAsync(CancellationToken token)
        {
            view.SetOpened(model.IsOpened);
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
