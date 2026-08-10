using DungeonTeam.Gameplay.Skills.Domain;
using DungeonTeam.Gameplay.Skills.Runtime;
using NUnit.Framework;
using UnityEditor;

namespace DungeonTeam.Gameplay.Skills.Tests
{
    public sealed class ProductionHeroSkillConfigTests
    {
        private const string ConfigPath =
            "Assets/Content/Configuration/SkillConfig.asset";

        [TestCase("loadout.king", "skill.strike.king", "skill.smite.king")]
        [TestCase("loadout.druid", "skill.bolt.druid", "skill.lance.druid")]
        [TestCase("loadout.rogue", "skill.strike.rogue", "skill.knife.rogue")]
        [TestCase("loadout.wizard", "skill.bolt.arcane", "skill.fireball")]
        public void HeroLoadout_ContainsDistinctPrimaryAndActiveSkill(
            string loadoutId,
            string expectedPrimarySkillId,
            string expectedActiveSkillId)
        {
            var config = AssetDatabase.LoadAssetAtPath<SkillConfigPage>(ConfigPath);

            Assert.That(config, Is.Not.Null, $"Skill config is missing at '{ConfigPath}'.");
            var catalog = config.CreateCatalog();
            var primary = catalog.Resolve(loadoutId, SkillSlot.Primary);
            var active = catalog.Resolve(loadoutId, SkillSlot.Active1);

            Assert.That(primary.Skill.SkillId, Is.EqualTo(expectedPrimarySkillId));
            Assert.That(active.Skill.SkillId, Is.EqualTo(expectedActiveSkillId));
            Assert.That(active.Skill.SkillId, Is.Not.EqualTo(primary.Skill.SkillId));
        }

        [TestCase("skill.bolt.druid")]
        [TestCase("skill.lance.druid")]
        [TestCase("skill.knife.rogue")]
        [TestCase("skill.bolt.arcane")]
        [TestCase("skill.fireball")]
        public void RangedHeroSkill_UsesProjectileMechanic(string skillId)
        {
            var config = AssetDatabase.LoadAssetAtPath<SkillConfigPage>(ConfigPath);

            Assert.That(config, Is.Not.Null, $"Skill config is missing at '{ConfigPath}'.");
            Assert.That(
                config.CreateCatalog().RequireSkill(skillId),
                Is.TypeOf<ProjectileDamageSkillDefinition>());
        }
    }
}
