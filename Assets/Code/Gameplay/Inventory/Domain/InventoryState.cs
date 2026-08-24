using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DungeonTeam.Gameplay.Inventory.Domain
{
    public sealed class InventoryState
    {
        private readonly ReadOnlyCollection<ItemInstanceState> _uniqueItems;
        private readonly ReadOnlyCollection<ResourceStackState> _resources;
        private readonly ReadOnlyCollection<HeroEquipmentState> _equipmentByHero;

        public InventoryState(
            IReadOnlyList<ItemInstanceState> uniqueItems,
            IReadOnlyList<ResourceStackState> resources,
            IReadOnlyList<HeroEquipmentState> equipmentByHero)
        {
            _uniqueItems = CopyUniqueItems(uniqueItems);
            _resources = CopyResources(resources);
            _equipmentByHero = CopyEquipment(equipmentByHero, _uniqueItems);
        }

        public static InventoryState Empty => new(
            Array.Empty<ItemInstanceState>(),
            Array.Empty<ResourceStackState>(),
            Array.Empty<HeroEquipmentState>());

        public IReadOnlyList<ItemInstanceState> UniqueItems => _uniqueItems;
        public IReadOnlyList<ResourceStackState> Resources => _resources;
        public IReadOnlyList<HeroEquipmentState> EquipmentByHero => _equipmentByHero;

        public InventoryState Equip(string actorId, string instanceId, EquipmentSlot slot)
        {
            InventoryValidation.RequireId(actorId, nameof(actorId));
            InventoryValidation.RequireId(instanceId, nameof(instanceId));
            InventoryValidation.RequireSlot(slot, nameof(slot));
            if (!ContainsInstance(instanceId))
            {
                throw new ArgumentException("Item instance is not owned by the inventory.", nameof(instanceId));
            }

            var heroIndex = FindHero(actorId);
            if (heroIndex < 0)
            {
                throw new ArgumentException("Hero does not have an equipment mapping.", nameof(actorId));
            }

            var equipment = CopyEquipment(_equipmentByHero);
            var existingIndex = FindEquippedInstance(instanceId);
            if (existingIndex >= 0 && existingIndex != heroIndex)
            {
                equipment[existingIndex] = RemoveInstance(equipment[existingIndex], instanceId);
            }

            equipment[heroIndex] = equipment[heroIndex].SetInstanceId(slot, instanceId);
            return new InventoryState(_uniqueItems, _resources, equipment);
        }

        public InventoryState Unequip(string actorId, EquipmentSlot slot)
        {
            InventoryValidation.RequireId(actorId, nameof(actorId));
            InventoryValidation.RequireSlot(slot, nameof(slot));
            var heroIndex = FindHero(actorId);
            if (heroIndex < 0)
            {
                throw new ArgumentException("Hero does not have an equipment mapping.", nameof(actorId));
            }

            if (string.IsNullOrWhiteSpace(_equipmentByHero[heroIndex].GetInstanceId(slot)))
            {
                return this;
            }

            var equipment = CopyEquipment(_equipmentByHero);
            equipment[heroIndex] = equipment[heroIndex].SetInstanceId(slot, null);
            return new InventoryState(_uniqueItems, _resources, equipment);
        }

        public bool ContainsInstance(string instanceId)
        {
            for (var index = 0; index < _uniqueItems.Count; index++)
            {
                if (string.Equals(_uniqueItems[index].InstanceId, instanceId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryGetInstance(string instanceId, out ItemInstanceState item)
        {
            for (var index = 0; index < _uniqueItems.Count; index++)
            {
                if (string.Equals(_uniqueItems[index].InstanceId, instanceId, StringComparison.Ordinal))
                {
                    item = _uniqueItems[index];
                    return true;
                }
            }

            item = default;
            return false;
        }

        public bool TryGetResource(string definitionId, out ResourceStackState resource)
        {
            var index = FindResource(definitionId);
            if (index >= 0)
            {
                resource = _resources[index];
                return true;
            }

            resource = default;
            return false;
        }

        public InventoryState RemoveUniqueItem(string instanceId)
        {
            InventoryValidation.RequireId(instanceId, nameof(instanceId));
            if (!ContainsInstance(instanceId))
            {
                throw new ArgumentException("Item instance is not owned by the inventory.", nameof(instanceId));
            }

            if (FindEquippedInstance(instanceId) >= 0)
            {
                throw new InvalidOperationException("An equipped item cannot be sold.");
            }

            var items = new ItemInstanceState[_uniqueItems.Count - 1];
            for (int sourceIndex = 0, targetIndex = 0; sourceIndex < _uniqueItems.Count; sourceIndex++)
            {
                if (!string.Equals(_uniqueItems[sourceIndex].InstanceId, instanceId, StringComparison.Ordinal))
                {
                    items[targetIndex++] = _uniqueItems[sourceIndex];
                }
            }

            return new InventoryState(items, _resources, _equipmentByHero);
        }

        public InventoryState RemoveResourceStack(string definitionId)
        {
            InventoryValidation.RequireId(definitionId, nameof(definitionId));
            var resourceIndex = FindResource(definitionId);
            if (resourceIndex < 0)
            {
                throw new ArgumentException("Resource stack is not owned by the inventory.", nameof(definitionId));
            }

            var resources = new ResourceStackState[_resources.Count - 1];
            for (int sourceIndex = 0, targetIndex = 0; sourceIndex < _resources.Count; sourceIndex++)
            {
                if (sourceIndex != resourceIndex)
                {
                    resources[targetIndex++] = _resources[sourceIndex];
                }
            }

            return new InventoryState(_uniqueItems, resources, _equipmentByHero);
        }

        public InventoryState AddResourceGrants(IReadOnlyList<ResourceStackState> grants)
        {
            if (grants == null)
            {
                throw new ArgumentNullException(nameof(grants));
            }

            if (grants.Count == 0)
            {
                return this;
            }

            var resources = new List<ResourceStackState>(_resources.Count + grants.Count);
            for (var index = 0; index < _resources.Count; index++)
            {
                resources.Add(_resources[index]);
            }

            for (var grantIndex = 0; grantIndex < grants.Count; grantIndex++)
            {
                var grant = grants[grantIndex];
                var existingIndex = FindResource(resources, grant.DefinitionId);
                if (existingIndex >= 0)
                {
                    resources[existingIndex] = new ResourceStackState(
                        grant.DefinitionId,
                        checked(resources[existingIndex].Quantity + grant.Quantity));
                }
                else
                {
                    resources.Add(grant);
                }
            }

            return new InventoryState(_uniqueItems, resources, _equipmentByHero);
        }

        public bool TryGetEquipment(string actorId, out HeroEquipmentState equipment)
        {
            var index = FindHero(actorId);
            if (index >= 0)
            {
                equipment = _equipmentByHero[index];
                return true;
            }

            equipment = default;
            return false;
        }

        private int FindHero(string actorId)
        {
            for (var index = 0; index < _equipmentByHero.Count; index++)
            {
                if (string.Equals(_equipmentByHero[index].ActorId, actorId, StringComparison.Ordinal))
                {
                    return index;
                }
            }

            return -1;
        }

        private int FindResource(string definitionId) => FindResource(_resources, definitionId);

        private static int FindResource(IReadOnlyList<ResourceStackState> resources, string definitionId)
        {
            for (var index = 0; index < resources.Count; index++)
            {
                if (string.Equals(resources[index].DefinitionId, definitionId, StringComparison.Ordinal))
                {
                    return index;
                }
            }

            return -1;
        }

        private int FindEquippedInstance(string instanceId)
        {
            for (var index = 0; index < _equipmentByHero.Count; index++)
            {
                var equipment = _equipmentByHero[index];
                if (string.Equals(equipment.WeaponInstanceId, instanceId, StringComparison.Ordinal) ||
                    string.Equals(equipment.ArmorInstanceId, instanceId, StringComparison.Ordinal) ||
                    string.Equals(equipment.RelicInstanceId, instanceId, StringComparison.Ordinal))
                {
                    return index;
                }
            }

            return -1;
        }

        private static ReadOnlyCollection<ItemInstanceState> CopyUniqueItems(IReadOnlyList<ItemInstanceState> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var copy = new ItemInstanceState[source.Count];
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < copy.Length; index++)
            {
                copy[index] = source[index];
                if (!ids.Add(copy[index].InstanceId))
                {
                    throw new ArgumentException("Item instance IDs must be unique.", nameof(source));
                }
            }

            return Array.AsReadOnly(copy);
        }

        private static ReadOnlyCollection<ResourceStackState> CopyResources(IReadOnlyList<ResourceStackState> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var copy = new ResourceStackState[source.Count];
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < copy.Length; index++)
            {
                copy[index] = source[index];
                if (!ids.Add(copy[index].DefinitionId))
                {
                    throw new ArgumentException("Resource definitions must be unique.", nameof(source));
                }
            }

            return Array.AsReadOnly(copy);
        }

        private static ReadOnlyCollection<HeroEquipmentState> CopyEquipment(
            IReadOnlyList<HeroEquipmentState> source,
            IReadOnlyList<ItemInstanceState> ownedItems = null)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var copy = new HeroEquipmentState[source.Count];
            var actors = new HashSet<string>(StringComparer.Ordinal);
            var equipped = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < copy.Length; index++)
            {
                copy[index] = source[index];
                if (!actors.Add(copy[index].ActorId))
                {
                    throw new ArgumentException("Hero equipment mappings must be unique.", nameof(source));
                }

                ValidateEquipped(copy[index].WeaponInstanceId, ownedItems, equipped);
                ValidateEquipped(copy[index].ArmorInstanceId, ownedItems, equipped);
                ValidateEquipped(copy[index].RelicInstanceId, ownedItems, equipped);
            }

            return Array.AsReadOnly(copy);
        }

        private static void ValidateEquipped(
            string instanceId,
            IReadOnlyList<ItemInstanceState> ownedItems,
            HashSet<string> equipped)
        {
            if (string.IsNullOrWhiteSpace(instanceId))
            {
                return;
            }

            if (!equipped.Add(instanceId))
            {
                throw new ArgumentException("An item instance cannot be equipped twice.", nameof(ownedItems));
            }

            if (ownedItems == null)
            {
                return;
            }

            for (var index = 0; index < ownedItems.Count; index++)
            {
                if (string.Equals(ownedItems[index].InstanceId, instanceId, StringComparison.Ordinal))
                {
                    return;
                }
            }

            throw new ArgumentException("Equipped item instance is not owned by inventory.", nameof(ownedItems));
        }

        private static HeroEquipmentState[] CopyEquipment(IReadOnlyList<HeroEquipmentState> source)
        {
            var copy = new HeroEquipmentState[source.Count];
            for (var index = 0; index < copy.Length; index++)
            {
                copy[index] = source[index];
            }

            return copy;
        }

        private static HeroEquipmentState RemoveInstance(HeroEquipmentState equipment, string instanceId)
        {
            if (string.Equals(equipment.WeaponInstanceId, instanceId, StringComparison.Ordinal))
                return equipment.SetInstanceId(EquipmentSlot.Weapon, null);
            if (string.Equals(equipment.ArmorInstanceId, instanceId, StringComparison.Ordinal))
                return equipment.SetInstanceId(EquipmentSlot.Armor, null);
            if (string.Equals(equipment.RelicInstanceId, instanceId, StringComparison.Ordinal))
                return equipment.SetInstanceId(EquipmentSlot.Relic, null);
            return equipment;
        }
    }
}
