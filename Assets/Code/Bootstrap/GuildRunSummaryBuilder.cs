using System;
using DungeonTeam.Gameplay.DungeonRun.Runtime;
using DungeonTeam.Gameplay.GuildHall.Application;
using DungeonTeam.Gameplay.Rewards.Runtime;

namespace Code.ApplicationRoot
{
    internal sealed class GuildRunSummaryBuilder
    {
        public GuildRunSummarySnapshot Build(
            DungeonRunResult result,
            RewardCatalog rewards,
            GuildRunSummaryTextSnapshot text)
        {
            if (rewards == null)
            {
                throw new ArgumentNullException(nameof(rewards));
            }

            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            var rewardLines = new GuildTextSnapshot[result.CollectedRewards.Count];
            for (var index = 0; index < rewardLines.Length; index++)
            {
                var grant = result.CollectedRewards[index];
                var definition = rewards.Require(grant.RewardId);
                rewardLines[index] = new GuildTextSnapshot(
                    grant.RewardId,
                    string.Format(text.RewardLineFormat, definition.DisplayName, grant.Amount));
            }

            return new GuildRunSummarySnapshot(
                GetOutcomeText(result.Outcome, text),
                new GuildTextSnapshot(result.DungeonId, result.DungeonId),
                rewardLines,
                text);
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
}
