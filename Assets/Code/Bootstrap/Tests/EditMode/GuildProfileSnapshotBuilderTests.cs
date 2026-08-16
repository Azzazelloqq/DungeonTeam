using System;
using Code.ApplicationRoot;
using DungeonTeam.Gameplay.Actors.Runtime;
using DungeonTeam.Gameplay.GuildHall.Application;
using DungeonTeam.Gameplay.PlayerProfile.Domain;
using DungeonTeam.Gameplay.Skills.Domain;
using DungeonTeam.Gameplay.Skills.Runtime;
using NUnit.Framework;

namespace Code.ApplicationRoot.Tests.EditMode
{
    public sealed class GuildProfileSnapshotBuilderTests
    {
        [Test]
        public void Build_UsesSavedRosterOnlyAndActualDefinitions()
        {
            var actors = new ActorConfigCatalog(new[]
            {
                new ActorDefinitionConfig(
                    "hero",
                    "Hero",
                    new[] { new ActorLevelDefinitionConfig(2, 42, 3f) }),
                new ActorDefinitionConfig(
                    "enemy",
                    "Enemy",
                    new[] { new ActorLevelDefinitionConfig(1, 99, 1f) })
            });
            var skills = new SkillCatalog(
                new[]
                {
                    new DirectDamageSkillDefinitionConfig(
                        "hit",
                        "Hit",
                        SkillTargetRule.EnemyActor,
                        new[] { new DirectDamageSkillLevelConfig(1, 7, 2f, 1f) })
                },
                Array.Empty<ProjectileDamageSkillDefinitionConfig>(),
                Array.Empty<DirectHealSkillDefinitionConfig>(),
                new[]
                {
                    new CombatLoadoutDefinitionConfig(
                        "main",
                        new[] { new CombatLoadoutSlotConfig(SkillSlot.Primary, "hit", 1) })
                });
            var profile = new PlayerProfileState(
                5,
                null,
                new[] { new HeroProfileState("hero", 2, "main") },
                "hero",
                Array.Empty<string>());

            var result = GuildProfileSnapshotBuilder.Build(
                profile,
                actors,
                skills,
                CreateProfileText());

            Assert.That(result.Roster, Has.Count.EqualTo(profile.Heroes.Count));
            Assert.That(result.Leader.MaximumHealth, Is.EqualTo(42));
            Assert.That(result.Leader.Skills[0].DisplayName, Is.EqualTo("Hit"));
            Assert.That(result.Leader.Skills[0].SlotDisplayText, Is.EqualTo("Primary"));
        }

        private static GuildProfileTextSnapshot CreateProfileText() => new(
            Text("profile.header", "Profile"),
            Text("profile.gold", "Gold"),
            Text("profile.rank", "Rank"),
            Text("profile.rank.unassigned", "-"),
            Text("profile.leader", "Leader"),
            Text("profile.leader.explanation", "Controlled hero"),
            Text("profile.team", "Team"),
            Text("profile.roster", "Roster"),
            Text("profile.available", "Available"),
            Text("profile.level", "Level"),
            Text("profile.health", "Health"),
            Text("profile.speed", "Speed"),
            Text("profile.skill.primary", "Primary"),
            Text("profile.skill.active", "Active"),
            Text("profile.close", "Close"));

        private static GuildTextSnapshot Text(string id, string value) => new(id, value);
    }
}
