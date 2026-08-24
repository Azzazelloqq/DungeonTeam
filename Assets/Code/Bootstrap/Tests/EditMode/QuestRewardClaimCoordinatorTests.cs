using System;
using System.Collections.Generic;
using DungeonTeam.Gameplay.PlayerProfile.Application;
using DungeonTeam.Gameplay.PlayerProfile.Domain;
using DungeonTeam.Gameplay.Quests.Application;
using DungeonTeam.Gameplay.Quests.Domain;
using NUnit.Framework;

namespace Code.ApplicationRoot.Tests.EditMode
{
    public sealed class QuestRewardClaimCoordinatorTests
    {
        [Test]
        public void Claim_WrongPoint_IsRejectedWithoutProfileOrQuestMutation()
        {
            var fixture = CreateFixture();

            var result = fixture.Coordinator.Claim(
                "quest.rewarded",
                QuestRewardClaimPoint.Npc("npc.other"));

            Assert.That(result.Status, Is.EqualTo(QuestRewardClaimStatus.Rejected));
            Assert.That(fixture.Profile.State.Gold, Is.EqualTo(0));
            Assert.That(fixture.Quests.State.IsRewardClaimed("quest.rewarded"), Is.False);
        }

        [Test]
        public void Claim_AppliesProfileFirstAndFinalizedDuplicateIsRejected()
        {
            var fixture = CreateFixture();

            var first = fixture.Coordinator.Claim("quest.rewarded", QuestRewardClaimPoint.Reception);
            var second = fixture.Coordinator.Claim("quest.rewarded", QuestRewardClaimPoint.Reception);

            Assert.That(first.Status, Is.EqualTo(QuestRewardClaimStatus.Applied));
            Assert.That(second.Status, Is.EqualTo(QuestRewardClaimStatus.Rejected));
            Assert.That(fixture.Profile.State.Gold, Is.EqualTo(4));
            Assert.That(fixture.Profile.State.Inventory.Resources[0].Quantity, Is.EqualTo(2));
            Assert.That(fixture.Quests.State.ClaimedRewardQuestIds, Is.EqualTo(new[] { "quest.rewarded" }));
        }

        [Test]
        public void Claim_ProfileSaveFailure_LeavesQuestPending()
        {
            var fixture = CreateFixture();
            fixture.ProfileRepository.ThrowOnSave = true;

            Assert.Throws<InvalidOperationException>(() => fixture.Coordinator.Claim(
                "quest.rewarded", QuestRewardClaimPoint.Reception));
            Assert.That(fixture.Quests.State.IsRewardClaimed("quest.rewarded"), Is.False);
        }

        [Test]
        public void Claim_QuestSaveFailure_RetryUsesAlreadyAppliedAndMarksQuest()
        {
            var fixture = CreateFixture();
            fixture.QuestRepository.ThrowOnSave = true;

            Assert.Throws<InvalidOperationException>(() => fixture.Coordinator.Claim(
                "quest.rewarded", QuestRewardClaimPoint.Reception));
            Assert.That(fixture.Profile.State.Gold, Is.EqualTo(4));
            Assert.That(fixture.Quests.State.IsRewardClaimed("quest.rewarded"), Is.False);

            fixture.QuestRepository.ThrowOnSave = false;
            var retry = fixture.Coordinator.Claim("quest.rewarded", QuestRewardClaimPoint.Reception);

            Assert.That(retry.Status, Is.EqualTo(QuestRewardClaimStatus.AlreadyApplied));
            Assert.That(fixture.Profile.State.Gold, Is.EqualTo(4));
            Assert.That(fixture.Quests.State.IsRewardClaimed("quest.rewarded"), Is.True);
        }

        private static Fixture CreateFixture()
        {
            var catalog = new QuestCatalog(new[]
            {
                new QuestDefinition(
                    "quest.rewarded",
                    Text("title"),
                    Text("summary"),
                    Text("objective"),
                    new QuestObjective(QuestObjectiveKind.CompleteDialogue, "npc.debater"),
                    null,
                    new QuestRewardDefinition(
                        4,
                        new[] { new QuestRewardResource("resource.crystal", 2) },
                        QuestRewardClaimPoint.Reception,
                        Text("claim.hint")))
            });
            var questRepository = new QuestRepository
            {
                LoadedState = new QuestState(null, new[] { "quest.rewarded" })
            };
            var quests = new QuestSession(questRepository);
            var profileRepository = new ProfileRepository();
            var profile = new PlayerProfileSession(
                profileRepository,
                new PlayerProfileSeed(
                    new[] { new HeroProfileState("leader", 1, "loadout") },
                    "leader",
                    Array.Empty<string>()));
            return new Fixture(
                new QuestRewardClaimCoordinator(quests, catalog, profile),
                quests,
                profile,
                questRepository,
                profileRepository);
        }

        private static QuestText Text(string id) => new(id, id);

        private sealed class Fixture
        {
            public Fixture(
                QuestRewardClaimCoordinator coordinator,
                QuestSession quests,
                PlayerProfileSession profile,
                QuestRepository questRepository,
                ProfileRepository profileRepository)
            {
                Coordinator = coordinator;
                Quests = quests;
                Profile = profile;
                QuestRepository = questRepository;
                ProfileRepository = profileRepository;
            }

            public QuestRewardClaimCoordinator Coordinator { get; }
            public QuestSession Quests { get; }
            public PlayerProfileSession Profile { get; }
            public QuestRepository QuestRepository { get; }
            public ProfileRepository ProfileRepository { get; }
        }

        private sealed class QuestRepository : IQuestRepository
        {
            public QuestState LoadedState { get; set; }
            public bool ThrowOnSave { get; set; }
            public bool TryLoad(out QuestState state)
            {
                state = LoadedState;
                LoadedState = null;
                return state != null;
            }

            public void Save(QuestState state)
            {
                if (ThrowOnSave) throw new InvalidOperationException("Quest save failed.");
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
