using System;
using Code.Configuration;
using DungeonTeam.Gameplay.Skills.Domain;
using UnityEngine;

namespace DungeonTeam.Gameplay.Skills.Runtime
{
    [CreateAssetMenu(
        menuName = "DungeonTeam/Gameplay/Skill Config",
        fileName = "SkillConfig")]
    public sealed class SkillConfigPage : ConfigPage
    {
        [SerializeField]
        private DirectDamageSkillDefinitionConfig[] _directDamageSkills =
            Array.Empty<DirectDamageSkillDefinitionConfig>();

        [SerializeField]
        private ProjectileDamageSkillDefinitionConfig[] _projectileDamageSkills =
            Array.Empty<ProjectileDamageSkillDefinitionConfig>();

        [SerializeField]
        private CombatLoadoutDefinitionConfig[] _loadouts =
            Array.Empty<CombatLoadoutDefinitionConfig>();

        public SkillCatalog CreateCatalog()
        {
            return new SkillCatalog(_directDamageSkills, _projectileDamageSkills, _loadouts);
        }
    }

    [Serializable]
    public sealed class DirectDamageSkillLevelConfig
    {
        [SerializeField, Min(1)] private int _level = 1;
        [SerializeField, Min(1)] private int _damage = 1;
        [SerializeField, Min(0.1f)] private float _range = 1f;
        [SerializeField, Min(0.01f)] private float _cooldown = 1f;
        [SerializeField, Min(0f)] private float _commitDelay;
        [SerializeField, Min(0f)] private float _recoveryDuration;

        public DirectDamageSkillLevelConfig(
            int level,
            int damage,
            float range,
            float cooldown,
            float commitDelay = 0f,
            float recoveryDuration = 0f)
        {
            _level = level;
            _damage = damage;
            _range = range;
            _cooldown = cooldown;
            _commitDelay = commitDelay;
            _recoveryDuration = recoveryDuration;
        }

        internal DirectDamageSkillLevelDefinition ToDomain()
        {
            return new DirectDamageSkillLevelDefinition(
                _level,
                _damage,
                _range,
                _cooldown,
                new SkillUseTiming(_commitDelay, _recoveryDuration));
        }
    }

    [Serializable]
    public sealed class ProjectileDamageSkillLevelConfig
    {
        [SerializeField, Min(1)] private int _level = 1;
        [SerializeField, Min(1)] private int _damage = 1;
        [SerializeField, Min(0.1f)] private float _range = 1f;
        [SerializeField, Min(0.01f)] private float _cooldown = 1f;
        [SerializeField, Min(0.1f)] private float _projectileSpeed = 1f;
        [SerializeField, Min(0f)] private float _commitDelay;
        [SerializeField, Min(0f)] private float _recoveryDuration;

        public ProjectileDamageSkillLevelConfig(
            int level,
            int damage,
            float range,
            float cooldown,
            float projectileSpeed,
            float commitDelay = 0f,
            float recoveryDuration = 0f)
        {
            _level = level;
            _damage = damage;
            _range = range;
            _cooldown = cooldown;
            _projectileSpeed = projectileSpeed;
            _commitDelay = commitDelay;
            _recoveryDuration = recoveryDuration;
        }

        internal ProjectileDamageSkillLevelDefinition ToDomain()
        {
            return new ProjectileDamageSkillLevelDefinition(
                _level,
                _damage,
                _range,
                _cooldown,
                _projectileSpeed,
                new SkillUseTiming(_commitDelay, _recoveryDuration));
        }
    }

    [Serializable]
    public sealed class DirectDamageSkillDefinitionConfig
    {
        [SerializeField] private string _skillId;
        [SerializeField] private string _displayName;
        [SerializeField] private SkillTargetRule _targetRule = SkillTargetRule.EnemyActor;
        [SerializeField] private DirectDamageSkillLevelConfig[] _levels =
            Array.Empty<DirectDamageSkillLevelConfig>();

