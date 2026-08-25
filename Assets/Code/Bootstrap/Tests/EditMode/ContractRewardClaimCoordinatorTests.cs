using System;
using DungeonTeam.Gameplay.Contracts.Application;
using DungeonTeam.Gameplay.Contracts.Domain;
using DungeonTeam.Gameplay.PlayerProfile.Application;
using DungeonTeam.Gameplay.PlayerProfile.Domain;
using NUnit.Framework;

namespace Code.ApplicationRoot.Tests.EditMode
{
    public sealed class ContractRewardClaimCoordinatorTests
    {
        [Test]
        public void Claim_WrongPoint_IsRejectedWithoutMutatingEitherOwner()
        {
            var fixture = CreateFixture();

            var result = fixture.Coordinator.Claim(
                "contract.rewarded",
                ContractRewardClaimPoint.Npc("npc.other"));

            Assert.That(result.Status, Is.EqualTo(ContractRewardClaimStatus.Rejected));
            Assert.That(fixture.Profile.State.Gold, Is.Zero);
            Assert.That(fixture.Contracts.State.IsRewardClaimed("contract.rewarded"), Is.False);
        }

        [Test]
        public void Claim_AppliesProfileFirstAndSecondClaimIsRejected()
        {
            var fixture = CreateFixture();

            var first = fixture.Coordinator.Claim(
                "contract.rewarded",
                ContractRewardClaimPoint.Reception);
            var second = fixture.Coordinator.Claim(
                "contract.rewarded",
                ContractRewardClaimPoint.Reception);

            Assert.That(first.Status, Is.EqualTo(ContractRewardClaimStatus.Applied));
            Assert.That(second.Status, Is.EqualTo(ContractRewardClaimStatus.Rejected));
            Assert.That(fixture.Profile.State.Gold, Is.EqualTo(4));
            Assert.That(fixture.Profile.State.Inventory.Resources[0].Quantity, Is.EqualTo(2));
            Assert.That(
                fixture.Contracts.State.ClaimedRewardContractIds,
                Is.EqualTo(new[] { "contract.rewarded" }));
        }

        [Test]
        public void Claim_ProfileSaveFailure_LeavesContractUnclaimed()
        {
            var fixture = CreateFixture();
            fixture.ProfileRepository.ThrowOnSave = true;

            Assert.Throws<InvalidOperationException>(() => fixture.Coordinator.Claim(
                "contract.rewarded",
                ContractRewardClaimPoint.Reception));

            Assert.That(fixture.Contracts.State.IsRewardClaimed("contract.rewarded"), Is.False);
        }

        [Test]
        public void Claim_ContractSaveFailure_RetryUsesAlreadyAppliedWithoutDuplicatePayout()
        {
            var fixture = CreateFixture();
            fixture.ContractRepository.ThrowOnSave = true;

            Assert.Throws<InvalidOperationException>(() => fixture.Coordinator.Claim(
                "contract.rewarded",
                ContractRewardClaimPoint.Reception));
            Assert.That(fixture.Profile.State.Gold, Is.EqualTo(4));
            Assert.That(fixture.Contracts.State.IsRewardClaimed("contract.rewarded"), Is.False);

            fixture.ContractRepository.ThrowOnSave = false;
            var retry = fixture.Coordinator.Claim(
                "contract.rewarded",
                ContractRewardClaimPoint.Reception);

            Assert.That(retry.Status, Is.EqualTo(ContractRewardClaimStatus.AlreadyApplied));
            Assert.That(fixture.Profile.State.Gold, Is.EqualTo(4));
            Assert.That(fixture.Profile.State.Inventory.Resources[0].Quantity, Is.EqualTo(2));
            Assert.That(fixture.Contracts.State.IsRewardClaimed("contract.rewarded"), Is.True);
        }

        private static Fixture CreateFixture()
        {
            var catalog = new ContractCatalog(new[]
            {
                new ContractDefinition(
                    "contract.rewarded",
                    Text("title"),
                    Text("summary"),
                    "location.dungeon",
                    true,
                    null,
                    null,
                    new ContractRewardDefinition(
                        4,
                        new[] { new ContractRewardResource("resource.crystal", 2) },
                        ContractRewardClaimPoint.Reception,
                        Text("claim.hint")))
            });
            var contractRepository = new ContractRepository
            {
                LoadedState = new ContractState(null, new[] { "contract.rewarded" })
            };
            var contracts = new ContractSession(contractRepository);
            var profileRepository = new ProfileRepository();
            var profile = new PlayerProfileSession(
                profileRepository,
                new PlayerProfileSeed(
                    new[] { new HeroProfileState("leader", 1, "loadout") },
                    "leader",
                    Array.Empty<string>()));
            return new Fixture(
                new ContractRewardClaimCoordinator(contracts, catalog, profile),
                contracts,
                profile,
                contractRepository,
                profileRepository);
        }

        private static ContractTextSnapshot Text(string id) => new(id, id);

        private sealed class Fixture
        {
            public Fixture(
                ContractRewardClaimCoordinator coordinator,
                ContractSession contracts,
                PlayerProfileSession profile,
                ContractRepository contractRepository,
                ProfileRepository profileRepository)
            {
                Coordinator = coordinator;
                Contracts = contracts;
                Profile = profile;
                ContractRepository = contractRepository;
                ProfileRepository = profileRepository;
            }

            public ContractRewardClaimCoordinator Coordinator { get; }
            public ContractSession Contracts { get; }
            public PlayerProfileSession Profile { get; }
            public ContractRepository ContractRepository { get; }
            public ProfileRepository ProfileRepository { get; }
        }

        private sealed class ContractRepository : IContractRepository
        {
            public ContractState LoadedState { get; set; }
            public bool ThrowOnSave { get; set; }

            public bool TryLoad(out ContractState state)
            {
                state = LoadedState;
                LoadedState = null;
                return state != null;
            }

            public void Save(ContractState state)
            {
                if (ThrowOnSave) throw new InvalidOperationException("Contract save failed.");
            }
        }

        private sealed class ProfileRepository : IPlayerProfileRepository
        {
            public bool ThrowOnSave { get; set; }

            public bool TryLoad(out PlayerProfileState state)
            {
                state = null;
                return false;
            }

            public void Save(PlayerProfileState state)
            {
                if (ThrowOnSave) throw new InvalidOperationException("Profile save failed.");
            }
        }
    }
}
