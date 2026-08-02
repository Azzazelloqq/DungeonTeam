using System;
using System.Collections.Generic;

namespace DungeonTeam.Gameplay.Dungeon.Domain
{
    public sealed class DungeonContentPlanner
    {
        public DungeonContentPlan Build(
            int seed,
            IReadOnlyList<EnemyPlacement> enemyPlacements,
            IReadOnlyList<InterestPointPlacement> interestPointPlacements,
            IReadOnlyList<ObjectivePlacement> objectivePlacements,
            DungeonScenario scenario,
            DungeonDifficulty difficulty)
        {
            if (enemyPlacements == null)
            {
                throw new ArgumentNullException(nameof(enemyPlacements));
            }

            if (interestPointPlacements == null)
            {
                throw new ArgumentNullException(nameof(interestPointPlacements));
            }

            if (objectivePlacements == null)
            {
                throw new ArgumentNullException(nameof(objectivePlacements));
            }

            if (scenario == null)
            {
                throw new ArgumentNullException(nameof(scenario));
            }

            if (difficulty == null)
            {
                throw new ArgumentNullException(nameof(difficulty));
            }

            var random = new DungeonDeterministicRandom(seed);
            var enabledOptionalPlacements = new HashSet<string>(
                scenario.EnabledOptionalPlacementIds,
                StringComparer.Ordinal);
            var enemySpawns = BuildEnemySpawns(
                enemyPlacements,
                scenario,
                difficulty,
                enabledOptionalPlacements,
                random);
            var interestPointSpawns = BuildInterestPointSpawns(
                interestPointPlacements,
                scenario,
                difficulty,
                enabledOptionalPlacements,
                random);
            var objectiveSpawns = BuildObjectiveSpawns(objectivePlacements, scenario);

            return new DungeonContentPlan(
                enemySpawns.ToArray(),
                interestPointSpawns.ToArray(),
                objectiveSpawns.ToArray(),
                difficulty.RewardBudgetMultiplier);
        }

        private static List<EnemySpawnPlan> BuildEnemySpawns(
            IReadOnlyList<EnemyPlacement> placements,
            DungeonScenario scenario,
            DungeonDifficulty difficulty,
            ISet<string> enabledOptionalPlacements,
            DungeonDeterministicRandom random)
        {
            var spawns = new List<EnemySpawnPlan>(placements.Count);

            for (var index = 0; index < placements.Count; index++)
            {
                var placement = placements[index];
                if (placement.Mode == DungeonPlacementMode.Fixed ||
                    placement.Mode == DungeonPlacementMode.OptionalFixed &&
                    enabledOptionalPlacements.Contains(placement.AuthoringId))
                {
                    spawns.Add(new EnemySpawnPlan(
                        placement.PlacementId,
                        placement.FixedEnemyId,
                        placement.EncounterGroupId,
                        placement.Pose));
                }
            }

            var remainingBudget = (int)Math.Floor(
                scenario.ThreatBudget * difficulty.ThreatBudgetMultiplier);

            for (var index = 0; index < placements.Count && remainingBudget > 0; index++)
            {
                var placement = placements[index];
                if (placement.Mode != DungeonPlacementMode.Slot ||
                    !TrySelectEnemy(
                        scenario.EnemyCandidates,
                        placement.SlotTag,
                        remainingBudget,
                        random,
                        out var candidate))
                {
                    continue;
                }

                spawns.Add(new EnemySpawnPlan(
                    placement.PlacementId,
                    candidate.EnemyId,
                    placement.EncounterGroupId,
                    placement.Pose));
                remainingBudget -= candidate.Cost;
            }

            return spawns;
        }

