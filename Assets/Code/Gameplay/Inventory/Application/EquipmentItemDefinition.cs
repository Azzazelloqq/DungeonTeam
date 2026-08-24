using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DungeonTeam.Gameplay.Inventory.Domain;

namespace DungeonTeam.Gameplay.Inventory.Application
{
    public enum EquipmentEffectKind
    {
        PrimaryDamage = 0,
        MaximumHealth = 1,
        MovementSpeed = 2
    }

    public sealed class EquipmentItemDefinition
    {
        private readonly ReadOnlyCollection<string> _eligibleActorIds;

        public EquipmentItemDefinition(
            string definitionId,
            string displayName,
            int saleValue,
            EquipmentSlot slot,
            EquipmentEffectKind effect,
            float effectValue,
            IReadOnlyList<string> eligibleActorIds)
        {
            DefinitionId = Require(definitionId, nameof(definitionId));
            DisplayName = Require(displayName, nameof(displayName));
            SaleValue = saleValue >= 0 ? saleValue : throw new ArgumentOutOfRangeException(nameof(saleValue));
            if (!Enum.IsDefined(typeof(EquipmentSlot), slot))
            {
                throw new ArgumentOutOfRangeException(nameof(slot), slot, "Unknown equipment slot.");
            }
            if (!Enum.IsDefined(typeof(EquipmentEffectKind), effect))
            {
                throw new ArgumentOutOfRangeException(nameof(effect), effect, "Unknown equipment effect.");
            }

            if (effectValue <= 0f || float.IsNaN(effectValue) || float.IsInfinity(effectValue))
            {
                throw new ArgumentOutOfRangeException(nameof(effectValue));
            }

            if (effect != EquipmentEffectKind.MovementSpeed &&
                (effectValue > int.MaxValue || effectValue != (int)effectValue))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(effectValue),
                    "Damage and health effects must use whole-number values.");
            }

            if (eligibleActorIds == null || eligibleActorIds.Count == 0)
            {
                throw new ArgumentException("At least one eligible actor is required.", nameof(eligibleActorIds));
            }

            var actors = new string[eligibleActorIds.Count];
            var uniqueActors = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < actors.Length; index++)
            {
                actors[index] = Require(eligibleActorIds[index], nameof(eligibleActorIds));
                if (!uniqueActors.Add(actors[index]))
                {
                    throw new ArgumentException("Eligible actor IDs must be unique.", nameof(eligibleActorIds));
                }
            }

            Slot = slot;
            Effect = effect;
            EffectValue = effectValue;
            _eligibleActorIds = Array.AsReadOnly(actors);
        }

        public string DefinitionId { get; }
        public string DisplayName { get; }
        public int SaleValue { get; }
        public EquipmentSlot Slot { get; }
        public EquipmentEffectKind Effect { get; }
        public float EffectValue { get; }
        public IReadOnlyList<string> EligibleActorIds => _eligibleActorIds;

        public bool IsEligibleFor(string actorId)
        {
            for (var index = 0; index < _eligibleActorIds.Count; index++)
            {
                if (string.Equals(_eligibleActorIds[index], actorId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static string Require(string value, string parameterName) =>
            !string.IsNullOrWhiteSpace(value)
                ? value
                : throw new ArgumentException("Value cannot be empty.", parameterName);
    }
}
