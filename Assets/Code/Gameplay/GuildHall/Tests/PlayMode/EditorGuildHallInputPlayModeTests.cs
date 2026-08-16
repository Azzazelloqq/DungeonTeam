#if UNITY_EDITOR
using System;
using DungeonTeam.Gameplay.GuildHall.Runtime.Input;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DungeonTeam.Gameplay.GuildHall.Tests.PlayMode
{
    public sealed class EditorGuildHallInputPlayModeTests : InputTestFixture
    {
        private EditorGuildHallInput _input;

        public override void Setup()
        {
            base.Setup();
            _input = new EditorGuildHallInput();
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
        public void Dispose_IsIdempotent_AndPreventsReenable()
        {
            _input.Dispose();
            _input.Dispose();

            Assert.That(_input.Movement, Is.EqualTo(Vector2.zero));
            Assert.Throws<ObjectDisposedException>(_input.Enable);
        }
    }
}
#endif
