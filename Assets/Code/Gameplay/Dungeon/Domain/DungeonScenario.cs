using System;
using System.Collections.Generic;

namespace DungeonTeam.Gameplay.Dungeon.Domain
{
    public sealed class DungeonScenario
    {
        public DungeonScenario(
            string scenarioId,
            int threatBudget,
            EnemyCandidate[] enemyCandidates,
            InterestPointRule[] interestPointRules,
            string[] enabledOptionalPlacementIds,
            RequiredObjective[] requiredObjectives)
            : this(
                scenarioId,
                threatBudget,
                enemyCandidates,
                interestPointRules,
                enabledOptionalPlacementIds,
                requiredObjectives,
                Array.Empty<DungeonRewardProfile>(),
                Array.Empty<EnemyRewardRule>(),
                completionRewardProfileId: null)
        {
        }

        public DungeonScenario(
            string scenarioId,
            int threatBudget,
            EnemyCandidate[] enemyCandidates,
            InterestPointRule[] interestPointRules,
            string[] enabledOptionalPlacementIds,
            RequiredObjective[] requiredObjectives,
            DungeonRewardProfile[] rewardProfiles,
            EnemyRewardRule[] enemyRewardRules,
            string completionRewardProfileId)
        {
            if (string.IsNullOrWhiteSpace(scenarioId))
            {
                throw new ArgumentException("Scenario ID cannot be empty.", nameof(scenarioId));
            }

            if (threatBudget < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(threatBudget));
            }

            ScenarioId = scenarioId;
            ThreatBudget = threatBudget;
            EnemyCandidates = Copy(enemyCandidates, nameof(enemyCandidates));
            InterestPointRules = Copy(interestPointRules, nameof(interestPointRules));
            EnabledOptionalPlacementIds = Copy(
                enabledOptionalPlacementIds,
                nameof(enabledOptionalPlacementIds));
            RequiredObjectives = Copy(requiredObjectives, nameof(requiredObjectives));
            RewardProfiles = Copy(rewardProfiles, nameof(rewardProfiles));
            EnemyRewardRules = Copy(enemyRewardRules, nameof(enemyRewardRules));
            CompletionRewardProfileId = completionRewardProfileId;

