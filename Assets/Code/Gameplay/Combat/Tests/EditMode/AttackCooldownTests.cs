using System;
using DungeonTeam.Gameplay.Combat.Domain;
using NUnit.Framework;

namespace DungeonTeam.Gameplay.Combat.Tests
{
    public sealed class AttackCooldownTests
    {
        [Test]
        public void Create_WithNonPositiveDuration_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new AttackCooldown(0f));
        }

        [Test]
        public void TryConsume_WhenReady_StartsCooldown()
        {
            var cooldown = new AttackCooldown(1f);

            Assert.That(cooldown.TryConsume(), Is.True);
            Assert.That(cooldown.IsReady, Is.False);
            Assert.That(cooldown.TryConsume(), Is.False);
        }

        [Test]
        public void Tick_AfterDuration_MakesCooldownReady()
        {
            var cooldown = new AttackCooldown(1f);
            cooldown.TryConsume();

            cooldown.Tick(1f);

            Assert.That(cooldown.IsReady, Is.True);
        }
    }
}
