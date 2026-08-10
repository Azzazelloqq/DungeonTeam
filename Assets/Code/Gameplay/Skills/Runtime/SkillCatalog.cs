using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DungeonTeam.Gameplay.Skills.Domain;

namespace DungeonTeam.Gameplay.Skills.Runtime
{
    public readonly struct ResolvedSkillSlot
    {
        public ResolvedSkillSlot(
            SkillSlot slot,
            SkillDefinition skill,
            SkillLevelDefinition level)
        {
            Slot = slot;
            Skill = skill ?? throw new ArgumentNullException(nameof(skill));
            Level = level ?? throw new ArgumentNullException(nameof(level));
        }

        public SkillSlot Slot { get; }
        public SkillDefinition Skill { get; }
        public SkillLevelDefinition Level { get; }
    }

    public sealed class SkillCatalog
    {
        private readonly Dictionary<string, SkillDefinition> _skills;
        private readonly Dictionary<string, CombatLoadoutDefinition> _loadouts;
        private readonly ReadOnlyCollection<CombatLoadoutDefinition> _allLoadouts;

        public SkillCatalog(
            DirectDamageSkillDefinitionConfig[] directDamageSkills,
            ProjectileDamageSkillDefinitionConfig[] projectileDamageSkills,
            CombatLoadoutDefinitionConfig[] loadouts)
        {
            if (directDamageSkills == null)
                throw new ArgumentNullException(nameof(directDamageSkills));
            if (projectileDamageSkills == null)
                throw new ArgumentNullException(nameof(projectileDamageSkills));
            if (loadouts == null)
                throw new ArgumentNullException(nameof(loadouts));

            _skills = new Dictionary<string, SkillDefinition>(
                directDamageSkills.Length + projectileDamageSkills.Length,
                StringComparer.Ordinal);
            AddSkills(directDamageSkills, (config, index) => config.ToDomain(index));
            AddSkills(projectileDamageSkills, (config, index) => config.ToDomain(index));

            _loadouts = new Dictionary<string, CombatLoadoutDefinition>(
                loadouts.Length,
                StringComparer.Ordinal);
            var allLoadouts = new CombatLoadoutDefinition[loadouts.Length];
            for (var index = 0; index < loadouts.Length; index++)
            {
                var definition = (loadouts[index] ?? throw new ArgumentException(
                    $"Combat loadout at index {index} is missing.",
                    nameof(loadouts))).ToDomain(index);
                ValidateSlots(definition, nameof(loadouts));
                if (!_loadouts.TryAdd(definition.LoadoutId, definition))
                {
                    throw new ArgumentException(
                        $"Combat loadout ID '{definition.LoadoutId}' is configured more than once.",
                        nameof(loadouts));
                }

                allLoadouts[index] = definition;
            }

            _allLoadouts = Array.AsReadOnly(allLoadouts);
        }

        public IReadOnlyList<CombatLoadoutDefinition> Loadouts => _allLoadouts;

        public SkillDefinition RequireSkill(string skillId)
        {
            return Require(_skills, skillId, "Skill");
        }

        public CombatLoadoutDefinition RequireLoadout(string loadoutId)
        {
            return Require(_loadouts, loadoutId, "Combat loadout");
        }

        public ResolvedSkillSlot Resolve(string loadoutId, SkillSlot slot)
        {
            var slotDefinition = RequireLoadout(loadoutId).RequireSlot(slot);
            var skill = RequireSkill(slotDefinition.SkillId);
            return new ResolvedSkillSlot(
                slot,
                skill,
                skill.RequireLevel(slotDefinition.SkillLevel));
        }

        private void AddSkills<TConfig>(
            TConfig[] configs,
            Func<TConfig, int, SkillDefinition> converter)
            where TConfig : class
        {
            for (var index = 0; index < configs.Length; index++)
            {
                var config = configs[index] ?? throw new ArgumentException(
                    $"Skill definition at index {index} is missing.",
                    nameof(configs));
                var definition = converter(config, index);
                if (!_skills.TryAdd(definition.SkillId, definition))
                {
                    throw new ArgumentException(
                        $"Skill ID '{definition.SkillId}' is configured more than once.",
                        nameof(configs));
                }
            }
        }

        private void ValidateSlots(CombatLoadoutDefinition loadout, string parameterName)
        {
            for (var index = 0; index < loadout.Slots.Count; index++)
            {
                var slot = loadout.Slots[index];
                if (!_skills.TryGetValue(slot.SkillId, out var skill))
                {
                    throw new ArgumentException(
                        $"Combat loadout '{loadout.LoadoutId}' references unknown skill ID " +
                        $"'{slot.SkillId}'.",
                        parameterName);
                }

                try
                {
                    skill.RequireLevel(slot.SkillLevel);
                }
                catch (InvalidOperationException exception)
                {
                    throw new ArgumentException(
                        $"Combat loadout '{loadout.LoadoutId}' references unknown level " +
                        $"{slot.SkillLevel} of skill '{slot.SkillId}'.",
                        parameterName,
                        exception);
                }
            }
        }

        private static T Require<T>(
            IReadOnlyDictionary<string, T> definitions,
            string id,
            string label)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException($"{label} ID cannot be empty.", nameof(id));
            }

            if (!definitions.TryGetValue(id, out var definition))
            {
                throw new InvalidOperationException($"{label} catalog does not contain ID '{id}'.");
            }

            return definition;
        }
    }
}
