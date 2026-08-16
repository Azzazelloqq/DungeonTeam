using System;
using System.Collections.Generic;
using DungeonTeam.Gameplay.GuildHall.Application;
using DungeonTeam.Gameplay.AmbientNpc.Application;
using NUnit.Framework;

namespace DungeonTeam.Gameplay.GuildHall.Tests.EditMode
{
    public sealed class GuildHallApplicationTests
    {
        [Test]
        public void Snapshot_EmptyStableId_Throws()
        {
            Assert.Throws<ArgumentException>(() => new GuildTextSnapshot(" ", "Текст"));
            Assert.Throws<ArgumentException>(() => new AmbientNpcSnapshot(
                string.Empty,
                new AmbientTextSnapshot("npc.name", "Регистратор"),
                "dialogue.registrar",
                "ambient.idle"));
        }

        [Test]
        public void StartContext_SourceCollectionsChange_RemainsUnchanged()
        {
            var npcs = new List<AmbientNpcSnapshot>
            {
                CreateNpc("npc.registrar")
            };
            var offers = new List<NoticeBoardOfferSnapshot>
            {
                CreateOffer("contract.demo")
            };
            var context = new GuildHallStartContext(npcs, offers, null, null);

            npcs.Add(CreateNpc("npc.visitor"));
            offers.Clear();

            Assert.That(context.Npcs, Has.Count.EqualTo(1));
            Assert.That(context.Npcs[0].NpcId, Is.EqualTo("npc.registrar"));
            Assert.That(context.Offers, Has.Count.EqualTo(1));
            Assert.That(context.Offers[0].ContractId, Is.EqualTo("contract.demo"));
        }

        [TestCase(1)]
        [TestCase(3)]
        public void ContextBuilder_VariableCatalogSize_PreservesEveryEntry(int count)
        {
            var npcs = new AmbientNpcSnapshot[count];
            var offers = new NoticeBoardOfferSnapshot[count];
            for (var index = 0; index < count; index++)
            {
                npcs[index] = CreateNpc($"npc.{index}");
                offers[index] = CreateOffer($"contract.{index}");
            }

            var guildCatalog = new GuildHallCatalog(
                npcs,
                new GuildHallMovementSettings(4f, 16f, 0.1f),
                CreateInteractionLabels(),
                CreateNoticeBoardText(),
                CreateRunSummaryText(),
                CreateProfileText());
            var contractCatalog = new ContractCatalog(offers);
            var sessionState = new GuildSessionState();

            var context = GuildHallStartContextBuilder.Build(
                guildCatalog,
                contractCatalog,
                sessionState);

            Assert.That(context.Npcs, Has.Count.EqualTo(count));
            Assert.That(context.Offers, Has.Count.EqualTo(count));
        }

        [Test]
        public void SessionState_SelectContract_StoresOnlySessionValues()
        {
            var state = new GuildSessionState();
            var summary = new GuildRunSummarySnapshot(
                new GuildTextSnapshot("run-summary.outcome.completed", "Завершено"),
                new GuildTextSnapshot("dungeon.demo", "Учебное подземелье"),
                new[] { new GuildTextSnapshot("reward.gold", "Золото: 5") },
                CreateRunSummaryText());

            state.SelectContract("contract.demo");
            state.SetLastRunSummary(summary);

            Assert.That(state.SelectedContractId, Is.EqualTo("contract.demo"));
            Assert.That(state.LastRunSummary, Is.SameAs(summary));
            Assert.Throws<ArgumentException>(() => state.SelectContract(""));
        }

        [TestCase("{0}")]
        [TestCase("{1}")]
        [TestCase("Без подстановок")]
        [TestCase("{{0}} {1}")]
        [TestCase("{0} {{1}}")]
        public void RunSummaryText_FormatMissingRequiredValue_Throws(string rewardLineFormat)
        {
            Assert.Throws<ArgumentException>(() => CreateRunSummaryText(rewardLineFormat));
        }

        private static AmbientNpcSnapshot CreateNpc(string id)
        {
            return new AmbientNpcSnapshot(
                id,
                new AmbientTextSnapshot($"{id}.name", id),
                "dialogue.registrar",
                "ambient.idle");
        }

        private static NoticeBoardOfferSnapshot CreateOffer(string id)
        {
            return new NoticeBoardOfferSnapshot(
                id,
                new GuildTextSnapshot($"{id}.title", id),
                new GuildTextSnapshot($"{id}.summary", "Описание"),
                "location.dungeon",
                true,
                null);
        }

        private static GuildInteractionLabels CreateInteractionLabels()
        {
            return new GuildInteractionLabels(
                new GuildTextSnapshot("interaction.npc", "Поговорить"),
                new GuildTextSnapshot("interaction.board", "Доска"),
                new GuildTextSnapshot("interaction.reception", "Стойка"),
                new GuildTextSnapshot("interaction.exit", "Выйти"));
        }

        private static NoticeBoardTextSnapshot CreateNoticeBoardText()
        {
            return new NoticeBoardTextSnapshot(
                new GuildTextSnapshot("notice.header", "Контракты"),
                new GuildTextSnapshot("notice.select", "Выбрать"),
                new GuildTextSnapshot("notice.selected", "Выбрано"),
                new GuildTextSnapshot("notice.close", "Закрыть"),
                new GuildTextSnapshot("notice.empty", "Нет контрактов"));
        }

        private static GuildRunSummaryTextSnapshot CreateRunSummaryText(
            string rewardLineFormat = "{0} x{1}") => new(
            new GuildTextSnapshot("summary.header", "Итог"),
            new GuildTextSnapshot("summary.completed", "Завершено"),
            new GuildTextSnapshot("summary.defeated", "Поражение"),
            new GuildTextSnapshot("summary.dungeon", "Данж"),
            new GuildTextSnapshot("summary.rewards", "Награды"),
            rewardLineFormat,
            new GuildTextSnapshot("summary.empty", "Нет"),
            new GuildTextSnapshot("summary.close", "Закрыть"));

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
            Text("profile.close"));

        private static GuildTextSnapshot Text(string id) => new(id, id);
    }
}