            foreach (var id in EnabledOptionalPlacementIds)
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    throw new ArgumentException(
                        "Enabled optional placement ID cannot be empty.",
                        nameof(enabledOptionalPlacementIds));
                }
            }
        }

        public string ScenarioId { get; }
        public int ThreatBudget { get; }
        public IReadOnlyList<EnemyCandidate> EnemyCandidates { get; }
        public IReadOnlyList<InterestPointRule> InterestPointRules { get; }
        public IReadOnlyList<string> EnabledOptionalPlacementIds { get; }
        public IReadOnlyList<RequiredObjective> RequiredObjectives { get; }
        public IReadOnlyList<DungeonRewardProfile> RewardProfiles { get; }
        public IReadOnlyList<EnemyRewardRule> EnemyRewardRules { get; }
        public string CompletionRewardProfileId { get; }

        private static IReadOnlyList<T> Copy<T>(T[] source, string parameterName)
        {
            if (source == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            return Array.AsReadOnly((T[])source.Clone());
        }
    }

    public sealed class DungeonRewardProfile
    {
        public DungeonRewardProfile(string profileId, DungeonRewardEntry[] entries)
        {
            if (string.IsNullOrWhiteSpace(profileId))
            {
                throw new ArgumentException("Reward profile ID cannot be empty.", nameof(profileId));
            }

            ProfileId = profileId;
            Entries = entries != null
                ? Array.AsReadOnly((DungeonRewardEntry[])entries.Clone())
                : throw new ArgumentNullException(nameof(entries));
        }

        public string ProfileId { get; }

        public IReadOnlyList<DungeonRewardEntry> Entries { get; }
    }

    public readonly struct DungeonRewardEntry
    {
        public DungeonRewardEntry(string rewardId, int amount)
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

    public readonly struct EnemyRewardRule
    {
        public EnemyRewardRule(string enemyId, string rewardProfileId)
        {
            if (string.IsNullOrWhiteSpace(enemyId))
            {
                throw new ArgumentException("Enemy ID cannot be empty.", nameof(enemyId));
            }

            if (string.IsNullOrWhiteSpace(rewardProfileId))
            {
                throw new ArgumentException(
                    "Reward profile ID cannot be empty.",
                    nameof(rewardProfileId));
            }

            EnemyId = enemyId;
            RewardProfileId = rewardProfileId;
        }

        public string EnemyId { get; }

        public string RewardProfileId { get; }
    }

    public sealed class DungeonDifficulty
    {
        public DungeonDifficulty(
            string difficultyId,
            float threatBudgetMultiplier,
            float interestPointCountMultiplier,
            float rewardBudgetMultiplier)
        {
            if (string.IsNullOrWhiteSpace(difficultyId))
            {
                throw new ArgumentException("Difficulty ID cannot be empty.", nameof(difficultyId));
            }

            RequireNonNegative(threatBudgetMultiplier, nameof(threatBudgetMultiplier));
            RequireNonNegative(interestPointCountMultiplier, nameof(interestPointCountMultiplier));
            RequireNonNegative(rewardBudgetMultiplier, nameof(rewardBudgetMultiplier));

            DifficultyId = difficultyId;
            ThreatBudgetMultiplier = threatBudgetMultiplier;
            InterestPointCountMultiplier = interestPointCountMultiplier;
            RewardBudgetMultiplier = rewardBudgetMultiplier;
        }

        public string DifficultyId { get; }
        public float ThreatBudgetMultiplier { get; }
        public float InterestPointCountMultiplier { get; }
        public float RewardBudgetMultiplier { get; }

        private static void RequireNonNegative(float value, string parameterName)
        {
            if (value < 0f)
            {
                throw new ArgumentOutOfRangeException(parameterName);
            }
        }
    }

    public readonly struct EnemyCandidate
    {
        public EnemyCandidate(
            string enemyId,
            string behaviorId,
            int cost,
            int weight,
            string[] allowedSlotTags)
        {
            if (string.IsNullOrWhiteSpace(enemyId))
            {
                throw new ArgumentException("Enemy ID cannot be empty.", nameof(enemyId));
            }

            if (string.IsNullOrWhiteSpace(behaviorId))
            {
                throw new ArgumentException("Behavior ID cannot be empty.", nameof(behaviorId));
            }

            if (cost <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(cost));
            }

            if (weight <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(weight));
            }

            if (allowedSlotTags == null)
            {
                throw new ArgumentNullException(nameof(allowedSlotTags));
            }

            EnemyId = enemyId;
            BehaviorId = behaviorId;
            Cost = cost;
            Weight = weight;
            AllowedSlotTags = Array.AsReadOnly((string[])allowedSlotTags.Clone());
        }

        public string EnemyId { get; }
        public string BehaviorId { get; }
        public int Cost { get; }
        public int Weight { get; }
        public IReadOnlyList<string> AllowedSlotTags { get; }
    }

    public sealed class InterestPointRule
    {
        public InterestPointRule(
            string slotTag,
            int minCount,
            int maxCount,
            InterestPointCandidate[] candidates)
        {
            if (string.IsNullOrWhiteSpace(slotTag))
            {
                throw new ArgumentException("Slot tag cannot be empty.", nameof(slotTag));
            }

            if (minCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minCount));
            }

            if (maxCount < minCount)
            {
                throw new ArgumentOutOfRangeException(nameof(maxCount));
            }

            if (candidates == null)
            {
                throw new ArgumentNullException(nameof(candidates));
            }

            if (maxCount > 0 && candidates.Length == 0)
            {
                throw new ArgumentException(
                    "A rule that can place content must contain at least one candidate.",
                    nameof(candidates));
            }

            SlotTag = slotTag;
            MinCount = minCount;
            MaxCount = maxCount;
            Candidates = Array.AsReadOnly((InterestPointCandidate[])candidates.Clone());
        }

        public string SlotTag { get; }
        public int MinCount { get; }
        public int MaxCount { get; }
        public IReadOnlyList<InterestPointCandidate> Candidates { get; }
    }

    public readonly struct InterestPointCandidate
    {
        public InterestPointCandidate(string interestPointId, int weight, string rewardProfileId)
        {
            if (string.IsNullOrWhiteSpace(interestPointId))
            {
                throw new ArgumentException("Interest point ID cannot be empty.", nameof(interestPointId));
            }

            if (weight <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(weight));
            }

            InterestPointId = interestPointId;
            Weight = weight;
            RewardProfileId = rewardProfileId;
        }

        public string InterestPointId { get; }
        public int Weight { get; }
        public string RewardProfileId { get; }
    }

    public readonly struct RequiredObjective
    {
        public RequiredObjective(string objectiveId, string requiredSlotTag)
        {
            if (string.IsNullOrWhiteSpace(objectiveId))
            {
                throw new ArgumentException("Objective ID cannot be empty.", nameof(objectiveId));
            }

            if (string.IsNullOrWhiteSpace(requiredSlotTag))
            {
                throw new ArgumentException("Required slot tag cannot be empty.", nameof(requiredSlotTag));
            }

            ObjectiveId = objectiveId;
            RequiredSlotTag = requiredSlotTag;
        }

        public string ObjectiveId { get; }
        public string RequiredSlotTag { get; }
    }
}
