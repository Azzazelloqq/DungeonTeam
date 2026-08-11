using System;
using System.Collections.Generic;

namespace DungeonTeam.Gameplay.Team.Domain
{
    public readonly struct CompanionHealthSnapshot
    {
        public CompanionHealthSnapshot(int currentHealth, int maximumHealth)
        {
            if (maximumHealth <= 0)
                throw new ArgumentOutOfRangeException(nameof(maximumHealth));
            if (currentHealth < 0 || currentHealth > maximumHealth)
                throw new ArgumentOutOfRangeException(nameof(currentHealth));

            CurrentHealth = currentHealth;
            MaximumHealth = maximumHealth;
        }

        public int CurrentHealth { get; }
        public int MaximumHealth { get; }
        public bool IsAlive => CurrentHealth > 0;
        public bool IsWounded => CurrentHealth < MaximumHealth;
    }

    public static class CompanionHealTargetSelector
    {
        public static int Select(
            IReadOnlyList<CompanionHealthSnapshot> candidates,
            float maximumHealthRatio)
        {
            if (candidates == null)
                throw new ArgumentNullException(nameof(candidates));
            if (float.IsNaN(maximumHealthRatio) ||
                float.IsInfinity(maximumHealthRatio) ||
                maximumHealthRatio <= 0f ||
                maximumHealthRatio >= 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumHealthRatio));
            }

            var selectedIndex = -1;
            for (var index = 0; index < candidates.Count; index++)
            {
                var candidate = candidates[index];
                if (!candidate.IsAlive ||
                    !candidate.IsWounded ||
                    candidate.CurrentHealth / (float)candidate.MaximumHealth >
                    maximumHealthRatio)
                {
                    continue;
                }

                if (selectedIndex >= 0)
                {
                    var selected = candidates[selectedIndex];
                    if ((long)candidate.CurrentHealth * selected.MaximumHealth >=
                        (long)selected.CurrentHealth * candidate.MaximumHealth)
                    {
                        continue;
                    }
                }

                selectedIndex = index;
            }

            return selectedIndex;
        }
    }
}
