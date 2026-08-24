using NUnit.Framework;
using DungeonTeam.Gameplay.DungeonRun.Runtime;
using DungeonTeam.Gameplay.GuildHall.Application;
using DungeonTeam.Gameplay.PlayerProfile.Application;
using DungeonTeam.Gameplay.Rewards.Runtime;

namespace Code.ApplicationRoot.Tests.EditMode
{
    public sealed class GuildRunSummaryBuilderTests
    {
        [Test]
        public void Build_CompletedRunWithVariableRewards_UsesConfiguredTextAndCatalogNames()
        {
            var result = new DungeonRunResult(
                "run-id", DungeonRunOutcome.Completed, "dungeon.crypt", 4, 0,
                System.Array.Empty<RewardGrant>());
            var receipt = new ProfileSettlementReceiptForTests(
                "run-id", 17,
                new[] { new ProfileResourceGrant("resource.monster-crystal", 3) });
            var summary = new GuildRunSummaryBuilder().Build(
                result, receipt.Value, CreateRewards(), CreateText());

            Assert.That(summary.Outcome.DisplayText, Is.EqualTo("Победа"));
            Assert.That(summary.Dungeon.TextId, Is.EqualTo("dungeon.crypt"));
            Assert.That(summary.Dungeon.DisplayText, Is.EqualTo("dungeon.crypt"));
            Assert.That(summary.RewardLines, Has.Count.EqualTo(2));
            Assert.That(summary.RewardLines[0].DisplayText, Is.EqualTo("Золото x17"));
            Assert.That(summary.RewardLines[1].DisplayText, Is.EqualTo("Кристалл x3"));
        }

        [Test]
        public void Build_DefeatWithNoRewards_PreservesEmptySnapshot()
        {
            var result = new DungeonRunResult(
                "run-id", DungeonRunOutcome.Defeated, "dungeon.ruins", 8, 2,
                System.Array.Empty<RewardGrant>());
            var receipt = new ProfileSettlementReceiptForTests(
                "run-id", 0, System.Array.Empty<ProfileResourceGrant>());

            var summary = new GuildRunSummaryBuilder().Build(
                result, receipt.Value, CreateRewards(), CreateText());

            Assert.That(summary.Outcome.DisplayText, Is.EqualTo("Поражение"));
            Assert.That(summary.RewardLines, Is.Empty);
        }

        [Test]
        public void Build_UnknownResource_RejectsWholeSummary()
        {
            var result = new DungeonRunResult(
                "run-id", DungeonRunOutcome.Completed, "dungeon.ruins", 8, 2,
                System.Array.Empty<RewardGrant>());
            var receipt = new ProfileSettlementReceiptForTests(
                "run-id", 0,
                new[] { new ProfileResourceGrant("resource.unknown", 1) });

            Assert.Throws<System.InvalidOperationException>(() =>
                new GuildRunSummaryBuilder().Build(
                    result, receipt.Value, CreateRewards(), CreateText()));
        }

        [Test]
        public void Build_CommittedReceipt_UsesBankedValuesForSummary()
        {
            var result = new DungeonRunResult(
                "run-id",
                DungeonRunOutcome.Completed,
                "dungeon.ruins",
                8,
                0,
                System.Array.Empty<RewardGrant>());
            var receipt = new ProfileSettlementReceiptForTests(
                "run-id",
                17,
                new[] { new ProfileResourceGrant("resource.monster-crystal", 3) });

            var summary = new GuildRunSummaryBuilder().Build(
                result,
                receipt.Value,
                CreateRewardsWithSilver(),
                CreateText());

            Assert.That(summary.RewardLines, Has.Count.EqualTo(2));
            Assert.That(summary.RewardLines[0].DisplayText, Is.EqualTo("Золото x17"));
            Assert.That(summary.RewardLines[1].DisplayText, Is.EqualTo("Кристалл x3"));
        }

        [Test]
        public void Build_ReceiptFromDifferentRun_RejectsSummary()
        {
            var result = new DungeonRunResult(
                "run-a", DungeonRunOutcome.Completed, "dungeon", 1, 0,
                System.Array.Empty<RewardGrant>());
            var receipt = new ProfileSettlementReceiptForTests(
                "run-b", 1, System.Array.Empty<ProfileResourceGrant>());

            Assert.Throws<System.InvalidOperationException>(() => new GuildRunSummaryBuilder().Build(
                result, receipt.Value, CreateRewardsWithSilver(), CreateText()));
        }

        private static RewardCatalog CreateRewards() => new(new[]
        {
            new RewardDefinition("reward.crystal", "Кристалл"),
            new RewardDefinition("reward.gold", "Золото")
        });

        private static RewardCatalog CreateRewardsWithSilver() => new(new[]
        {
            new RewardDefinition("reward.crystal", "Кристалл"),
            new RewardDefinition("reward.gold", "Золото"),
            new RewardDefinition("reward.silver", "Серебро")
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

        private sealed class ProfileSettlementReceiptForTests
        {
            public ProfileSettlementReceiptForTests(
                string runId,
                long goldAmount,
                System.Collections.Generic.IReadOnlyList<ProfileResourceGrant> grants)
            {
                Value = Create(runId, goldAmount, grants);
            }

            public ProfileSettlementReceipt Value { get; }

            private static ProfileSettlementReceipt Create(
                string runId,
                long goldAmount,
                System.Collections.Generic.IReadOnlyList<ProfileResourceGrant> grants)
            {
                var repository = new ReceiptRepository();
                var session = new PlayerProfileSession(
                    repository,
                    new PlayerProfileSeed(
                        new[] { new DungeonTeam.Gameplay.PlayerProfile.Domain.HeroProfileState("leader", 1, "loadout") },
                        "leader",
                        System.Array.Empty<string>()));
                return session.BankTerminalResult(new ProfileTerminalResultRequest(runId, goldAmount, grants)).Receipt;
            }

            private sealed class ReceiptRepository : IPlayerProfileRepository
            {
                public bool TryLoad(out DungeonTeam.Gameplay.PlayerProfile.Domain.PlayerProfileState state)
                {
                    state = null;
                    return false;
                }

                public void Save(DungeonTeam.Gameplay.PlayerProfile.Domain.PlayerProfileState state) { }
            }
        }
    }
}
