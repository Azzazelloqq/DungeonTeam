using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azzazelloqq.MVVM.ReactiveLibrary;
using DungeonTeam.Gameplay.Skills.Domain;
using DungeonTeam.UI.CombatHud.Base;

namespace DungeonTeam.UI.CombatHud
{
    public sealed class CombatHudModel : CombatHudModelBase
    {
        private readonly ReactiveProperty<CombatHudSlotState>[] _mutableSlots;
        private readonly IReadOnlyReactiveProperty<CombatHudSlotState>[] _slots;
        private readonly Dictionary<SkillSlot, int> _indices;
        private readonly ReactiveProperty<bool> _controlsEnabled;

        public CombatHudModel(IReadOnlyList<CombatHudSlotState> initialSlots)
        {
            if (initialSlots == null)
                throw new ArgumentNullException(nameof(initialSlots));
            if (initialSlots.Count == 0)
                throw new ArgumentException("Combat HUD requires at least one slot.", nameof(initialSlots));

            _mutableSlots = new ReactiveProperty<CombatHudSlotState>[initialSlots.Count];
            _slots = new IReadOnlyReactiveProperty<CombatHudSlotState>[initialSlots.Count];
            _indices = new Dictionary<SkillSlot, int>(initialSlots.Count);
            _controlsEnabled = new ReactiveProperty<bool>(true);
            _controlsEnabled.AddTo(compositeDisposable);
            for (var index = 0; index < initialSlots.Count; index++)
            {
                var state = initialSlots[index];
                if (!_indices.TryAdd(state.Slot, index))
                {
                    throw new ArgumentException(
                        $"Combat HUD contains slot '{state.Slot}' more than once.",
                        nameof(initialSlots));
                }

                var property = new ReactiveProperty<CombatHudSlotState>(state);
                property.AddTo(compositeDisposable);
                _mutableSlots[index] = property;
                _slots[index] = property;
            }
        }

        public override IReadOnlyList<IReadOnlyReactiveProperty<CombatHudSlotState>> Slots =>
            _slots;

        public override IReadOnlyReactiveProperty<bool> ControlsEnabled => _controlsEnabled;

        public override void UpdateSlot(CombatHudSlotState state)
        {
            if (!_indices.TryGetValue(state.Slot, out var index))
            {
                throw new InvalidOperationException(
                    $"Combat HUD does not contain slot '{state.Slot}'.");
            }

            _mutableSlots[index].SetValue(state);
        }

        public override void SetControlsEnabled(bool isEnabled)
        {
            _controlsEnabled.SetValue(isEnabled);
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
