using System;
using DungeonTeam.Gameplay.GuildHall.Application;
using DungeonTeam.Gameplay.PlayerProfile.Application;
using NUnit.Framework;

namespace Code.ApplicationRoot.Tests.EditMode
{
    public sealed class GuildRankOfferTests
    {
        [Test]
        public void OfferAvailability_PreservesOffersAndBlocksOnlyBelowMinimumRank()
        {
            var offers = new[]
            {
                Offer("contract.demo", null, true, null),
                Offer("contract.veteran", "rank.e", true, null),
                Offer("contract.authored-disabled", null, false, Text("disabled"))
            };
            var catalog = new ContractCatalog(offers);
            var ranks = new GuildRankCatalog(new[]
            {
                new GuildRankDefinition("rank.f", "F", 0),
                new GuildRankDefinition("rank.e", "E", 10)
            });

            var blocked = GuildOfferAvailabilityBuilder.Build(
                catalog,
                ranks,
                "rank.f",
                Text("Required rank: {0}"));
            var available = GuildOfferAvailabilityBuilder.Build(
                catalog,
                ranks,
                "rank.e",
                Text("Required rank: {0}"));

            Assert.That(blocked, Has.Length.EqualTo(offers.Length));
            Assert.That(blocked[0].IsAvailable, Is.True);
            Assert.That(blocked[1].IsAvailable, Is.False);
            Assert.That(blocked[1].DisabledReason.DisplayText, Is.EqualTo("Required rank: E"));
            Assert.That(blocked[2].IsAvailable, Is.False);
            Assert.That(blocked[2].DisabledReason.DisplayText, Is.EqualTo("disabled"));
            Assert.That(available[1].IsAvailable, Is.True);
            Assert.That(available[1].DisabledReason, Is.Null);
        }

        private static NoticeBoardOfferSnapshot Offer(
            string contractId,
            string minimumRankId,
            bool isAvailable,
            GuildTextSnapshot disabledReason) =>
            new(
                contractId,
                Text(contractId + ".title"),
                Text(contractId + ".summary"),
                "location.dungeon",
                isAvailable,
                disabledReason,
                minimumRankId);

        private static GuildTextSnapshot Text(string value) => new(value, value);
    }
}
