using System;

namespace DungeonTeam.Gameplay.Inventory.Domain
{
    public enum EquipmentSlot
    {
        Weapon = 0,
        Armor = 1,
        Relic = 2
    }

    internal static class InventoryValidation
    {
        public static string RequireId(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("ID cannot be empty.", parameterName);
            }

            return value;
        }

        public static void RequireSlot(EquipmentSlot slot, string parameterName)
        {
            if (!Enum.IsDefined(typeof(EquipmentSlot), slot))
            {
                throw new ArgumentOutOfRangeException(parameterName, slot, "Unknown equipment slot.");
            }
        }
    }
}
