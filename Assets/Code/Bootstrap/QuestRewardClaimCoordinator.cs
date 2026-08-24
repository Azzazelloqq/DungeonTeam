using System;
using System.Collections.Generic;
using DungeonTeam.Gameplay.PlayerProfile.Application;
using DungeonTeam.Gameplay.Quests.Application;
using DungeonTeam.Gameplay.Quests.Domain;

namespace Code.ApplicationRoot
{
    internal enum QuestRewardClaimStatus
    {
        Rejected = 0,
        Applied = 1,
        AlreadyApplied = 2
    }

    internal sealed class QuestRewardClaimResult
    {
        private QuestRewardClaimResult(QuestRewardClaimStatus status)
        {
            Status = status;
        }

        public QuestRewardClaimStatus Status { get; }
        public bool Accepted => Status != QuestRewardClaimStatus.Rejected;
        public static QuestRewardClaimResult Rejected() => new(QuestRewardClaimStatus.Rejected);
        public static QuestRewardClaimResult Applied() => new(QuestRewardClaimStatus.Applied);
        public static QuestRewardClaimResult AlreadyApplied() => new(QuestRewardClaimStatus.AlreadyApplied);
    }

    internal sealed class QuestRewardClaimCoordinator
    {
        private readonly QuestSession _quests;
        private readonly QuestCatalog _catalog;
        private readonly PlayerProfileSession _profile;

        public QuestRewardClaimCoordinator(
            QuestSession quests,
            QuestCatalog catalog,
            PlayerProfileSession profile)
        {
            _quests = quests ?? throw new ArgumentNullException(nameof(quests));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _profile = profile ?? throw new ArgumentNullException(nameof(profile));
        }

        public QuestRewardClaimResult Claim(string questId, QuestRewardClaimPoint point)
        {
            if (string.IsNullOrWhiteSpace(questId) || point == null || !_catalog.Contains(questId))
                return QuestRewardClaimResult.Rejected();
            var definition = _catalog.Require(questId);
            if (definition.Reward == null || !_quests.State.IsCompleted(questId) ||
                _quests.State.IsRewardClaimed(questId) || !definition.Reward.ClaimPoint.Matches(point))
                return QuestRewardClaimResult.Rejected();

            var grants = new ProfileResourceGrant[definition.Reward.Resources.Count];
            for (var index = 0; index < grants.Length; index++)
            {
                var grant = definition.Reward.Resources[index];
                grants[index] = new ProfileResourceGrant(grant.DefinitionId, grant.Amount);
            }
            var profileResult = _profile.ClaimReward(new ProfileRewardClaimRequest(
                $"quest.reward:{definition.QuestId}",
                definition.Reward.GoldAmount,
                grants));
            _quests.MarkRewardClaimed(definition.QuestId, _catalog);
            return profileResult.Status == ProfileRewardClaimStatus.AlreadyApplied
                ? QuestRewardClaimResult.AlreadyApplied()
                : QuestRewardClaimResult.Applied();
        }

        public IReadOnlyList<string> GetClaimableAt(QuestRewardClaimPoint point)
        {
            if (point == null) throw new ArgumentNullException(nameof(point));
            return _quests.State.GetClaimableAt(point, _catalog);
        }
    }
}
