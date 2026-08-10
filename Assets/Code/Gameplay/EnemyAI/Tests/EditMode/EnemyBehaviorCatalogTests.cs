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
                Definition("behavior.enemy.melee.basic", viewDistance: 8f),
                Definition("behavior.enemy.melee.basic", viewDistance: 10f)
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
                Definition("behavior.enemy.melee.basic", viewDistance: 8f)
            });

            var exception = Assert.Throws<InvalidOperationException>(
                () => catalog.Require("behavior.enemy.unknown"));

            StringAssert.Contains("behavior.enemy.unknown", exception.Message);
        }

        [Test]
        public void Require_WithMeleeAndRangedProfiles_ReturnsDifferentDecisionProfiles()
        {
            var catalog = new EnemyBehaviorCatalog(new[]
            {
                Definition("behavior.enemy.melee.basic", viewDistance: 8f),
                Definition("behavior.enemy.ranged.basic", viewDistance: 12f)
            });

            var melee = catalog.Require("behavior.enemy.melee.basic");
            var ranged = catalog.Require("behavior.enemy.ranged.basic");

            Assert.That(melee.ViewDistance, Is.EqualTo(8f));
            Assert.That(ranged.ViewDistance, Is.EqualTo(12f));
        }

        private static EnemyBehaviorDefinition Definition(
            string behaviorId,
            float viewDistance)
        {
            return new EnemyBehaviorDefinition(
                behaviorId,
                new EnemyAiSettings(
                    viewDistance: viewDistance,
                    viewAngle: 90f,
                    targetLossDistance: 15f));
        }
    }
}
