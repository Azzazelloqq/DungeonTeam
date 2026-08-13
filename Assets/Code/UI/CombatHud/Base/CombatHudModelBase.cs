using System.Collections.Generic;
using Azzazelloqq.MVVM.Core;
using Azzazelloqq.MVVM.ReactiveLibrary;

namespace DungeonTeam.UI.CombatHud.Base
{
    public abstract class CombatHudModelBase : ModelBase
    {
        public abstract IReadOnlyList<IReadOnlyReactiveProperty<CombatHudSlotState>> Slots
        {
            get;
        }

        public abstract IReadOnlyReactiveProperty<bool> ControlsEnabled { get; }

        public abstract IReadOnlyReactiveProperty<CombatHudTargetState> Target { get; }

        public abstract void UpdateSlot(CombatHudSlotState state);

        public abstract void SetControlsEnabled(bool isEnabled);

        public abstract void UpdateTarget(CombatHudTargetState state);
    }
}
