using System;
using System.Collections.Generic;
using DungeonTeam.Gameplay.DungeonRun.Runtime;
using DungeonTeam.Gameplay.GuildHall.Application;
using DungeonTeam.Gameplay.Inventory.Application;
using DungeonTeam.Gameplay.PlayerProfile.Application;
using DungeonTeam.Gameplay.Rewards.Runtime;

namespace Code.ApplicationRoot
{
    internal sealed class GuildRunSummaryBuilder
    {
        public GuildRunSummarySnapshot Build(
            DungeonRunResult result,
            ProfileSettlementReceipt receipt,
            RewardCatalog rewards,
            GuildRunSummaryTextSnapshot text)
        {
            if (receipt == null)
            {
                throw new ArgumentNullException(nameof(receipt));
            }

            if (!string.Equals(result.RunId, receipt.RunId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "The settlement receipt belongs to a different dungeon run.");
            }

            if (rewards == null)
            {
                throw new ArgumentNullException(nameof(rewards));
            }

            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            var rewardLines = new List<GuildTextSnapshot>();
            if (receipt.GoldAmount > 0)
            {
                AddRewardLine(
                    rewardLines,
                    "reward.gold",
                    receipt.GoldAmount,
                    rewards,
                    text);
            }

            for (var index = 0; index < receipt.ResourceGrants.Count; index++)
            {
                var grant = receipt.ResourceGrants[index];
                var rewardId = grant.DefinitionId switch
                {
                    ItemCatalog.MonsterCrystalDefinitionId => "reward.crystal",
                    _ => throw new InvalidOperationException(
                        $"Resource '{grant.DefinitionId}' has no configured terminal summary display.")
                };
                AddRewardLine(rewardLines, rewardId, grant.Amount, rewards, text);
            }

            return new GuildRunSummarySnapshot(
                GetOutcomeText(result.Outcome, text),
                new GuildTextSnapshot(result.DungeonId, result.DungeonId),
                rewardLines,
                text);
        }

        private static void AddRewardLine(
            ICollection<GuildTextSnapshot> target,
            string rewardId,
            long amount,
            RewardCatalog rewards,
            GuildRunSummaryTextSnapshot text)
        {
            var definition = rewards.Require(rewardId);
            target.Add(new GuildTextSnapshot(
                rewardId,
                string.Format(text.RewardLineFormat, definition.DisplayName, amount)));
        }

        private static GuildTextSnapshot GetOutcomeText(
            DungeonRunOutcome outcome,
            GuildRunSummaryTextSnapshot text)
        {
            return outcome switch
            {
                DungeonRunOutcome.Completed => text.CompletedOutcome,
                DungeonRunOutcome.Defeated => text.DefeatedOutcome,
                _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, null)
            };
        }
    }

    internal sealed class RewardSettlementMapper
    {
        public ProfileTerminalResultRequest Map(
            DungeonRunResult result,
            RewardCatalog rewards)
        {
            if (rewards == null)
            {
                throw new ArgumentNullException(nameof(rewards));
            }

            long gold = 0;
            var crystalAmount = 0;
            for (var index = 0; index < result.CollectedRewards.Count; index++)
            {
                var grant = result.CollectedRewards[index];
                rewards.Require(grant.RewardId);
                switch (grant.RewardId)
                {
                    case "reward.gold":
                    case "reward.silver":
                        gold = checked(gold + grant.Amount);
                        break;
                    case "reward.crystal":
                        crystalAmount = checked(crystalAmount + grant.Amount);
                        break;
                    default:
                        throw new InvalidOperationException(
                            $"Reward '{grant.RewardId}' is not supported by terminal banking.");
                }
            }

            var resourceGrants = crystalAmount > 0
                ? new[] { new ProfileResourceGrant(ItemCatalog.MonsterCrystalDefinitionId, crystalAmount) }
                : Array.Empty<ProfileResourceGrant>();
            return new ProfileTerminalResultRequest(result.RunId, gold, resourceGrants);
        }
    }
}
