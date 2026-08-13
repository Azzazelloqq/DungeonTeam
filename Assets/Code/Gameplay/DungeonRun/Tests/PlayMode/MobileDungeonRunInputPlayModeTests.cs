using System;
using DungeonTeam.Gameplay.DungeonRun.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace DungeonTeam.Gameplay.DungeonRun.Tests.PlayMode
{
    public sealed class MobileDungeonRunInputPlayModeTests : InputTestFixture
    {
        private MobileDungeonRunInput _input;

        public override void Setup()
        {
            base.Setup();
            _input = new MobileDungeonRunInput();
            _input.Enable();
        }

        public override void TearDown()
        {
            _input?.Dispose();
            _input = null;
            base.TearDown();
        }

        [Test]
        public void SecondTouch_WhileFirstIsHeld_CapturesSecondTouchPosition()
        {
            var touchscreen = InputSystem.AddDevice<Touchscreen>();
            var firstPosition = new Vector2(120f, 180f);
            var secondPosition = new Vector2(760f, 420f);

            BeginTouch(1, firstPosition, screen: touchscreen);
            Assert.That(_input.TryConsumeTargetSelection(out var capturedFirst), Is.True);
            Assert.That(capturedFirst, Is.EqualTo(firstPosition));

            BeginTouch(2, secondPosition, screen: touchscreen);

            Assert.That(_input.TryConsumeTargetSelection(out var capturedSecond), Is.True);
            Assert.That(capturedSecond, Is.EqualTo(secondPosition));
            Assert.That(_input.TryConsumeTargetSelection(out _), Is.False);

            EndTouch(2, secondPosition, screen: touchscreen);
            EndTouch(1, firstPosition, screen: touchscreen);
        }

        [Test]
        public void Dispose_IsIdempotent_AndEnableAfterDisposeThrows()
        {
            _input.Dispose();
            _input.Dispose();

            Assert.Throws<ObjectDisposedException>(() => _input.Enable());
            Assert.That(_input.Movement, Is.EqualTo(Vector2.zero));
            Assert.That(_input.TryConsumeTargetSelection(out _), Is.False);
        }
    }
}
