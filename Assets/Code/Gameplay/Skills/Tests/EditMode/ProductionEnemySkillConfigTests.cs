using DungeonTeam.Gameplay.Skills.Domain;
using DungeonTeam.Gameplay.Skills.Runtime;
using NUnit.Framework;
using UnityEditor;

namespace DungeonTeam.Gameplay.Skills.Tests
{
    public sealed class ProductionEnemySkillConfigTests
    {
        private const string ConfigPath =
            "Assets/Content/Configuration/SkillConfig.asset";

        [Test]
        public void SkeletonAreaLoadout_PrimaryUsesTelegraphedAreaDamage()
        {
            var config = AssetDatabase.LoadAssetAtPath<SkillConfigPage>(ConfigPath);

            Assert.That(config, Is.Not.Null, $"Skill config is missing at '{ConfigPath}'.");
            var resolved = config.CreateCatalog().Resolve(
                "loadout.skeleton.area",
                SkillSlot.Primary);

            Assert.That(resolved.Skill.SkillId, Is.EqualTo("skill.blast.skeleton"));
            Assert.That(resolved.Skill, Is.TypeOf<AreaDamageSkillDefinition>());
            Assert.That(resolved.Level, Is.TypeOf<AreaDamageSkillLevelDefinition>());
            var level = (AreaDamageSkillLevelDefinition)resolved.Level;
            Assert.That(level.Range, Is.EqualTo(1.3f));
            Assert.That(level.Radius, Is.EqualTo(1.3f));
            Assert.That(level.UseTiming.CommitDelay, Is.EqualTo(0.35f));
        }
    }
}
