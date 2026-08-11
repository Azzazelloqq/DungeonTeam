using System;
using DungeonTeam.Gameplay.Actors.Domain;
using NUnit.Framework;

namespace DungeonTeam.Gameplay.Actors.Tests
{
    public sealed class ActorHealthTests
    {
        [Test]
        public void Create_WithNonPositiveMaximum_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ActorHealth(0));
        }

        [Test]
        public void ApplyDamage_WithNonLethalAmount_ReducesHealth()
        {
            var health = new ActorHealth(10);

            var result = health.ApplyDamage(3);

            Assert.That(result, Is.EqualTo(ActorDamageResult.Damaged));
            Assert.That(health.Current, Is.EqualTo(7));
            Assert.That(health.IsAlive, Is.True);
        }

        [Test]
        public void ApplyDamage_WithLethalAmount_KillsAndClampsHealth()
        {
            var health = new ActorHealth(10);

            var result = health.ApplyDamage(12);

            Assert.That(result, Is.EqualTo(ActorDamageResult.Killed));
            Assert.That(health.Current, Is.Zero);
            Assert.That(health.IsAlive, Is.False);
        }

        [Test]
        public void ApplyDamage_AfterDeath_DoesNotChangeState()
        {
            var health = new ActorHealth(10);
            health.ApplyDamage(10);

            var result = health.ApplyDamage(1);

            Assert.That(result, Is.EqualTo(ActorDamageResult.Ignored));
            Assert.That(health.Current, Is.Zero);
        }

        [Test]
        public void ApplyDamage_WithNonPositiveAmount_Throws()
        {
            var health = new ActorHealth(10);

            Assert.Throws<ArgumentOutOfRangeException>(() => health.ApplyDamage(0));
        }

        [Test]
        public void ApplyHeal_WithWoundedActor_RestoresAndClampsHealth()
        {
            var health = new ActorHealth(10);
            health.ApplyDamage(7);

            var result = health.ApplyHeal(20);

            Assert.That(result, Is.EqualTo(ActorHealResult.Healed));
            Assert.That(health.Current, Is.EqualTo(health.Maximum));
        }

        [Test]
        public void ApplyHeal_WithFullHealth_IsIgnored()
        {
            var health = new ActorHealth(10);

            var result = health.ApplyHeal(3);

            Assert.That(result, Is.EqualTo(ActorHealResult.Ignored));
            Assert.That(health.Current, Is.EqualTo(10));
        }

        [Test]
        public void ApplyHeal_WithMaximumInteger_ClampsWithoutOverflow()
        {
            var health = new ActorHealth(10);
            health.ApplyDamage(7);

            var result = health.ApplyHeal(int.MaxValue);

            Assert.That(result, Is.EqualTo(ActorHealResult.Healed));
            Assert.That(health.Current, Is.EqualTo(health.Maximum));
        }

        [Test]
        public void ApplyHeal_AfterDeath_DoesNotResurrect()
        {
            var health = new ActorHealth(10);
            health.ApplyDamage(10);

            var result = health.ApplyHeal(5);

            Assert.That(result, Is.EqualTo(ActorHealResult.Ignored));
            Assert.That(health.IsAlive, Is.False);
            Assert.That(health.Current, Is.Zero);
        }

        [Test]
        public void ApplyHeal_WithNonPositiveAmount_Throws()
        {
            var health = new ActorHealth(10);

            Assert.Throws<ArgumentOutOfRangeException>(() => health.ApplyHeal(0));
        }
    }
}
