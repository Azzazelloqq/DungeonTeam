using System;
using DungeonTeam.Gameplay.Actors.Domain;
using NUnit.Framework;

namespace DungeonTeam.Gameplay.Actors.Tests
{
    public sealed class AttackCooldownTests
    {
        [Test]
        public void Create_WithNonPositiveDuration_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new AttackCooldown(0f));
        }

        [Test]
        public void Tick_WhenReadyAndAttackIsAllowed_TriggersImmediately()
        {
            var cooldown = new AttackCooldown(duration: 1f);

            var shouldAttack = cooldown.Tick(deltaTime: 0f, canAttack: true);

            Assert.That(shouldAttack, Is.True);
        }

        [Test]
        public void Tick_BeforeCooldownExpires_DoesNotTriggerAgain()
        {
            var cooldown = new AttackCooldown(duration: 1f);
            cooldown.Tick(deltaTime: 0f, canAttack: true);

            var shouldAttack = cooldown.Tick(deltaTime: 0.5f, canAttack: true);

            Assert.That(shouldAttack, Is.False);
        }

        [Test]
        public void Tick_WhenCooldownExpires_TriggersAgain()
        {
            var cooldown = new AttackCooldown(duration: 1f);
            cooldown.Tick(deltaTime: 0f, canAttack: true);

            var shouldAttack = cooldown.Tick(deltaTime: 1f, canAttack: true);

            Assert.That(shouldAttack, Is.True);
        }

        [Test]
        public void Tick_WhileAttackIsUnavailable_StillRecoversCooldown()
        {
            var cooldown = new AttackCooldown(duration: 1f);
            cooldown.Tick(deltaTime: 0f, canAttack: true);
            cooldown.Tick(deltaTime: 1f, canAttack: false);

            var shouldAttack = cooldown.Tick(deltaTime: 0f, canAttack: true);

            Assert.That(shouldAttack, Is.True);
        }

        [Test]
        public void Tick_WithNegativeDeltaTime_Throws()
        {
            var cooldown = new AttackCooldown(duration: 1f);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                cooldown.Tick(deltaTime: -0.01f, canAttack: true));
        }
    }
}
