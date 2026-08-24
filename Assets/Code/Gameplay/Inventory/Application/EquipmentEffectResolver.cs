using System;
using DungeonTeam.Gameplay.Inventory.Domain;

namespace DungeonTeam.Gameplay.Inventory.Application
{
    public sealed class EquipmentEffectResolver
    {
        private readonly ItemCatalog _catalog;

        public EquipmentEffectResolver(ItemCatalog catalog)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        public EquipmentEffectSnapshot Resolve(InventoryState inventory, string actorId)
        {
            if (inventory == null)
            {
                throw new ArgumentNullException(nameof(inventory));
            }

            if (!inventory.TryGetEquipment(actorId, out var equipment))
            {
                throw new ArgumentException("Hero does not have an equipment mapping.", nameof(actorId));
            }

            var primary = 0;
            var health = 0;
            var speed = 0f;
            ResolveItem(inventory, actorId, equipment.WeaponInstanceId, EquipmentSlot.Weapon, ref primary, ref health, ref speed);
            ResolveItem(inventory, actorId, equipment.ArmorInstanceId, EquipmentSlot.Armor, ref primary, ref health, ref speed);
            ResolveItem(inventory, actorId, equipment.RelicInstanceId, EquipmentSlot.Relic, ref primary, ref health, ref speed);
            return new EquipmentEffectSnapshot(primary, health, speed);
        }

        public void ValidateInventory(InventoryState inventory)
        {
            if (inventory == null)
                throw new ArgumentNullException(nameof(inventory));

            for (var index = 0; index < inventory.UniqueItems.Count; index++)
            {
                var item = inventory.UniqueItems[index];
                if (!_catalog.TryGetEquipment(item.DefinitionId, out _))
                {
                    if (_catalog.TryGetResource(item.DefinitionId, out _))
                        throw new InvalidOperationException($"Resource '{item.DefinitionId}' cannot be a unique equipment instance.");
                    throw new InvalidOperationException($"Equipment definition '{item.DefinitionId}' is not configured.");
                }
            }

            for (var index = 0; index < inventory.Resources.Count; index++)
                _catalog.RequireResource(inventory.Resources[index].DefinitionId);

            for (var index = 0; index < inventory.EquipmentByHero.Count; index++)
                Resolve(inventory, inventory.EquipmentByHero[index].ActorId);
        }

        private void ResolveItem(
            InventoryState inventory,
            string actorId,
            string instanceId,
            EquipmentSlot slot,
            ref int primary,
            ref int health,
            ref float speed)
        {
            if (string.IsNullOrWhiteSpace(instanceId))
            {
                return;
            }

            if (!inventory.TryGetInstance(instanceId, out var instance))
            {
                throw new InvalidOperationException($"Equipped item instance '{instanceId}' is not owned.");
            }

            var definition = _catalog.RequireEquipment(instance.DefinitionId);
            if (definition.Slot != slot || !definition.IsEligibleFor(actorId))
            {
                throw new InvalidOperationException(
                    $"Equipment '{definition.DefinitionId}' is incompatible with actor '{actorId}' or slot '{slot}'.");
            }

            switch (definition.Effect)
            {
                case EquipmentEffectKind.PrimaryDamage:
                    primary += checked((int)definition.EffectValue);
                    break;
                case EquipmentEffectKind.MaximumHealth:
                    health += checked((int)definition.EffectValue);
                    break;
                case EquipmentEffectKind.MovementSpeed:
                    speed += definition.EffectValue;
                    break;
                default:
                    throw new InvalidOperationException("Unknown equipment effect.");
            }
        }
    }
}
