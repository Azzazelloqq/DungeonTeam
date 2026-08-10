using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DungeonTeam.Gameplay.Skills.Domain
{
    public enum SkillTargetRule
    {
        EnemyActor = 0
    }

    public abstract class SkillLevelDefinition
    {
        protected SkillLevelDefinition(
            int level,
            int damage,
            float range,
            float cooldown,
            SkillUseTiming useTiming)
        {
            Level = level > 0 ? level : throw new ArgumentOutOfRangeException(nameof(level));
            Damage = damage > 0 ? damage : throw new ArgumentOutOfRangeException(nameof(damage));
            Range = RequirePositiveFinite(range, nameof(range));
            Cooldown = RequirePositiveFinite(cooldown, nameof(cooldown));
            UseTiming = useTiming;
        }

        public int Level { get; }
        public int Damage { get; }
        public float Range { get; }
        public float Cooldown { get; }
        public SkillUseTiming UseTiming { get; }

        protected static float RequirePositiveFinite(float value, string parameterName)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0f)
            {
                throw new ArgumentOutOfRangeException(parameterName);
            }

            return value;
        }
    }

    public sealed class DirectDamageSkillLevelDefinition : SkillLevelDefinition
    {
        public DirectDamageSkillLevelDefinition(
            int level,
            int damage,
            float range,
            float cooldown,
            SkillUseTiming useTiming = default)
            : base(level, damage, range, cooldown, useTiming)
        {
        }
    }

    public sealed class ProjectileDamageSkillLevelDefinition : SkillLevelDefinition
    {
        public ProjectileDamageSkillLevelDefinition(
            int level,
            int damage,
            float range,
            float cooldown,
            float projectileSpeed,
            SkillUseTiming useTiming = default)
            : base(level, damage, range, cooldown, useTiming)
        {
            ProjectileSpeed = RequirePositiveFinite(
                projectileSpeed,
                nameof(projectileSpeed));
        }

        public float ProjectileSpeed { get; }
    }

    public abstract class SkillDefinition
    {
        private readonly ReadOnlyCollection<SkillLevelDefinition> _levels;
        private readonly Dictionary<int, SkillLevelDefinition> _levelsByNumber;

        protected SkillDefinition(
            string skillId,
            string displayName,
            SkillTargetRule targetRule,
            IReadOnlyList<SkillLevelDefinition> levels)
        {
            SkillId = RequireValue(skillId, nameof(skillId));
            DisplayName = RequireValue(displayName, nameof(displayName));
            if (!Enum.IsDefined(typeof(SkillTargetRule), targetRule))
            {
                throw new ArgumentOutOfRangeException(nameof(targetRule));
            }

            TargetRule = targetRule;
            if (levels == null || levels.Count == 0)
            {
                throw new ArgumentException(
                    $"Skill '{SkillId}' requires at least one level.",
                    nameof(levels));
            }

            var copiedLevels = new SkillLevelDefinition[levels.Count];
            _levelsByNumber = new Dictionary<int, SkillLevelDefinition>(levels.Count);
            for (var index = 0; index < levels.Count; index++)
            {
                var level = levels[index] ?? throw new ArgumentException(
                    $"Skill '{SkillId}' has a missing level at index {index}.",
                    nameof(levels));
                if (!_levelsByNumber.TryAdd(level.Level, level))
                {
                    throw new ArgumentException(
                        $"Skill '{SkillId}' contains level {level.Level} more than once.",
                        nameof(levels));
                }

                copiedLevels[index] = level;
            }

            Array.Sort(copiedLevels, (first, second) => first.Level.CompareTo(second.Level));
            _levels = Array.AsReadOnly(copiedLevels);
        }

        public string SkillId { get; }
        public string DisplayName { get; }
        public SkillTargetRule TargetRule { get; }
        public IReadOnlyList<SkillLevelDefinition> Levels => _levels;

        public SkillLevelDefinition RequireLevel(int level)
        {
            if (!_levelsByNumber.TryGetValue(level, out var definition))
            {
                throw new InvalidOperationException(
                    $"Skill '{SkillId}' does not contain level {level}.");
            }

            return definition;
        }

        private static string RequireValue(string value, string parameterName)
        {
            return !string.IsNullOrWhiteSpace(value)
                ? value
                : throw new ArgumentException("Value cannot be empty.", parameterName);
        }
    }

    public sealed class DirectDamageSkillDefinition : SkillDefinition
    {
        public DirectDamageSkillDefinition(
            string skillId,
            string displayName,
            SkillTargetRule targetRule,
            IReadOnlyList<DirectDamageSkillLevelDefinition> levels)
            : base(skillId, displayName, targetRule, Copy(levels))
        {
        }

        private static SkillLevelDefinition[] Copy(
            IReadOnlyList<DirectDamageSkillLevelDefinition> levels)
        {
            if (levels == null)
            {
                throw new ArgumentNullException(nameof(levels));
            }

            var result = new SkillLevelDefinition[levels.Count];
            for (var index = 0; index < levels.Count; index++)
            {
                result[index] = levels[index];
            }

            return result;
        }
    }

    public sealed class ProjectileDamageSkillDefinition : SkillDefinition
    {
        public ProjectileDamageSkillDefinition(
            string skillId,
            string displayName,
            SkillTargetRule targetRule,
            IReadOnlyList<ProjectileDamageSkillLevelDefinition> levels)
            : base(skillId, displayName, targetRule, Copy(levels))
        {
        }

        private static SkillLevelDefinition[] Copy(
            IReadOnlyList<ProjectileDamageSkillLevelDefinition> levels)
        {
            if (levels == null)
            {
                throw new ArgumentNullException(nameof(levels));
            }

            var result = new SkillLevelDefinition[levels.Count];
            for (var index = 0; index < levels.Count; index++)
            {
                result[index] = levels[index];
            }

            return result;
        }
    }
}
