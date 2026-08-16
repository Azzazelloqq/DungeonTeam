using NUnit.Framework;
using DungeonTeam.Gameplay.DungeonRun.Runtime;
using DungeonTeam.Gameplay.GuildHall.Application;
using DungeonTeam.Gameplay.Rewards.Runtime;

namespace Code.ApplicationRoot.Tests.EditMode
{
    public sealed class GuildRunSummaryBuilderTests
    {
        [Test]
        public void Build_CompletedRunWithVariableRewards_UsesConfiguredTextAndCatalogNames()
        {
            var input = new[] { new RewardGrant("reward.crystal", 3), new RewardGrant("reward.gold", 17) };
            var result = new DungeonRunResult(DungeonRunOutcome.Completed, "dungeon.crypt", 4, 0, input);
            var summary = new GuildRunSummaryBuilder().Build(result, CreateRewards(), CreateText());

            Assert.That(summary.Outcome.DisplayText, Is.EqualTo("Победа"));
            Assert.That(summary.Dungeon.TextId, Is.EqualTo("dungeon.crypt"));
            Assert.That(summary.Dungeon.DisplayText, Is.EqualTo("dungeon.crypt"));
            Assert.That(summary.RewardLines, Has.Count.EqualTo(input.Length));
            Assert.That(summary.RewardLines[0].DisplayText, Is.EqualTo("Кристалл x3"));
            Assert.That(summary.RewardLines[1].DisplayText, Is.EqualTo("Золото x17"));
            Assert.That(result.CollectedRewards[0].Amount, Is.EqualTo(3));
        }

        [Test]
        public void Build_DefeatWithNoRewards_PreservesEmptySnapshot()
        {
            var result = new DungeonRunResult(
                DungeonRunOutcome.Defeated, "dungeon.ruins", 8, 2, System.Array.Empty<RewardGrant>());

            var summary = new GuildRunSummaryBuilder().Build(result, CreateRewards(), CreateText());

            Assert.That(summary.Outcome.DisplayText, Is.EqualTo("Поражение"));
            Assert.That(summary.RewardLines, Is.Empty);
        }

        [Test]
        public void Build_UnknownReward_RejectsWholeSummary()
        {
            var result = new DungeonRunResult(
                DungeonRunOutcome.Completed, "dungeon.ruins", 8, 2,
                new[] { new RewardGrant("reward.unknown", 1) });

            Assert.Throws<System.InvalidOperationException>(() =>
                new GuildRunSummaryBuilder().Build(result, CreateRewards(), CreateText()));
        }

        private static RewardCatalog CreateRewards() => new(new[]
        {
            new RewardDefinition("reward.crystal", "Кристалл"),
            new RewardDefinition("reward.gold", "Золото")
        });

        private static GuildRunSummaryTextSnapshot CreateText() => new(
            new GuildTextSnapshot("summary.header", "Итог"),
            new GuildTextSnapshot("summary.completed", "Победа"),
            new GuildTextSnapshot("summary.defeated", "Поражение"),
            new GuildTextSnapshot("summary.dungeon", "Подземелье"),
            new GuildTextSnapshot("summary.rewards", "Награды"),
            "{0} x{1}",
            new GuildTextSnapshot("summary.empty", "Нет наград"),
            new GuildTextSnapshot("summary.close", "Закрыть"));
    }
}
