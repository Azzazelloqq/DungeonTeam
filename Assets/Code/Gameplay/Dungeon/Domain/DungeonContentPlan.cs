using System;
using System.Collections.Generic;

namespace DungeonTeam.Gameplay.Dungeon.Domain
{
    public sealed class DungeonContentPlan
    {
        public DungeonContentPlan(
            EnemySpawnPlan[] enemySpawns,
            InterestPointSpawnPlan[] interestPointSpawns,
            ObjectiveSpawnPlan[] objectiveSpawns,
            float rewardBudgetMultiplier)
            : this(
                enemySpawns,
                interestPointSpawns,
                objectiveSpawns,
                Array.Empty<DungeonRewardGrantPlan>(),
                rewardBudgetMultiplier)
        {
        }

        public DungeonContentPlan(
            EnemySpawnPlan[] enemySpawns,
            InterestPointSpawnPlan[] interestPointSpawns,
            ObjectiveSpawnPlan[] objectiveSpawns,
            DungeonRewardGrantPlan[] completionRewards,
            float rewardBudgetMultiplier)
        {
            if (rewardBudgetMultiplier < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(rewardBudgetMultiplier),
                    "Reward budget multiplier cannot be negative.");
            }

            EnemySpawns = Copy(enemySpawns, nameof(enemySpawns));
            InterestPointSpawns = Copy(interestPointSpawns, nameof(interestPointSpawns));
            ObjectiveSpawns = Copy(objectiveSpawns, nameof(objectiveSpawns));
            CompletionRewards = Copy(completionRewards, nameof(completionRewards));
            RewardBudgetMultiplier = rewardBudgetMultiplier;
        }

        public IReadOnlyList<EnemySpawnPlan> EnemySpawns { get; }
        public IReadOnlyList<InterestPointSpawnPlan> InterestPointSpawns { get; }
        public IReadOnlyList<ObjectiveSpawnPlan> ObjectiveSpawns { get; }
        public IReadOnlyList<DungeonRewardGrantPlan> CompletionRewards { get; }
        public float RewardBudgetMultiplier { get; }

        private static IReadOnlyList<T> Copy<T>(T[] source, string parameterName)
        {
            if (source == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            return Array.AsReadOnly((T[])source.Clone());
        }
    }

    public readonly struct EnemySpawnPlan
    {
        public EnemySpawnPlan(
            string placementId,
            string enemyId,
            string behaviorId,
            string loadoutId,
            int actorLevel,
            string encounterGroupId,
            DungeonPose pose,
            DungeonRewardGrantPlan[] rewards)
        {
            PlacementId = RequireId(placementId, nameof(placementId));
            EnemyId = RequireId(enemyId, nameof(enemyId));
            BehaviorId = RequireId(behaviorId, nameof(behaviorId));
            LoadoutId = RequireId(loadoutId, nameof(loadoutId));
            ActorLevel = actorLevel > 0
                ? actorLevel
                : throw new ArgumentOutOfRangeException(nameof(actorLevel));
            EncounterGroupId = encounterGroupId;
            Pose = pose;
            Rewards = rewards != null
                ? Array.AsReadOnly((DungeonRewardGrantPlan[])rewards.Clone())
                : throw new ArgumentNullException(nameof(rewards));
        }

        public string PlacementId { get; }
        public string EnemyId { get; }
        public string BehaviorId { get; }
        public string LoadoutId { get; }
        public int ActorLevel { get; }
        public string EncounterGroupId { get; }
        public DungeonPose Pose { get; }
        public IReadOnlyList<DungeonRewardGrantPlan> Rewards { get; }

        private static string RequireId(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("ID cannot be empty.", parameterName);
            }

            return value;
        }
    }

    public readonly struct InterestPointSpawnPlan
    {
        public InterestPointSpawnPlan(
            string placementId,
            string interestPointId,
            string rewardProfileId,
            DungeonPose pose)
            : this(
                placementId,
                interestPointId,
                rewardProfileId,
                pose,
                Array.Empty<DungeonRewardGrantPlan>())
        {
        }

        public InterestPointSpawnPlan(
            string placementId,
            string interestPointId,
            string rewardProfileId,
            DungeonPose pose,
            DungeonRewardGrantPlan[] rewards)
        {
            PlacementId = placementId;
            InterestPointId = interestPointId;
            RewardProfileId = rewardProfileId;
            Pose = pose;
            Rewards = rewards != null
                ? Array.AsReadOnly((DungeonRewardGrantPlan[])rewards.Clone())
                : throw new ArgumentNullException(nameof(rewards));
        }

        public string PlacementId { get; }
        public string InterestPointId { get; }
        public string RewardProfileId { get; }
        public DungeonPose Pose { get; }
        public IReadOnlyList<DungeonRewardGrantPlan> Rewards { get; }
    }

    public readonly struct DungeonRewardGrantPlan
    {
        public DungeonRewardGrantPlan(string rewardId, int amount)
        {
            if (string.IsNullOrWhiteSpace(rewardId))
            {
                throw new ArgumentException("Reward ID cannot be empty.", nameof(rewardId));
            }

            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            RewardId = rewardId;
            Amount = amount;
        }

        public string RewardId { get; }

        public int Amount { get; }
    }

    public readonly struct ObjectiveSpawnPlan
    {
        public ObjectiveSpawnPlan(
            string placementId,
            string objectiveId,
            DungeonPose pose)
        {
            PlacementId = placementId;
            ObjectiveId = objectiveId;
            Pose = pose;
        }

        public string PlacementId { get; }
        public string ObjectiveId { get; }
        public DungeonPose Pose { get; }
    }
}
