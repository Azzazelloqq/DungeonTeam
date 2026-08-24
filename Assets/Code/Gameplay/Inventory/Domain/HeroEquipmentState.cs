using System;

namespace DungeonTeam.Gameplay.Inventory.Domain
{
    public readonly struct HeroEquipmentState
    {
        public HeroEquipmentState(
            string actorId,
            string weaponInstanceId = null,
            string armorInstanceId = null,
            string relicInstanceId = null)
        {
            ActorId = InventoryValidation.RequireId(actorId, nameof(actorId));
            WeaponInstanceId = RequireOptionalId(weaponInstanceId, nameof(weaponInstanceId));
            ArmorInstanceId = RequireOptionalId(armorInstanceId, nameof(armorInstanceId));
            RelicInstanceId = RequireOptionalId(relicInstanceId, nameof(relicInstanceId));
        }

        public string ActorId { get; }
        public string WeaponInstanceId { get; }
        public string ArmorInstanceId { get; }
        public string RelicInstanceId { get; }

        public string GetInstanceId(EquipmentSlot slot)
        {
            InventoryValidation.RequireSlot(slot, nameof(slot));
            return slot switch
            {
                EquipmentSlot.Weapon => WeaponInstanceId,
                EquipmentSlot.Armor => ArmorInstanceId,
                EquipmentSlot.Relic => RelicInstanceId,
                _ => throw new ArgumentOutOfRangeException(nameof(slot), slot, null)
            };
        }

        public HeroEquipmentState SetInstanceId(EquipmentSlot slot, string instanceId)
        {
            InventoryValidation.RequireSlot(slot, nameof(slot));
            instanceId = RequireOptionalId(instanceId, nameof(instanceId));
            return slot switch
            {
                EquipmentSlot.Weapon => new HeroEquipmentState(ActorId, instanceId, ArmorInstanceId, RelicInstanceId),
                EquipmentSlot.Armor => new HeroEquipmentState(ActorId, WeaponInstanceId, instanceId, RelicInstanceId),
                EquipmentSlot.Relic => new HeroEquipmentState(ActorId, WeaponInstanceId, ArmorInstanceId, instanceId),
                _ => throw new ArgumentOutOfRangeException(nameof(slot), slot, null)
            };
        }

        private static string RequireOptionalId(string value, string parameterName)
        {
            return string.IsNullOrWhiteSpace(value) ? null : InventoryValidation.RequireId(value, parameterName);
        }
    }
}
