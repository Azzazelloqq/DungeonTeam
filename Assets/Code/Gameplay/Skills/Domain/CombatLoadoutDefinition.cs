using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DungeonTeam.Gameplay.Skills.Domain
{
    public enum SkillSlot
    {
        Primary = 0,
        Active1 = 1
    }

    public readonly struct CombatLoadoutSlotDefinition
    {
        public CombatLoadoutSlotDefinition(SkillSlot slot, string skillId, int skillLevel)
        {
            if (!Enum.IsDefined(typeof(SkillSlot), slot))
            {
                throw new ArgumentOutOfRangeException(nameof(slot));
            }

            Slot = slot;
            SkillId = !string.IsNullOrWhiteSpace(skillId)
                ? skillId
                : throw new ArgumentException("Skill ID cannot be empty.", nameof(skillId));
            SkillLevel = skillLevel > 0
                ? skillLevel
                : throw new ArgumentOutOfRangeException(nameof(skillLevel));
        }

        public SkillSlot Slot { get; }
        public string SkillId { get; }
        public int SkillLevel { get; }
    }

    public sealed class CombatLoadoutDefinition
    {
        private readonly ReadOnlyCollection<CombatLoadoutSlotDefinition> _slots;
        private readonly Dictionary<SkillSlot, CombatLoadoutSlotDefinition> _slotsById;

        public CombatLoadoutDefinition(
            string loadoutId,
            IReadOnlyList<CombatLoadoutSlotDefinition> slots)
        {
            LoadoutId = !string.IsNullOrWhiteSpace(loadoutId)
                ? loadoutId
                : throw new ArgumentException("Loadout ID cannot be empty.", nameof(loadoutId));
            if (slots == null || slots.Count == 0)
            {
                throw new ArgumentException(
                    $"Combat loadout '{LoadoutId}' requires at least one slot.",
                    nameof(slots));
            }

            var copiedSlots = new CombatLoadoutSlotDefinition[slots.Count];
            _slotsById = new Dictionary<SkillSlot, CombatLoadoutSlotDefinition>(slots.Count);
            for (var index = 0; index < slots.Count; index++)
            {
                var slot = slots[index];
                if (!_slotsById.TryAdd(slot.Slot, slot))
                {
                    throw new ArgumentException(
                        $"Combat loadout '{LoadoutId}' contains slot '{slot.Slot}' more than once.",
                        nameof(slots));
                }

                copiedSlots[index] = slot;
            }

            _slots = Array.AsReadOnly(copiedSlots);
        }

        public string LoadoutId { get; }
        public IReadOnlyList<CombatLoadoutSlotDefinition> Slots => _slots;

        public CombatLoadoutSlotDefinition RequireSlot(SkillSlot slot)
        {
            if (!_slotsById.TryGetValue(slot, out var definition))
            {
                throw new InvalidOperationException(
                    $"Combat loadout '{LoadoutId}' does not contain slot '{slot}'.");
            }

            return definition;
        }
    }
}