        public DirectDamageSkillDefinitionConfig(
            string skillId,
            string displayName,
            SkillTargetRule targetRule,
            DirectDamageSkillLevelConfig[] levels)
        {
            _skillId = skillId;
            _displayName = displayName;
            _targetRule = targetRule;
            _levels = levels;
        }

        internal DirectDamageSkillDefinition ToDomain(int index)
        {
            if (_levels == null)
            {
                throw new ArgumentException(
                    $"Direct damage skill at index {index} has no levels.");
            }

            var levels = new DirectDamageSkillLevelDefinition[_levels.Length];
            for (var levelIndex = 0; levelIndex < levels.Length; levelIndex++)
            {
                levels[levelIndex] = (_levels[levelIndex] ?? throw new ArgumentException(
                    $"Direct damage skill at index {index} has a missing level at index " +
                    $"{levelIndex}.")).ToDomain();
            }

            return new DirectDamageSkillDefinition(_skillId, _displayName, _targetRule, levels);
        }
    }

    [Serializable]
    public sealed class ProjectileDamageSkillDefinitionConfig
    {
        [SerializeField] private string _skillId;
        [SerializeField] private string _displayName;
        [SerializeField] private SkillTargetRule _targetRule = SkillTargetRule.EnemyActor;
        [SerializeField] private ProjectileDamageSkillLevelConfig[] _levels =
            Array.Empty<ProjectileDamageSkillLevelConfig>();

        public ProjectileDamageSkillDefinitionConfig(
            string skillId,
            string displayName,
            SkillTargetRule targetRule,
            ProjectileDamageSkillLevelConfig[] levels)
        {
            _skillId = skillId;
            _displayName = displayName;
            _targetRule = targetRule;
            _levels = levels;
        }

        internal ProjectileDamageSkillDefinition ToDomain(int index)
        {
            if (_levels == null)
            {
                throw new ArgumentException(
                    $"Projectile damage skill at index {index} has no levels.");
            }

            var levels = new ProjectileDamageSkillLevelDefinition[_levels.Length];
            for (var levelIndex = 0; levelIndex < levels.Length; levelIndex++)
            {
                levels[levelIndex] = (_levels[levelIndex] ?? throw new ArgumentException(
                    $"Projectile damage skill at index {index} has a missing level at index " +
                    $"{levelIndex}.")).ToDomain();
            }

            return new ProjectileDamageSkillDefinition(
                _skillId,
                _displayName,
                _targetRule,
                levels);
        }
    }

    [Serializable]
    public sealed class CombatLoadoutSlotConfig
    {
        [SerializeField] private SkillSlot _slot;
        [SerializeField] private string _skillId;
        [SerializeField, Min(1)] private int _skillLevel = 1;

        public CombatLoadoutSlotConfig(SkillSlot slot, string skillId, int skillLevel)
        {
            _slot = slot;
            _skillId = skillId;
            _skillLevel = skillLevel;
        }

        internal CombatLoadoutSlotDefinition ToDomain()
        {
            return new CombatLoadoutSlotDefinition(_slot, _skillId, _skillLevel);
        }
    }

    [Serializable]
    public sealed class CombatLoadoutDefinitionConfig
    {
        [SerializeField] private string _loadoutId;
        [SerializeField] private CombatLoadoutSlotConfig[] _slots =
            Array.Empty<CombatLoadoutSlotConfig>();

        public CombatLoadoutDefinitionConfig(
            string loadoutId,
            CombatLoadoutSlotConfig[] slots)
        {
            _loadoutId = loadoutId;
            _slots = slots;
        }

        internal CombatLoadoutDefinition ToDomain(int index)
        {
            if (_slots == null)
            {
                throw new ArgumentException($"Combat loadout at index {index} has no slots.");
            }

            var slots = new CombatLoadoutSlotDefinition[_slots.Length];
            for (var slotIndex = 0; slotIndex < slots.Length; slotIndex++)
            {
                slots[slotIndex] = (_slots[slotIndex] ?? throw new ArgumentException(
                    $"Combat loadout at index {index} has a missing slot at index " +
                    $"{slotIndex}.")).ToDomain();
            }

            return new CombatLoadoutDefinition(_loadoutId, slots);
        }
    }
}
