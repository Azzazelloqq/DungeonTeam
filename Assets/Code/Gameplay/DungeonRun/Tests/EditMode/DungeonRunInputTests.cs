using System;
using DungeonTeam.Gameplay.DungeonRun.Runtime;
using DungeonTeam.Gameplay.Hero.Runtime;
using DungeonTeam.Gameplay.Skills.Domain;
using NUnit.Framework;
using UnityEngine;

namespace DungeonTeam.Gameplay.DungeonRun.Tests
{
    public sealed class DungeonRunInputTests
    {
        [Test]
        public void Commands_BeforeEnable_AreIgnored()
        {
            var input = new VirtualHeroInput();

            input.SetMovement(Vector2.right);
            input.RequestSkill(SkillSlot.Primary);

            Assert.That(input.Movement, Is.EqualTo(Vector2.zero));
            Assert.That(input.TryConsumeTargetSelection(out _), Is.False);
            Assert.That(input.TryConsumeSkillRequest(out _), Is.False);
        }

        [Test]
        public void Disable_WithPendingState_ClearsAndIgnoresLaterCommands()
        {
            var input = new VirtualHeroInput();
            input.Enable();
            input.SetMovement(Vector2.right);
            input.RequestSkill(SkillSlot.Primary);

            input.Disable();
            input.SetMovement(Vector2.up);
            input.RequestSkill(SkillSlot.Active1);

            Assert.That(input.Movement, Is.EqualTo(Vector2.zero));
            Assert.That(input.TryConsumeTargetSelection(out _), Is.False);
            Assert.That(input.TryConsumeSkillRequest(out _), Is.False);
        }

        [Test]
        public void SetMovement_AboveUnitMagnitude_ClampsDirection()
        {
            var input = new VirtualHeroInput();
            input.Enable();

            input.SetMovement(new Vector2(3f, 4f));

            Assert.That(input.Movement.x, Is.EqualTo(0.6f).Within(0.0001f));
            Assert.That(input.Movement.y, Is.EqualTo(0.8f).Within(0.0001f));
        }

        [Test]
        public void SetMovement_WithNonFiniteValue_Throws()
        {
            var input = new VirtualHeroInput();
            input.Enable();

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                input.SetMovement(new Vector2(float.NaN, 0f)));
        }

        [Test]
        public void RequestSkill_MultipleBeforeConsume_ReturnsLatestExactlyOnce()
        {
            var input = new VirtualHeroInput();
            input.Enable();
            input.RequestSkill(SkillSlot.Primary);
            input.RequestSkill(SkillSlot.Active1);

            var consumed = input.TryConsumeSkillRequest(out var slot);

            Assert.That(consumed, Is.True);
            Assert.That(slot, Is.EqualTo(SkillSlot.Active1));
            Assert.That(input.TryConsumeSkillRequest(out _), Is.False);
        }

        [Test]
        public void RequestSkill_WithUndefinedSlot_Throws()
        {
            var input = new VirtualHeroInput();
            input.Enable();

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                input.RequestSkill((SkillSlot)999));
        }

        [Test]
        public void MobileInput_ProvidesNoPhysicalHeroCommands()
        {
            var input = new MobileDungeonRunInput();

            input.Enable();

            Assert.That(input.Movement, Is.EqualTo(Vector2.zero));
            Assert.That(input.TryConsumeTargetSelection(out _), Is.False);
            Assert.That(input.TryConsumeSkillRequest(out _), Is.False);

            input.Dispose();
            Assert.Throws<ObjectDisposedException>(() => input.Enable());
        }

        [Test]
        public void Movement_WithVirtualValue_OverridesPhysicalValue()
        {
            var physical = new FakeHeroInput { Movement = Vector2.left };
            var virtualInput = new FakeHeroInput { Movement = Vector2.up };
            var input = new CompositeHeroInput(physical, virtualInput);

            Assert.That(input.Movement, Is.EqualTo(Vector2.up));
        }

        [Test]
        public void Movement_WithNeutralVirtualValue_UsesPhysicalValue()
        {
            var physical = new FakeHeroInput { Movement = Vector2.left };
            var virtualInput = new FakeHeroInput { Movement = Vector2.zero };
            var input = new CompositeHeroInput(physical, virtualInput);

            Assert.That(input.Movement, Is.EqualTo(Vector2.left));
        }

        [Test]
        public void SkillRequest_WhenBothSourcesHaveCommands_ReturnsVirtualWithoutReplay()
        {
            var physical = new FakeHeroInput();
            physical.QueueSkill(SkillSlot.Primary);
            var virtualInput = new FakeHeroInput();
            virtualInput.QueueSkill(SkillSlot.Active1);
            var input = new CompositeHeroInput(physical, virtualInput);

            var consumed = input.TryConsumeSkillRequest(out var slot);

            Assert.That(consumed, Is.True);
            Assert.That(slot, Is.EqualTo(SkillSlot.Active1));
            Assert.That(input.TryConsumeSkillRequest(out _), Is.False);
        }

        [Test]
        public void TargetSelection_UsesPhysicalInputOnly()
        {
            var expectedPosition = new Vector2(320f, 180f);
            var physical = new FakeHeroInput();
            physical.QueueTargetSelection(expectedPosition);
            var virtualInput = new FakeHeroInput();
            var input = new CompositeHeroInput(physical, virtualInput);

            var consumed = input.TryConsumeTargetSelection(out var screenPosition);

            Assert.That(consumed, Is.True);
            Assert.That(screenPosition, Is.EqualTo(expectedPosition));
            Assert.That(input.TryConsumeTargetSelection(out _), Is.False);
        }

        private sealed class FakeHeroInput : IHeroInput
        {
            private SkillSlot? _pendingSkill;
            private Vector2? _pendingTargetSelection;

            public Vector2 Movement { get; set; }

            public void QueueSkill(SkillSlot slot)
            {
                _pendingSkill = slot;
            }

            public void QueueTargetSelection(Vector2 screenPosition)
            {
                _pendingTargetSelection = screenPosition;
            }

            public bool TryConsumeTargetSelection(out Vector2 screenPosition)
            {
                if (!_pendingTargetSelection.HasValue)
                {
                    screenPosition = Vector2.zero;
                    return false;
                }

                screenPosition = _pendingTargetSelection.Value;
                _pendingTargetSelection = null;
                return true;
            }

            public bool TryConsumeSkillRequest(out SkillSlot slot)
            {
                if (!_pendingSkill.HasValue)
                {
                    slot = default;
                    return false;
                }

                slot = _pendingSkill.Value;
                _pendingSkill = null;
                return true;
            }
        }
    }
}
