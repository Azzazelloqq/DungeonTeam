using System;
using DungeonTeam.Gameplay.Skills.Domain;
using DungeonTeam.Gameplay.Skills.Runtime;
using NUnit.Framework;

namespace DungeonTeam.Gameplay.Skills.Tests
{
    public sealed class SkillCatalogTests
    {
        [Test]
        public void Create_WithDuplicateSkillIdAcrossMechanics_Throws()
        {
            Assert.Throws<ArgumentException>(() => new SkillCatalog(
                new[] { Direct("skill.shared") },
                new[] { Projectile("skill.shared") },
                Array.Empty<DirectHealSkillDefinitionConfig>(),
                Array.Empty<CombatLoadoutDefinitionConfig>()));
        }

        [Test]
        public void Create_WithDuplicateSkillIdAcrossDirectAndArea_Throws()
        {
            Assert.Throws<ArgumentException>(() => new SkillCatalog(
                new[] { Direct("skill.shared") },
                new[] { Area("skill.shared") },
                Array.Empty<ProjectileDamageSkillDefinitionConfig>(),
                Array.Empty<DirectHealSkillDefinitionConfig>(),
                Array.Empty<CombatLoadoutDefinitionConfig>()));
        }

        [Test]
        public void CreateSkill_WithMissingId_Throws()
        {
            Assert.Throws<ArgumentException>(() => new DirectDamageSkillDefinition(
                string.Empty,
                "Strike",
                SkillTargetRule.EnemyActor,
                new[] { new DirectDamageSkillLevelDefinition(1, 10, 1.5f, 1f) }));
        }

        [Test]
        public void Create_WithDuplicateSkillLevel_Throws()
        {
            var skill = new DirectDamageSkillDefinitionConfig(
                "skill.strike",
                "Strike",
                SkillTargetRule.EnemyActor,
                new[]
                {
                    new DirectDamageSkillLevelConfig(1, 10, 1.5f, 1f),
                    new DirectDamageSkillLevelConfig(1, 20, 2f, 0.5f)
                });

            Assert.Throws<ArgumentException>(() => new SkillCatalog(
                new[] { skill },
                Array.Empty<ProjectileDamageSkillDefinitionConfig>(),
                Array.Empty<DirectHealSkillDefinitionConfig>(),
                Array.Empty<CombatLoadoutDefinitionConfig>()));
        }

