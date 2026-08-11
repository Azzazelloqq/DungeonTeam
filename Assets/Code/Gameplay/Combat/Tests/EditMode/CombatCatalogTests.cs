using System;
using DungeonTeam.Gameplay.Combat.Runtime;
using NUnit.Framework;

namespace DungeonTeam.Gameplay.Combat.Tests
{
    public sealed class CombatCatalogTests
    {
        [Test]
        public void CreateCatalog_WithDuplicateAttackIds_Throws()
        {
            var attack = CreateAttack("attack.basic", 1f);

            Assert.Throws<ArgumentException>(() => new CombatCatalog(
                new[] { attack, CreateAttack("attack.basic", 2f) },
                Array.Empty<CombatLoadoutDefinitionConfig>()));
        }

        [Test]
        public void CreateCatalog_WithDuplicateRanks_Throws()
        {
            var attack = new AttackDefinitionConfig(
                "attack.basic",
                "BASIC",
                new[]
                {
                    new AttackRankDefinitionConfig(1, 10, 1f, 1f),
                    new AttackRankDefinitionConfig(1, 20, 2f, 1f)
                });

            Assert.Throws<ArgumentException>(() => new CombatCatalog(
                new[] { attack },
                Array.Empty<CombatLoadoutDefinitionConfig>()));
        }

        [Test]
        public void CreateCatalog_WithUnknownLoadoutAttack_Throws()
        {
            Assert.Throws<ArgumentException>(() => new CombatCatalog(
                new[] { CreateAttack("attack.basic", 1f) },
                new[]
                {
                    new CombatLoadoutDefinitionConfig(
                        "loadout.basic",
                        "attack.unknown")
                }));
        }

        [Test]
        public void ResolvePrimaryAttack_WithDifferentRanks_ReturnsConfiguredValues()
        {
            var catalog = new CombatCatalog(
                new[]
                {
                    new AttackDefinitionConfig(
                        "attack.basic",
                        "BASIC",
                        new[]
                        {
                            new AttackRankDefinitionConfig(1, 10, 1.5f, 1f),
                            new AttackRankDefinitionConfig(2, 16, 2f, 0.8f)
                        })
                },
                new[]
                {
                    new CombatLoadoutDefinitionConfig(
                        "loadout.basic",
                        "attack.basic")
                });

            var first = catalog.ResolvePrimaryAttack("loadout.basic", 1);
            var second = catalog.ResolvePrimaryAttack("loadout.basic", 2);

            Assert.That(first.Damage, Is.EqualTo(10));
            Assert.That(second.Damage, Is.EqualTo(16));
            Assert.That(second.Range, Is.EqualTo(2f));
            Assert.That(second.Cooldown, Is.EqualTo(0.8f));
        }

        [Test]
        public void ResolvePrimaryAttack_WithUnknownRank_Throws()
        {
            var catalog = new CombatCatalog(
                new[] { CreateAttack("attack.basic", 1f) },
                new[]
                {
                    new CombatLoadoutDefinitionConfig(
                        "loadout.basic",
                        "attack.basic")
                });

            Assert.Throws<InvalidOperationException>(() =>
                catalog.ResolvePrimaryAttack("loadout.basic", 2));
        }

        private static AttackDefinitionConfig CreateAttack(string id, float range)
        {
            return new AttackDefinitionConfig(
                id,
                "BASIC",
                new[] { new AttackRankDefinitionConfig(1, 10, range, 1f) });
        }
    }
}
