using System;
using DungeonTeam.Gameplay.Contracts.Domain;
using DungeonTeam.Gameplay.GuildHall.Application;
using DungeonTeam.Gameplay.PlayerProfile.Application;
using ContractDefinitionCatalog = DungeonTeam.Gameplay.Contracts.Domain.ContractCatalog;

namespace Code.ApplicationRoot
{
    internal static class GuildOfferAvailabilityBuilder
    {
        public static NoticeBoardOfferSnapshot[] Build(
            ContractDefinitionCatalog contracts,
            ContractState state,
            GuildRankCatalog ranks,
            string currentRankId,
            GuildTextSnapshot requiredRankFormat,
            NoticeBoardTextSnapshot boardText)
        {
            if (contracts == null) throw new ArgumentNullException(nameof(contracts));
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (ranks == null) throw new ArgumentNullException(nameof(ranks));
            if (boardText == null) throw new ArgumentNullException(nameof(boardText));

            var currentRank = ranks.Require(currentRankId);
            var offers = new NoticeBoardOfferSnapshot[contracts.Definitions.Count];
            for (var index = 0; index < offers.Length; index++)
            {
                var source = contracts.Definitions[index];
                if (string.Equals(state.ActiveContractId, source.ContractId, StringComparison.Ordinal))
                {
                    offers[index] = ToBoardSnapshot(
                        source, false, null,
                        NoticeBoardOfferSnapshot.OfferStatus.Active,
                        boardText.Selected);
                    continue;
                }

                if (state.IsCompleted(source.ContractId))
                {
                    offers[index] = ToBoardSnapshot(
                        source, false, null,
                        NoticeBoardOfferSnapshot.OfferStatus.Completed,
                        boardText.Completed);
                    continue;
                }

                var minimumRank = string.IsNullOrWhiteSpace(source.MinimumRankId)
                    ? null
                    : ranks.Require(source.MinimumRankId);
                if (!source.IsAuthoredAvailable)
                {
                    offers[index] = ToBoardSnapshot(
                        source, false, source.AuthoredDisabledReason,
                        NoticeBoardOfferSnapshot.OfferStatus.Disabled, null);
                    continue;
                }

                if (minimumRank == null || ranks.Compare(currentRank.RankId, minimumRank.RankId) >= 0)
                {
                    offers[index] = ToBoardSnapshot(
                        source, true, null,
                        NoticeBoardOfferSnapshot.OfferStatus.Available, null);
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
                    displayText = string.Format(requiredRankFormat.DisplayText, minimumRank.DisplayName);
                }
                catch (FormatException exception)
                {
                    throw new InvalidOperationException(
                        "Configured required-rank offer text has an invalid format.", exception);
                }

                offers[index] = ToBoardSnapshot(
                    source, false,
                    new ContractTextSnapshot(requiredRankFormat.TextId, displayText),
                    NoticeBoardOfferSnapshot.OfferStatus.Disabled, null);
            }

            return offers;
        }

        private static NoticeBoardOfferSnapshot ToBoardSnapshot(
            ContractDefinition source,
            bool isAvailable,
            ContractTextSnapshot disabledReason,
            NoticeBoardOfferSnapshot.OfferStatus status,
            GuildTextSnapshot statusText)
        {
            return new NoticeBoardOfferSnapshot(
                source.ContractId,
                new GuildTextSnapshot(source.Title.TextId, source.Title.DisplayText),
                new GuildTextSnapshot(source.Summary.TextId, source.Summary.DisplayText),
                source.LocationId,
                isAvailable,
                disabledReason == null
                    ? null
                    : new GuildTextSnapshot(disabledReason.TextId, disabledReason.DisplayText),
                source.MinimumRankId,
                status,
                statusText);
        }
    }
}