        [TestCase(0, 10, 1f, 1f)]
        [TestCase(1, 0, 1f, 1f)]
        [TestCase(1, 10, 0f, 1f)]
        [TestCase(1, 10, 1f, 0f)]
        public void CreateDirectSkill_WithInvalidTypedParameters_Throws(
            int level,
            int damage,
            float range,
            float cooldown)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new DirectDamageSkillLevelDefinition(level, damage, range, cooldown));
        }

        [Test]
        public void CreateCommonSkillLevel_DoesNotRequireDamage()
        {
            var level = new TestSkillLevelDefinition(1, 2f, 3f);

            Assert.That(level.Level, Is.EqualTo(1));
            Assert.That(level.Range, Is.EqualTo(2f));
            Assert.That(level.Cooldown, Is.EqualTo(3f));
        }

        [Test]
        public void CreateProjectileSkill_WithInvalidSpeed_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ProjectileDamageSkillLevelDefinition(1, 10, 4f, 1f, 0f));
        }

        [Test]
        public void CreateProjectileSkill_WithInvalidDamage_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ProjectileDamageSkillLevelDefinition(1, 0, 4f, 1f, 8f));
        }

        [Test]
        public void CreateAreaDamage_WithInvalidRadiusOrTargetRule_Throws()
        {
            var timing = new SkillUseTiming(0.2f, 0.1f);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new AreaDamageSkillLevelDefinition(1, 10, 2f, 1f, 0f, timing));
            Assert.Throws<ArgumentException>(() => new AreaDamageSkillDefinition(
                "skill.area",
                "Area",
                SkillTargetRule.AllyOrSelfActor,
                new[] { new AreaDamageSkillLevelDefinition(1, 10, 2f, 1f, 1f, timing) }));
        }

        [Test]
        public void CreateDirectHeal_WithInvalidAmountOrTargetRule_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new DirectHealSkillLevelDefinition(1, 0, 4f, 1f));
            Assert.Throws<ArgumentException>(() => new DirectHealSkillDefinition(
                "skill.heal",
                "Heal",
                SkillTargetRule.EnemyActor,
                new[] { new DirectHealSkillLevelDefinition(1, 10, 4f, 1f) }));
        }

        [Test]
        public void Resolve_DirectHealLevelsChangeObservableParameters()
        {
            var heal = new DirectHealSkillDefinitionConfig(
                "skill.heal",
                "Heal",
                SkillTargetRule.AllyOrSelfActor,
                new[]
                {
                    new DirectHealSkillLevelConfig(1, 10, 4f, 3f),
                    new DirectHealSkillLevelConfig(2, 18, 5f, 2f)
                });
            var catalog = new SkillCatalog(
                Array.Empty<DirectDamageSkillDefinitionConfig>(),
                Array.Empty<ProjectileDamageSkillDefinitionConfig>(),
                new[] { heal },
                new[]
                {
                    Loadout("skill.heal", 1, "loadout.heal.one"),
                    Loadout("skill.heal", 2, "loadout.heal.two")
                });

            var first = (DirectHealSkillLevelDefinition)catalog
                .Resolve("loadout.heal.one", SkillSlot.Primary)
                .Level;
            var second = (DirectHealSkillLevelDefinition)catalog
                .Resolve("loadout.heal.two", SkillSlot.Primary)
                .Level;

            Assert.That(second.HealAmount, Is.GreaterThan(first.HealAmount));
            Assert.That(second.Range, Is.GreaterThan(first.Range));
            Assert.That(second.Cooldown, Is.LessThan(first.Cooldown));
        }

        [Test]
        public void Create_WithDuplicateSkillIdAcrossDamageAndHeal_Throws()
        {
            Assert.Throws<ArgumentException>(() => new SkillCatalog(
                new[] { Direct("skill.shared") },
                Array.Empty<ProjectileDamageSkillDefinitionConfig>(),
                new[]
                {
                    new DirectHealSkillDefinitionConfig(
                        "skill.shared",
                        "Heal",
                        SkillTargetRule.AllyOrSelfActor,
                        new[] { new DirectHealSkillLevelConfig(1, 10, 4f, 1f) })
                },
                Array.Empty<CombatLoadoutDefinitionConfig>()));
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void CreateSkill_WithNonFiniteTypedParameter_Throws(float value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new DirectDamageSkillLevelDefinition(1, 10, value, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new DirectDamageSkillLevelDefinition(1, 10, 1f, value));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ProjectileDamageSkillLevelDefinition(1, 10, 4f, 1f, value));
        }

        [Test]
        public void CreateLoadout_WithDuplicateSlot_Throws()
        {
            Assert.Throws<ArgumentException>(() => new CombatLoadoutDefinition(
                "loadout.test",
                new[]
                {
                    new CombatLoadoutSlotDefinition(SkillSlot.Primary, "skill.strike", 1),
                    new CombatLoadoutSlotDefinition(SkillSlot.Primary, "skill.fireball", 1)
                }));
        }

        [Test]
        public void CreateCatalog_WithDuplicateLoadoutId_Throws()
        {
            Assert.Throws<ArgumentException>(() => new SkillCatalog(
                new[] { Direct("skill.strike") },
                Array.Empty<ProjectileDamageSkillDefinitionConfig>(),
                Array.Empty<DirectHealSkillDefinitionConfig>(),
                new[]
                {
                    Loadout("skill.strike", 1, "loadout.shared"),
                    Loadout("skill.strike", 1, "loadout.shared")
                }));
        }

        [Test]
        public void CreateLoadout_WithMissingId_Throws()
        {
            Assert.Throws<ArgumentException>(() => new CombatLoadoutDefinition(
                string.Empty,
                new[]
                {
                    new CombatLoadoutSlotDefinition(
                        SkillSlot.Primary,
                        "skill.strike",
                        1)
                }));
        }

        [Test]
        public void CreateCatalog_WithUnknownLoadoutSkill_Throws()
        {
            Assert.Throws<ArgumentException>(() => new SkillCatalog(
                new[] { Direct("skill.strike") },
                Array.Empty<ProjectileDamageSkillDefinitionConfig>(),
                Array.Empty<DirectHealSkillDefinitionConfig>(),
                new[] { Loadout("skill.unknown", 1) }));
        }

        [Test]
        public void CreateCatalog_WithUnknownLoadoutSkillLevel_Throws()
        {
            Assert.Throws<ArgumentException>(() => new SkillCatalog(
                new[] { Direct("skill.strike") },
                Array.Empty<ProjectileDamageSkillDefinitionConfig>(),
                Array.Empty<DirectHealSkillDefinitionConfig>(),
                new[] { Loadout("skill.strike", 2) }));
        }

        [Test]
        public void Resolve_DifferentLevels_ChangeObservableParameters()
        {
            var skill = new DirectDamageSkillDefinitionConfig(
                "skill.strike",
                "Strike",
                SkillTargetRule.EnemyActor,
                new[]
                {
                    new DirectDamageSkillLevelConfig(
                        1, 10, 1.5f, 1f, commitDelay: 0.3f, recoveryDuration: 0.2f),
                    new DirectDamageSkillLevelConfig(
                        2, 18, 2f, 0.7f, commitDelay: 0.2f, recoveryDuration: 0.1f)
                });
            var catalog = new SkillCatalog(
                new[] { skill },
                Array.Empty<ProjectileDamageSkillDefinitionConfig>(),
                Array.Empty<DirectHealSkillDefinitionConfig>(),
                new[]
                {
                    Loadout("skill.strike", 1, "loadout.one"),
                    Loadout("skill.strike", 2, "loadout.two")
                });

            var first = (DirectDamageSkillLevelDefinition)catalog
                .Resolve("loadout.one", SkillSlot.Primary)
                .Level;
            var second = (DirectDamageSkillLevelDefinition)catalog
                .Resolve("loadout.two", SkillSlot.Primary)
                .Level;

            Assert.That(second.Damage, Is.GreaterThan(first.Damage));
            Assert.That(second.Range, Is.GreaterThan(first.Range));
            Assert.That(second.Cooldown, Is.LessThan(first.Cooldown));
            Assert.That(
                second.UseTiming.CommitDelay,
                Is.LessThan(first.UseTiming.CommitDelay));
            Assert.That(
                second.UseTiming.RecoveryDuration,
                Is.LessThan(first.UseTiming.RecoveryDuration));
        }

        private static DirectDamageSkillDefinitionConfig Direct(string skillId)
        {
            return new DirectDamageSkillDefinitionConfig(
                skillId,
                "Strike",
                SkillTargetRule.EnemyActor,
                new[] { new DirectDamageSkillLevelConfig(1, 10, 1.5f, 1f) });
        }

        private static ProjectileDamageSkillDefinitionConfig Projectile(string skillId)
        {
            return new ProjectileDamageSkillDefinitionConfig(
                skillId,
                "Fireball",
                SkillTargetRule.EnemyActor,
                new[] { new ProjectileDamageSkillLevelConfig(1, 10, 5f, 1f, 8f) });
        }

        private static AreaDamageSkillDefinitionConfig Area(string skillId)
        {
            return new AreaDamageSkillDefinitionConfig(
                skillId,
                "Area",
                SkillTargetRule.EnemyActor,
                new[] { new AreaDamageSkillLevelConfig(1, 10, 2f, 1f, 1f, 0.2f) });
        }

        private static CombatLoadoutDefinitionConfig Loadout(
            string skillId,
            int level,
            string loadoutId = "loadout.test")
        {
            return new CombatLoadoutDefinitionConfig(
                loadoutId,
                new[] { new CombatLoadoutSlotConfig(SkillSlot.Primary, skillId, level) });
        }

        private sealed class TestSkillLevelDefinition : SkillLevelDefinition
        {
            public TestSkillLevelDefinition(int level, float range, float cooldown)
                : base(level, range, cooldown, default)
            {
            }
        }
    }
}
