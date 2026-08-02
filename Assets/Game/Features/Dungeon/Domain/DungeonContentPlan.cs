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
            RewardBudgetMultiplier = rewardBudgetMultiplier;
        }

        public IReadOnlyList<EnemySpawnPlan> EnemySpawns { get; }
        public IReadOnlyList<InterestPointSpawnPlan> InterestPointSpawns { get; }
        public IReadOnlyList<ObjectiveSpawnPlan> ObjectiveSpawns { get; }
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
            string encounterGroupId,
            DungeonPose pose)
        {
            PlacementId = placementId;
            EnemyId = enemyId;
            EncounterGroupId = encounterGroupId;
            Pose = pose;
        }

        public string PlacementId { get; }
        public string EnemyId { get; }
        public string EncounterGroupId { get; }
        public DungeonPose Pose { get; }
    }

    public readonly struct InterestPointSpawnPlan
    {
        public InterestPointSpawnPlan(
            string placementId,
            string interestPointId,
            string rewardProfileId,
            DungeonPose pose)
        {
            PlacementId = placementId;
            InterestPointId = interestPointId;
            RewardProfileId = rewardProfileId;
            Pose = pose;
        }

        public string PlacementId { get; }
        public string InterestPointId { get; }
        public string RewardProfileId { get; }
        public DungeonPose Pose { get; }
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