        private static List<InterestPointSpawnPlan> BuildInterestPointSpawns(
            IReadOnlyList<InterestPointPlacement> placements,
            DungeonScenario scenario,
            DungeonDifficulty difficulty,
            ISet<string> enabledOptionalPlacements,
            DungeonDeterministicRandom random)
        {
            var spawns = new List<InterestPointSpawnPlan>(placements.Count);
            var usedPlacements = new HashSet<string>(StringComparer.Ordinal);

            for (var index = 0; index < placements.Count; index++)
            {
                var placement = placements[index];
                if (placement.Mode == DungeonPlacementMode.Fixed ||
                    placement.Mode == DungeonPlacementMode.OptionalFixed &&
                    enabledOptionalPlacements.Contains(placement.AuthoringId))
                {
                    spawns.Add(new InterestPointSpawnPlan(
                        placement.PlacementId,
                        placement.FixedInterestPointId,
                        placement.FixedRewardProfileId,
                        placement.Pose));
                    usedPlacements.Add(placement.PlacementId);
                }
            }

            for (var ruleIndex = 0; ruleIndex < scenario.InterestPointRules.Count; ruleIndex++)
            {
                var rule = scenario.InterestPointRules[ruleIndex];
                var baseCount = random.NextInclusive(rule.MinCount, rule.MaxCount);
                var targetCount = (int)Math.Floor(
                    baseCount * difficulty.InterestPointCountMultiplier);

                for (var placementIndex = 0;
                     placementIndex < placements.Count && targetCount > 0;
                     placementIndex++)
                {
                    var placement = placements[placementIndex];
                    if (placement.Mode != DungeonPlacementMode.Slot ||
                        !string.Equals(placement.SlotTag, rule.SlotTag, StringComparison.Ordinal) ||
                        usedPlacements.Contains(placement.PlacementId))
                    {
                        continue;
                    }

                    var candidate = SelectInterestPoint(rule.Candidates, random);
                    spawns.Add(new InterestPointSpawnPlan(
                        placement.PlacementId,
                        candidate.InterestPointId,
                        candidate.RewardProfileId,
                        placement.Pose));
                    usedPlacements.Add(placement.PlacementId);
                    targetCount--;
                }
            }

            return spawns;
        }

        private static List<ObjectiveSpawnPlan> BuildObjectiveSpawns(
            IReadOnlyList<ObjectivePlacement> placements,
            DungeonScenario scenario)
        {
            var spawns = new List<ObjectiveSpawnPlan>(scenario.RequiredObjectives.Count);
            var usedPlacements = new HashSet<string>(StringComparer.Ordinal);

            for (var objectiveIndex = 0;
                 objectiveIndex < scenario.RequiredObjectives.Count;
                 objectiveIndex++)
            {
                var objective = scenario.RequiredObjectives[objectiveIndex];
                var found = false;

                for (var placementIndex = 0; placementIndex < placements.Count; placementIndex++)
                {
                    var placement = placements[placementIndex];
                    if (usedPlacements.Contains(placement.PlacementId) ||
                        !string.Equals(
                            placement.SlotTag,
                            objective.RequiredSlotTag,
                            StringComparison.Ordinal))
                    {
                        continue;
                    }

                    spawns.Add(new ObjectiveSpawnPlan(
                        placement.PlacementId,
                        objective.ObjectiveId,
                        placement.Pose));
                    usedPlacements.Add(placement.PlacementId);
                    found = true;
                    break;
                }

                if (!found)
                {
                    throw new InvalidOperationException(
                        $"Objective '{objective.ObjectiveId}' has no free placement with tag " +
                        $"'{objective.RequiredSlotTag}'.");
                }
            }

            return spawns;
        }

        private static bool TrySelectEnemy(
            IReadOnlyList<EnemyCandidate> candidates,
            string slotTag,
            int remainingBudget,
            DungeonDeterministicRandom random,
            out EnemyCandidate selected)
        {
            var totalWeight = 0;
            for (var index = 0; index < candidates.Count; index++)
            {
                var candidate = candidates[index];
                if (candidate.Cost <= remainingBudget && AllowsSlot(candidate, slotTag))
                {
                    totalWeight += candidate.Weight;
                }
            }

            if (totalWeight == 0)
            {
                selected = default;
                return false;
            }

            var roll = random.Next(totalWeight);
            for (var index = 0; index < candidates.Count; index++)
            {
                var candidate = candidates[index];
                if (candidate.Cost > remainingBudget || !AllowsSlot(candidate, slotTag))
                {
                    continue;
                }

                if (roll < candidate.Weight)
                {
                    selected = candidate;
                    return true;
                }

                roll -= candidate.Weight;
            }

            throw new InvalidOperationException("Weighted enemy selection failed.");
        }

        private static bool AllowsSlot(EnemyCandidate candidate, string slotTag)
        {
            for (var index = 0; index < candidate.AllowedSlotTags.Count; index++)
            {
                if (string.Equals(
                        candidate.AllowedSlotTags[index],
                        slotTag,
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static InterestPointCandidate SelectInterestPoint(
            IReadOnlyList<InterestPointCandidate> candidates,
            DungeonDeterministicRandom random)
        {
            var totalWeight = 0;
            for (var index = 0; index < candidates.Count; index++)
            {
                totalWeight += candidates[index].Weight;
            }

            var roll = random.Next(totalWeight);
            for (var index = 0; index < candidates.Count; index++)
            {
                var candidate = candidates[index];
                if (roll < candidate.Weight)
                {
                    return candidate;
                }

                roll -= candidate.Weight;
            }

            throw new InvalidOperationException("Weighted interest point selection failed.");
        }

    }
}
