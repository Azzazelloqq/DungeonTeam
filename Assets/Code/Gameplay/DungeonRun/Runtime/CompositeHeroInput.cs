using System;
using DungeonTeam.Gameplay.Hero.Runtime;
using DungeonTeam.Gameplay.Skills.Domain;
using UnityEngine;

namespace DungeonTeam.Gameplay.DungeonRun.Runtime
{
    internal sealed class CompositeHeroInput : IHeroInput
    {
        private readonly IHeroInput _physicalInput;
        private readonly IHeroInput _virtualInput;

        public CompositeHeroInput(IHeroInput physicalInput, IHeroInput virtualInput)
        {
            _physicalInput = physicalInput ?? throw new ArgumentNullException(nameof(physicalInput));
            _virtualInput = virtualInput ?? throw new ArgumentNullException(nameof(virtualInput));
        }

        public Vector2 Movement
        {
            get
            {
                var virtualMovement = _virtualInput.Movement;
                return virtualMovement.sqrMagnitude > 0f
                    ? virtualMovement
                    : _physicalInput.Movement;
            }
        }

        public bool TryConsumeSkillRequest(out SkillSlot slot)
        {
            if (!_virtualInput.TryConsumeSkillRequest(out slot))
            {
                return _physicalInput.TryConsumeSkillRequest(out slot);
            }

            _physicalInput.TryConsumeSkillRequest(out _);
            return true;
        }

        public bool TryConsumeTargetSelection(out Vector2 screenPosition)
        {
            return _physicalInput.TryConsumeTargetSelection(out screenPosition);
        }
    }
}
