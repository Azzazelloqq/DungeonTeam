using DungeonTeam.Gameplay.Skills.Domain;
using DungeonTeam.Gameplay.Skills.Runtime;
using NUnit.Framework;
using System.Linq;
using UnityEditor;

namespace DungeonTeam.Gameplay.Skills.Tests
{
    public sealed class ProductionHeroSkillConfigTests
    {
        private const string ConfigPath =
            "Assets/Content/Configuration/SkillConfig.asset";
        private const string PresentationRoot =
            "Assets/Content/Gameplay/Skills/Presentation";

        [Test]
        public void ProductionSkillPresentations_LoadAsTypedAssets()
        {
            var presentationGuids = AssetDatabase.FindAssets(
                "t:SkillPresentationAsset",
                new[] { PresentationRoot });

            Assert.That(presentationGuids, Has.Length.EqualTo(10));
            for (var index = 0; index < presentationGuids.Length; index++)
            {
                var path = AssetDatabase.GUIDToAssetPath(presentationGuids[index]);
                Assert.That(
                    AssetDatabase.LoadAssetAtPath<SkillPresentationAsset>(path),
                    Is.Not.Null,
                    $"Skill presentation at '{path}' has an invalid script type.");
            }
        }

        [Test]
        public void ProductionSkillCatalog_ExposesEverySkillSortedByDisplayName()
        {
            var config = AssetDatabase.LoadAssetAtPath<SkillConfigPage>(ConfigPath);

            var skills = config.CreateCatalog().Skills;
            var displayNames = skills.Select(skill => skill.DisplayName).ToArray();
            var sortedDisplayNames = displayNames
                .OrderBy(displayName => displayName, System.StringComparer.Ordinal)
                .ToArray();

            Assert.That(skills, Has.Count.EqualTo(10));
            Assert.That(skills.Select(skill => skill.SkillId), Is.Unique);
            Assert.That(displayNames, Is.EqualTo(sortedDisplayNames));
        }

        [TestCase("loadout.king", "skill.strike.king", "skill.smite.king")]
        [TestCase("loadout.druid", "skill.bolt.druid", "skill.lance.druid")]
        [TestCase("loadout.druid.healer", "skill.bolt.druid", "skill.heal.druid")]
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

        [Test]
        public void DruidHealerLoadout_UsesTypedDirectHealInActiveSlot()
        {
            var config = AssetDatabase.LoadAssetAtPath<SkillConfigPage>(ConfigPath);

            var resolved = config.CreateCatalog().Resolve(
                "loadout.druid.healer",
                SkillSlot.Active1);

            Assert.That(resolved.Skill, Is.TypeOf<DirectHealSkillDefinition>());
            Assert.That(resolved.Level, Is.TypeOf<DirectHealSkillLevelDefinition>());
            Assert.That(resolved.Skill.TargetRule, Is.EqualTo(SkillTargetRule.AllyOrSelfActor));
        }
    }
}
