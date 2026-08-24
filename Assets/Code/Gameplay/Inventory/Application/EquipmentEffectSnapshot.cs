using System;

namespace DungeonTeam.Gameplay.Inventory.Application
{
    public readonly struct EquipmentEffectSnapshot
    {
        public EquipmentEffectSnapshot(int primaryDamageBonus, int maximumHealthBonus, float movementSpeedBonus)
        {
            PrimaryDamageBonus = primaryDamageBonus >= 0
                ? primaryDamageBonus
                : throw new ArgumentOutOfRangeException(nameof(primaryDamageBonus));
            MaximumHealthBonus = maximumHealthBonus >= 0
                ? maximumHealthBonus
                : throw new ArgumentOutOfRangeException(nameof(maximumHealthBonus));
            MovementSpeedBonus = movementSpeedBonus >= 0f && !float.IsNaN(movementSpeedBonus) && !float.IsInfinity(movementSpeedBonus)
                ? movementSpeedBonus
                : throw new ArgumentOutOfRangeException(nameof(movementSpeedBonus));
        }

        public int PrimaryDamageBonus { get; }
        public int MaximumHealthBonus { get; }
        public float MovementSpeedBonus { get; }
        public static EquipmentEffectSnapshot Zero => new(0, 0, 0f);
    }
}
