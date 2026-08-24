using System;
using DungeonTeam.Gameplay.GuildHall.Application;
using DungeonTeam.Gameplay.PlayerProfile.Application;

namespace Code.ApplicationRoot
{
    internal static class GuildOfferAvailabilityBuilder
    {
        public static NoticeBoardOfferSnapshot[] Build(
            ContractCatalog contracts,
            GuildRankCatalog ranks,
            string currentRankId,
            GuildTextSnapshot requiredRankFormat)
        {
            if (contracts == null)
            {
                throw new ArgumentNullException(nameof(contracts));
            }

            if (ranks == null)
            {
                throw new ArgumentNullException(nameof(ranks));
            }

            var currentRank = ranks.Require(currentRankId);
            var offers = new NoticeBoardOfferSnapshot[contracts.Offers.Count];
            for (var index = 0; index < offers.Length; index++)
            {
                var source = contracts.Offers[index];
                var minimumRank = string.IsNullOrWhiteSpace(source.MinimumRankId)
                    ? null
                    : ranks.Require(source.MinimumRankId);

                if (!source.IsAvailable || minimumRank == null ||
                    ranks.Compare(currentRank.RankId, minimumRank.RankId) >= 0)
                {
                    offers[index] = Copy(source, source.IsAvailable, source.DisabledReason);
                    continue;
                }

                if (requiredRankFormat == null)
                {
                    throw new InvalidOperationException(
                        "A rank-gated offer requires configured required-rank text.");
                }

                string displayText;
                try
                {
                    displayText = string.Format(
                        requiredRankFormat.DisplayText,
                        minimumRank.DisplayName);
                }
                catch (FormatException exception)
                {
                    throw new InvalidOperationException(
                        "Configured required-rank offer text has an invalid format.",
                        exception);
                }

                offers[index] = Copy(
                    source,
                    false,
                    new GuildTextSnapshot(requiredRankFormat.TextId, displayText));
            }

            return offers;
        }

        private static NoticeBoardOfferSnapshot Copy(
            NoticeBoardOfferSnapshot source,
            bool isAvailable,
            GuildTextSnapshot disabledReason) =>
            new(
                source.ContractId,
                source.Title,
                source.Summary,
                source.LocationId,
                isAvailable,
                disabledReason,
                source.MinimumRankId);
    }
}
