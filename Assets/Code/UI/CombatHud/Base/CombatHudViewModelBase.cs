using System.Collections.Generic;
using Azzazelloqq.MVVM.Core;
using Azzazelloqq.MVVM.ReactiveLibrary;
using DungeonTeam.Gameplay.Skills.Domain;
using UnityEngine;

namespace DungeonTeam.UI.CombatHud.Base
{
    public abstract class CombatHudViewModelBase : ViewModelBase<CombatHudModelBase>
    {
        protected CombatHudViewModelBase(CombatHudModelBase model) : base(model)
        {
        }

        public abstract IReadOnlyList<IReadOnlyReactiveProperty<CombatHudSlotState>> Slots
        {
            get;
        }

        public abstract IReadOnlyReactiveProperty<bool> ControlsEnabled { get; }

        public abstract IRelayCommand<Vector2> SetMovementCommand { get; }

        public abstract IRelayCommand<SkillSlot> RequestSkillCommand { get; }
    }
}
