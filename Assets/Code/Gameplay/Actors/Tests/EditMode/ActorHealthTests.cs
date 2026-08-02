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
    }
}
