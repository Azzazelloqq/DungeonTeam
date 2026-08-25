using System;
using System.Collections.Generic;
using DungeonTeam.Gameplay.GuildHall.Application;
using DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.UI.RewardCollection;
using NUnit.Framework;

namespace DungeonTeam.Gameplay.GuildHall.Tests.EditMode
{
    public sealed class RewardCollectionViewModelTests
    {
        [Test]
        public void Receive_NotInvokedUntilExplicitCommand()
        {
            var requests = new List<RewardClaimRequest>();
            var viewModel = Create(request => { requests.Add(request); return true; });
            viewModel.Initialize();

            Assert.That(requests, Is.Empty);
            Assert.That(viewModel.Entries, Has.Count.EqualTo(1));

            viewModel.ReceiveCommand.Execute(RewardClaimIdentity.Quest("quest.rewarded"));

            Assert.That(requests, Has.Count.EqualTo(1));
            Assert.That(viewModel.Entries, Is.Empty);
        }

        [Test]
        public void Receive_FailedCallback_KeepsEntryAvailable()
        {
            var viewModel = Create(_ => false);
            viewModel.Initialize();

            viewModel.ReceiveCommand.Execute(RewardClaimIdentity.Quest("quest.rewarded"));

            Assert.That(viewModel.Entries, Has.Count.EqualTo(1));
        }

        [Test]
        public void Collection_PreservesQuestAndContractIdentitiesAsDistinctSources()
        {
            var requests = new List<RewardClaimRequest>();
            var point = new RewardClaimPointSnapshot(RewardClaimPointKind.Reception);
            var snapshot = new RewardCollectionSnapshot(
                point,
                new[]
                {
                    Entry(RewardClaimIdentity.Quest("quest.rewarded"), point),
                    Entry(RewardClaimIdentity.Contract("contract.rewarded"), point)
                },
                Text("Rewards"),
                Text("Close"));
            var viewModel = new RewardCollectionViewModel(
                new RewardCollectionModel(snapshot),
                request =>
                {
                    requests.Add(request);
                    return true;
                },
                () => { });

            viewModel.Initialize();
            viewModel.ReceiveCommand.Execute(RewardClaimIdentity.Quest("quest.rewarded"));
            viewModel.ReceiveCommand.Execute(RewardClaimIdentity.Contract("contract.rewarded"));

            Assert.That(requests, Has.Count.EqualTo(2));
            Assert.That(requests[0].Identity.Kind, Is.EqualTo(RewardClaimIdentityKind.Quest));
            Assert.That(requests[1].Identity.Kind, Is.EqualTo(RewardClaimIdentityKind.Contract));
            Assert.That(viewModel.Entries, Is.Empty);
        }

        private static RewardCollectionViewModel Create(Func<RewardClaimRequest, bool> claim)
        {
            var point = new RewardClaimPointSnapshot(RewardClaimPointKind.Reception);
            var entry = Entry(RewardClaimIdentity.Quest("quest.rewarded"), point);
            var snapshot = new RewardCollectionSnapshot(
                point,
                new[] { entry },
                Text("Rewards"),
                Text("Close"));
            return new RewardCollectionViewModel(
                new RewardCollectionModel(snapshot),
                claim,
                () => { });
        }

        private static RewardCollectionEntrySnapshot Entry(
            RewardClaimIdentity identity,
            RewardClaimPointSnapshot point) => new(
            identity,
            Text(identity.SourceId + ".title"),
            new[] { Text("Gold: 4") },
            Text(identity.SourceId + ".reception"),
            Text("Receive"),
            point);

        private static GuildTextSnapshot Text(string value) => new(value, value);
    }
}
