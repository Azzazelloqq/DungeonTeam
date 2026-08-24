using System;
using DungeonTeam.Gameplay.Contracts.Domain;
using DungeonTeam.Gameplay.GuildHall.Application;
using DungeonTeam.Gameplay.PlayerProfile.Application;
using NUnit.Framework;

namespace Code.ApplicationRoot.Tests.EditMode
{
    public sealed class ContractOfferBridgeTests
    {
        [Test]
        public void OfferBuilder_VariableState_PreservesAuthoredDisabledRankAndCompletionStatuses()
        {
            var catalog = new DungeonTeam.Gameplay.Contracts.Domain.ContractCatalog(new[]
            {
                Definition("contract.demo", true, null),
                Definition("contract.veteran", true, "rank.e"),
                Definition("contract.disabled", false, null, "Temporarily closed")
            });
            var ranks = new GuildRankCatalog(new[]
            {
                new GuildRankDefinition("rank.f", "F", 0),
                new GuildRankDefinition("rank.e", "E", 10)
            });
            var state = new ContractState(null, new[] { "contract.demo" });

            var offers = GuildOfferAvailabilityBuilder.Build(
                catalog,
                state,
                ranks,
                "rank.f",
                new GuildTextSnapshot("rank.required", "Requires {0}"),
                NoticeBoardText());

            Assert.That(offers, Has.Length.EqualTo(3));
            Assert.That(offers[0].Status, Is.EqualTo(NoticeBoardOfferSnapshot.OfferStatus.Completed));
            Assert.That(offers[0].IsAvailable, Is.False);
            Assert.That(offers[1].Status, Is.EqualTo(NoticeBoardOfferSnapshot.OfferStatus.Disabled));
            Assert.That(offers[1].DisabledReason.DisplayText, Is.EqualTo("Requires E"));
            Assert.That(offers[2].Status, Is.EqualTo(NoticeBoardOfferSnapshot.OfferStatus.Disabled));
            Assert.That(offers[2].DisabledReason.DisplayText, Is.EqualTo("Temporarily closed"));
        }

        [Test]
        public void OfferBuilder_ActiveState_PreparesActiveBoardStateWithoutChangingDefinitionAvailability()
        {
            var catalog = new DungeonTeam.Gameplay.Contracts.Domain.ContractCatalog(new[] { Definition("contract.demo", true, null) });
            var state = new ContractState("contract.demo", Array.Empty<string>());
            var ranks = new GuildRankCatalog(new[] { new GuildRankDefinition("rank.f", "F", 0) });

            var offer = GuildOfferAvailabilityBuilder.Build(
                catalog,
                state,
                ranks,
                "rank.f",
                new GuildTextSnapshot("rank.required", "Requires {0}"),
                NoticeBoardText())[0];

            Assert.That(offer.IsActive, Is.True);
            Assert.That(offer.IsAvailable, Is.False);
            Assert.That(offer.StatusText.DisplayText, Is.EqualTo("Выбрано"));
        }

        [Test]
        public void IsAvailableForAcceptance_RankGatedContractBelowRequiredRank_ReturnsFalse()
        {
            var catalog = new DungeonTeam.Gameplay.Contracts.Domain.ContractCatalog(new[]
            {
                Definition("contract.veteran", true, "rank.e")
            });
            var ranks = new GuildRankCatalog(new[]
            {
                new GuildRankDefinition("rank.f", "F", 0),
                new GuildRankDefinition("rank.e", "E", 10)
            });

            var isAvailable = ContractSnapshotBuilder.IsAvailableForAcceptance(
                "contract.veteran",
                catalog,
                new ContractState(),
                ranks,
                "rank.f",
                new GuildTextSnapshot("rank.required", "Requires {0}"),
                NoticeBoardText());

            Assert.That(isAvailable, Is.False);
        }

        private static ContractDefinition Definition(
            string id,
            bool available,
            string minimumRank,
            string disabledReason = null) => new(
            id,
            new ContractTextSnapshot(id + ".title", id),
            new ContractTextSnapshot(id + ".summary", id + " summary"),
            "location.dungeon",
            available,
            available ? null : new ContractTextSnapshot(id + ".disabled", disabledReason),
            minimumRank);

        private static NoticeBoardTextSnapshot NoticeBoardText() => new(
            new GuildTextSnapshot("notice.header", "Контракты"),
            new GuildTextSnapshot("notice.select", "Выбрать"),
            new GuildTextSnapshot("notice.selected", "Выбрано"),
            new GuildTextSnapshot("notice.close", "Закрыть"),
            new GuildTextSnapshot("notice.empty", "Нет контрактов"));
    }
}
