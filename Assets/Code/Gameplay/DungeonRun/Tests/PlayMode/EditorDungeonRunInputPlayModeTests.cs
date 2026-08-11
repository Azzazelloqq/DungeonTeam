#if UNITY_EDITOR
using System;
using DungeonTeam.Gameplay.DungeonRun.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace DungeonTeam.Gameplay.DungeonRun.Tests.PlayMode
{
    public sealed class EditorDungeonRunInputPlayModeTests : InputTestFixture
    {
        private EditorDungeonRunInput _input;

        public override void Setup()
        {
            base.Setup();
            _input = new EditorDungeonRunInput();
            _input.Enable();
        }

        public override void TearDown()
        {
            _input?.Dispose();
            _input = null;
            base.TearDown();
        }

        [Test]
        public void WasdMovement_EmitsExpectedDirection()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();

            Press(keyboard.wKey);

            Assert.That(_input.Movement, Is.EqualTo(Vector2.up));
            Release(keyboard.wKey);
            Assert.That(_input.Movement, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void SkillBindings_AreNotProvided()
        {
            InputSystem.AddDevice<Keyboard>();

            Assert.That(_input.TryConsumeSkillRequest(out _), Is.False);
        }

        [Test]
        public void Dispose_IsIdempotent_AndEnableAfterDisposeThrows()
        {
            _input.Dispose();
            _input.Dispose();

            Assert.Throws<ObjectDisposedException>(() => _input.Enable());
            Assert.That(_input.Movement, Is.EqualTo(Vector2.zero));
        }
    }
}
#endif
