using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azzazelloqq.MVVM.Core;
using Azzazelloqq.MVVM.ReactiveLibrary;
using DungeonTeam.Gameplay.Skills.Domain;
using DungeonTeam.UI.CombatHud.Base;
using UnityEngine;

namespace DungeonTeam.UI.CombatHud
{
    public sealed class CombatHudViewModel : CombatHudViewModelBase
    {
        public CombatHudViewModel(
            CombatHudModelBase model,
            Action<Vector2> setMovement,
            Action<SkillSlot> requestSkill)
            : base(model)
        {
            if (setMovement == null)
                throw new ArgumentNullException(nameof(setMovement));
            if (requestSkill == null)
                throw new ArgumentNullException(nameof(requestSkill));

            SetMovementCommand = new RelayCommand<Vector2>(setMovement);
            SetMovementCommand.AddTo(compositeDisposable);
            RequestSkillCommand = new RelayCommand<SkillSlot>(requestSkill);
            RequestSkillCommand.AddTo(compositeDisposable);
        }

        public override IReadOnlyList<IReadOnlyReactiveProperty<CombatHudSlotState>> Slots =>
            model.Slots;

        public override IReadOnlyReactiveProperty<bool> ControlsEnabled =>
            model.ControlsEnabled;

        public override IReadOnlyReactiveProperty<CombatHudTargetState> Target => model.Target;

        public override IRelayCommand<Vector2> SetMovementCommand { get; }

        public override IRelayCommand<SkillSlot> RequestSkillCommand { get; }

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
