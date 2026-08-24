using DungeonTeam.Gameplay.Contracts.Domain;
using DungeonTeam.Gameplay.GuildHall.Application;
using DungeonTeam.Gameplay.PlayerProfile.Application;

namespace Code.ApplicationRoot
{
    internal static class ContractSnapshotBuilder
    {
        public static NoticeBoardOfferSnapshot[] Build(
            DungeonTeam.Gameplay.Contracts.Domain.ContractCatalog contracts,
            ContractState state,
            GuildRankCatalog ranks,
            string currentRankId,
            GuildTextSnapshot requiredRankFormat,
            NoticeBoardTextSnapshot boardText)
        {
            return GuildOfferAvailabilityBuilder.Build(
                contracts,
                state,
                ranks,
                currentRankId,
                requiredRankFormat,
                boardText);
        }

        public static bool IsAvailableForAcceptance(
            string contractId,
            DungeonTeam.Gameplay.Contracts.Domain.ContractCatalog contracts,
            ContractState state,
            GuildRankCatalog ranks,
            string currentRankId,
            GuildTextSnapshot requiredRankFormat,
            NoticeBoardTextSnapshot boardText)
        {
            if (string.IsNullOrWhiteSpace(contractId))
            {
                return false;
            }

            var offers = Build(
                contracts,
                state,
                ranks,
                currentRankId,
                requiredRankFormat,
                boardText);
            for (var index = 0; index < offers.Length; index++)
            {
                if (offers[index].ContractId == contractId)
                {
                    return offers[index].IsAvailable;
                }
            }

            return false;
        }
    }
}
