using System;
using DungeonTeam.Gameplay.Contracts.Domain;
using DungeonTeam.Gameplay.GuildHall.Application;
using DungeonTeam.Gameplay.AmbientNpc.Application;
using DungeonTeam.UI.WorldMap;
using NUnit.Framework;

namespace DungeonTeam.Gameplay.GuildHall.Tests.EditMode
{
    public sealed class GuildContentValidationTests
    {
        [Test]
        public void Validate_NpcReferencesUnknownDialoguePool_ThrowsSpecificError()
        {
            var guildHall = CreateGuildHall("dialogue.missing", "ambient.idle");
            var dialogues = new DialogueCatalog(new[]
            {
                new DialoguePoolSnapshot("dialogue.other", new[] { Line("line.other") })
            });

            var exception = Assert.Throws<InvalidOperationException>(() =>
                GuildContentValidator.Validate(
                    guildHall,
                    dialogues,
                    CreateProfiles(), CreateContracts(),
                    new[] { "location.dungeon" }));

            StringAssert.Contains("dialogue.missing", exception.Message);
            StringAssert.Contains("npc.registrar", exception.Message);
        }

        [Test]
        public void Validate_NpcReferencesUnknownAmbientProfile_ThrowsSpecificError()
        {
            var guildHall = CreateGuildHall("dialogue.registrar", "ambient.missing");
            var dialogues = new DialogueCatalog(new[]
            {
                new DialoguePoolSnapshot("dialogue.registrar", new[] { Line("line.hello") })
            });

            var exception = Assert.Throws<InvalidOperationException>(() =>
                GuildContentValidator.Validate(
                    guildHall,
                    dialogues,
                    CreateProfiles(), CreateContracts(),
                    new[] { "location.dungeon" }));

            StringAssert.Contains("ambient.missing", exception.Message);
        }

        [Test]
        public void Validate_ContractReferencesUnsupportedLocation_ThrowsSpecificError()
        {
            var guildHall = CreateGuildHall("dialogue.registrar", "ambient.idle");
            var dialogues = new DialogueCatalog(new[]
            {
                new DialoguePoolSnapshot("dialogue.registrar", new[] { Line("line.hello") })
            });

            var exception = Assert.Throws<InvalidOperationException>(() =>
                GuildContentValidator.Validate(
                    guildHall,
                    dialogues,
                    CreateProfiles(), CreateContracts(),
                    new[] { "location.guild-hall" }));

            StringAssert.Contains("contract.demo", exception.Message);
            StringAssert.Contains("location.dungeon", exception.Message);
        }

        [Test]
        public void WorldMapCatalog_VariableLocations_PreservesOrderAndContractDestinations()
        {
            var locations = new[]
            {
                new WorldLocationSnapshot(
                    "location.guild-hall",
                    TextMap("location.guild-hall.title"),
                    TextMap("location.guild-hall.description"),
                    true,
                    null,
                    WorldLocationDestinationKind.GuildHall,
                    null),
                new WorldLocationSnapshot(
                    "location.dungeon",
                    TextMap("location.dungeon.title"),
                    TextMap("location.dungeon.description"),
                    true,
                    null,
                    WorldLocationDestinationKind.DungeonRun,
                    "launch.demo")
            };

            var catalog = new WorldMapCatalog(
                locations,
                new WorldMapUiTextSnapshot(
                    TextMap("world-map.title"),
                    TextMap("world-map.back"),
                    TextMap("world-map.empty")));

            Assert.That(catalog.Locations, Has.Count.EqualTo(locations.Length));
            Assert.That(catalog.Locations[0].LocationId, Is.EqualTo("location.guild-hall"));
            Assert.That(catalog.ContractDestinationLocationIds, Is.EquivalentTo(
                new[] { "location.dungeon" }));
        }

        [Test]
        public void WorldLocation_DungeonWithoutLaunchPreset_Throws()
        {
            Assert.Throws<ArgumentException>(() => new WorldLocationSnapshot(
                "location.dungeon",
                TextMap("title"),
                TextMap("description"),
                true,
                null,
                WorldLocationDestinationKind.DungeonRun,
                null));
        }

        private static GuildHallCatalog CreateGuildHall(
            string dialoguePoolId,
            string ambientProfileId)
        {
            return new GuildHallCatalog(
                new[]
                {
                    new AmbientNpcSnapshot(
                        "npc.registrar",
                        new AmbientTextSnapshot("npc.registrar.name", "npc.registrar.name"),
                        dialoguePoolId,
                        ambientProfileId)
                },
                new GuildHallMovementSettings(4f, 16f, 0.1f),
                new GuildInteractionLabels(
                    Text("interaction.npc"),
                    Text("interaction.board"),
                    Text("interaction.reception"),
                    Text("interaction.exit")),
                CreateNoticeBoardText(),
                CreateRunSummaryText(),
                CreateProfileText());
        }

        private static ContractCatalog CreateContracts()
        {
            return new ContractCatalog(new[]
            {
                new ContractDefinition(
                    "contract.demo",
                    new ContractTextSnapshot("contract.demo.title", "contract.demo.title"),
                    new ContractTextSnapshot("contract.demo.summary", "contract.demo.summary"),
                    "location.dungeon",
                    true,
                    null)
            });
        }

        private static GuildTextSnapshot Text(string id)
        {
            return new GuildTextSnapshot(id, id);
        }

        private static NoticeBoardTextSnapshot CreateNoticeBoardText()
        {
            return new NoticeBoardTextSnapshot(
                Text("notice.header"),
                Text("notice.select"),
                Text("notice.selected"),
                Text("notice.close"),
                Text("notice.empty"));
        }

        private static GuildRunSummaryTextSnapshot CreateRunSummaryText() => new(
            Text("summary.header"), Text("summary.completed"), Text("summary.defeated"),
            Text("summary.dungeon"), Text("summary.rewards"), "{0} x{1}",
            Text("summary.empty"), Text("summary.close"));

        private static GuildProfileTextSnapshot CreateProfileText() => new(
            Text("profile.header"),
            Text("profile.gold"),
            Text("profile.rank"),
            Text("profile.rank.unassigned"),
            Text("profile.leader"),
            Text("profile.leader.explanation"),
            Text("profile.team"),
            Text("profile.roster"),
            Text("profile.available"),
            Text("profile.level"),
            Text("profile.health"),
            Text("profile.speed"),
            Text("profile.skill.primary"),
            Text("profile.skill.active"),
            Text("profile.close"),
            Text("profile.make-leader"),
            Text("profile.add-companion"),
            Text("profile.remove-companion"),
            Text("profile.loadout"),
            Text("profile.rejection.team-size"),
            Text("profile.rejection.invalid-actor"),
            Text("profile.rejection.invalid-loadout"),
            Text("profile.rejection.persistence"));

        private static DialogueLineSnapshot Line(string id) => new(id, id);

        private static AmbientNpcProfileCatalog CreateProfiles()
        {
            return new AmbientNpcProfileCatalog(new[]
            {
                new AmbientNpcProfileSnapshot("ambient.idle", 1f, 90f, 0f, 1f, 0f, 1f, false)
            });
        }

        private static WorldMapTextSnapshot TextMap(string id)
        {
            return new WorldMapTextSnapshot(id, id);
        }
    }
}
