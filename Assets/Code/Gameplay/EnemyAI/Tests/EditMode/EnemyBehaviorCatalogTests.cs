using System;
using DungeonTeam.Gameplay.EnemyAI.Runtime;
using NUnit.Framework;

namespace DungeonTeam.Gameplay.EnemyAI.Tests
{
    public sealed class EnemyBehaviorCatalogTests
    {
        [Test]
        public void Create_WithDuplicateBehaviorIds_Throws()
        {
            var definitions = new[]
            {
                Definition("behavior.enemy.melee.basic", attackRange: 1.5f),
                Definition("behavior.enemy.melee.basic", attackRange: 2f)
            };

            var exception = Assert.Throws<ArgumentException>(
                () => new EnemyBehaviorCatalog(definitions));

            StringAssert.Contains("configured more than once", exception.Message);
        }

        [Test]
        public void Require_WithUnknownBehaviorId_ThrowsClearError()
        {
            var catalog = new EnemyBehaviorCatalog(new[]
            {
                Definition("behavior.enemy.melee.basic", attackRange: 1.5f)
            });

            var exception = Assert.Throws<InvalidOperationException>(
                () => catalog.Require("behavior.enemy.unknown"));

            StringAssert.Contains("behavior.enemy.unknown", exception.Message);
        }

        [Test]
        public void Require_WithMeleeAndRangedProfiles_ReturnsDifferentAttackRanges()
        {
            var catalog = new EnemyBehaviorCatalog(new[]
            {
                Definition("behavior.enemy.melee.basic", attackRange: 1.5f),
                Definition("behavior.enemy.ranged.basic", attackRange: 6f)
            });

            var melee = catalog.Require("behavior.enemy.melee.basic");
            var ranged = catalog.Require("behavior.enemy.ranged.basic");

            Assert.That(melee.AttackRange, Is.EqualTo(1.5f));
            Assert.That(ranged.AttackRange, Is.EqualTo(6f));
        }

        private static EnemyBehaviorDefinition Definition(
            string behaviorId,
            float attackRange)
        {
            return new EnemyBehaviorDefinition(
                behaviorId,
                new EnemyAiSettings(
                    viewDistance: 10f,
                    viewAngle: 90f,
                    targetLossDistance: 15f,
                    attackRange: attackRange,
                    attackDamage: 10,
                    attackCooldown: 1f));
        }
    }
}
