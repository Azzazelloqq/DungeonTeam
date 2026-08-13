using System;
using DungeonTeam.Gameplay.Combat.Runtime;
using DungeonTeam.Gameplay.Hero.Runtime;
using DungeonTeam.Gameplay.Skills.Runtime;
using DungeonTeam.UI.CombatHud;
using DungeonTeam.UI.CombatHud.Base;
using TickHandler;
using UnityEngine;

namespace DungeonTeam.Gameplay.DungeonRun.Runtime
{
    internal sealed class DungeonRunCombatHudController : IDisposable
    {
        private readonly HeroController _heroController;
        private readonly ActorCombatController _combatController;
        private readonly Camera _worldCamera;
        private readonly ITickHandler _tickHandler;
        private readonly CombatHudModelBase _model;
        private readonly string[] _titles;
        private readonly UnityEngine.Texture2D[] _icons;

        private bool _isInitialized;
        private bool _isDisposed;

        public DungeonRunCombatHudController(
            HeroController heroController,
            ActorCombatController combatController,
            Camera worldCamera,
            ITickHandler tickHandler,
            CombatHudModelBase model)
        {
            _heroController = heroController ?? throw new ArgumentNullException(
                nameof(heroController));
            _combatController = combatController ?? throw new ArgumentNullException(
                nameof(combatController));
            _worldCamera = worldCamera != null
                ? worldCamera
                : throw new ArgumentNullException(nameof(worldCamera));
            _tickHandler = tickHandler ?? throw new ArgumentNullException(nameof(tickHandler));
            _model = model ?? throw new ArgumentNullException(nameof(model));
            if (_combatController.Slots.Count != model.Slots.Count)
            {
                throw new ArgumentException(
                    "Combat HUD model must contain every leader loadout slot.",
                    nameof(model));
            }

            _titles = new string[_combatController.Slots.Count];
            _icons = new UnityEngine.Texture2D[_combatController.Slots.Count];
            for (var index = 0; index < _combatController.Slots.Count; index++)
            {
                var slot = _combatController.Slots[index];
                _titles[index] = model.Slots[index].Value.Title;
                _icons[index] = model.Slots[index].Value.Icon;
                if (model.Slots[index].Value.Slot != slot.Slot)
                {
                    throw new ArgumentException(
                        "Combat HUD model slot order must match the leader loadout.",
                        nameof(model));
                }
            }
        }

        public static CombatHudSlotState[] CreateInitialStates(
            HeroController heroController,
            ActorCombatController combatController,
            SkillViewSet skillViews)
        {
            if (heroController == null)
                throw new ArgumentNullException(nameof(heroController));
            if (combatController == null)
                throw new ArgumentNullException(nameof(combatController));
            if (skillViews == null)
                throw new ArgumentNullException(nameof(skillViews));

            var states = new CombatHudSlotState[combatController.Slots.Count];
            for (var index = 0; index < states.Length; index++)
            {
                var slot = combatController.Slots[index];
                states[index] = CreateState(
                    heroController,
                    slot,
                    CreateTitle(slot),
                    skillViews.RequireIcon(slot.Skill.SkillId),
                    isEnabled: true);
            }

            return states;
        }

        public void Initialize()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(DungeonRunCombatHudController));
            if (_isInitialized)
            {
                throw new InvalidOperationException(
                    "Dungeon Run Combat HUD is already initialized.");
            }

            _tickHandler.SubscribeOnFrameUpdate(OnFrameUpdate);
            _isInitialized = true;
            _model.SetControlsEnabled(true);
            Refresh(isEnabled: true);
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            if (!_isInitialized)
                return;

            _tickHandler.UnsubscribeOnFrameUpdate(OnFrameUpdate);
            _model.SetControlsEnabled(false);
            _model.UpdateTarget(CombatHudTargetState.Hidden);
            Refresh(isEnabled: false);
        }

        private void OnFrameUpdate(float _)
        {
            Refresh(isEnabled: true);
        }

        private void Refresh(bool isEnabled)
        {
            RefreshTarget(isEnabled);
            for (var index = 0; index < _combatController.Slots.Count; index++)
            {
                _model.UpdateSlot(CreateState(
                    _heroController,
                    _combatController.Slots[index],
                    _titles[index],
                    _icons[index],
                    isEnabled));
            }
        }

        private void RefreshTarget(bool isEnabled)
        {
            var target = _heroController.Target;
            if (!isEnabled || target == null || !target.IsAlive)
            {
                _model.UpdateTarget(CombatHudTargetState.Hidden);
                return;
            }

            var overheadAnchor = target.OverheadAnchor;
            var worldPosition = overheadAnchor != null
                ? overheadAnchor.position
                : target.Position + Vector3.up;
            _model.UpdateTarget(new CombatHudTargetState(
                _worldCamera.WorldToScreenPoint(worldPosition),
                _heroController.IsTargetManuallySelected
                    ? CombatHudTargetSelection.Manual
                    : CombatHudTargetSelection.Automatic));
        }

        private static CombatHudSlotState CreateState(
            HeroController heroController,
            CombatSkillSlotState slot,
            string title,
            UnityEngine.Texture2D icon,
            bool isEnabled)
        {
            var canRequestSkill = isEnabled && heroController.CanRequestSkill(slot.Slot);
            var isPending = isEnabled && heroController.PendingSlot == slot.Slot;
            var activePhase = isEnabled && slot.IsActive ? slot.ActivePhase : null;
            var isActorBusy = isEnabled && slot.IsActorBusy;

            return new CombatHudSlotState(
                slot.Slot,
                title,
                icon,
                slot.CooldownDuration,
                slot.CooldownRemaining,
                canRequestSkill,
                heroController.SelectedSlot == slot.Slot,
                isPending,
                activePhase,
                isActorBusy,
                CombatHudSlotFeedbackResolver.Resolve(
                    isEnabled,
                    canRequestSkill,
                    isPending,
                    activePhase,
                    slot.CooldownRemaining,
                    isActorBusy));
        }

        private static string CreateTitle(CombatSkillSlotState slot)
        {
            return $"{slot.Skill.DisplayName}  Lv.{slot.Level.Level}";
        }
    }
}
