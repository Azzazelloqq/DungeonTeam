using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DungeonTeam.Gameplay.Inventory.Domain;

namespace DungeonTeam.Gameplay.Inventory.Application
{
    public sealed class ItemCatalog
    {
        public const string TrainingBladeDefinitionId = "equipment.training-blade";
        public const string WardenCoatDefinitionId = "equipment.warden-coat";
        public const string PathfinderCharmDefinitionId = "equipment.pathfinder-charm";
        public const string MonsterCrystalDefinitionId = "resource.monster-crystal";
        public const string TrainingBladeInstanceId = "starter.training-blade";
        public const string WardenCoatInstanceId = "starter.warden-coat";
        public const string PathfinderCharmInstanceId = "starter.pathfinder-charm";

        private readonly Dictionary<string, EquipmentItemDefinition> _equipment;
        private readonly Dictionary<string, ResourceItemDefinition> _resources;
        private readonly ReadOnlyCollection<EquipmentItemDefinition> _allEquipment;
        private readonly ReadOnlyCollection<ResourceItemDefinition> _allResources;

        public ItemCatalog(
            IReadOnlyList<EquipmentItemDefinition> equipment,
            IReadOnlyList<ResourceItemDefinition> resources,
            IReadOnlyCollection<string> knownActorIds = null)
        {
            if (equipment == null)
            {
                throw new ArgumentNullException(nameof(equipment));
            }

            if (resources == null)
            {
                throw new ArgumentNullException(nameof(resources));
            }

            _equipment = new Dictionary<string, EquipmentItemDefinition>(StringComparer.Ordinal);
            var equipmentCopy = new EquipmentItemDefinition[equipment.Count];
            for (var index = 0; index < equipmentCopy.Length; index++)
            {
                var definition = equipment[index] ?? throw new ArgumentException(
                    $"Equipment definition at index {index} is missing.", nameof(equipment));
                ValidateEquipment(definition, knownActorIds);
                if (!_equipment.TryAdd(definition.DefinitionId, definition))
                {
                    throw new ArgumentException(
                        $"Item definition '{definition.DefinitionId}' is duplicated.", nameof(equipment));
                }

                equipmentCopy[index] = definition;
            }

            _resources = new Dictionary<string, ResourceItemDefinition>(StringComparer.Ordinal);
            var resourceCopy = new ResourceItemDefinition[resources.Count];
            for (var index = 0; index < resourceCopy.Length; index++)
            {
                var definition = resources[index] ?? throw new ArgumentException(
                    $"Resource definition at index {index} is missing.", nameof(resources));
                if (!_resources.TryAdd(definition.DefinitionId, definition) || _equipment.ContainsKey(definition.DefinitionId))
                {
                    throw new ArgumentException(
                        $"Item definition '{definition.DefinitionId}' is duplicated.", nameof(resources));
                }

                resourceCopy[index] = definition;
            }

            _allEquipment = Array.AsReadOnly(equipmentCopy);
            _allResources = Array.AsReadOnly(resourceCopy);
        }

        public IReadOnlyList<EquipmentItemDefinition> Equipment => _allEquipment;
        public IReadOnlyList<ResourceItemDefinition> Resources => _allResources;

        public EquipmentItemDefinition RequireEquipment(string definitionId)
        {
            if (!_equipment.TryGetValue(definitionId, out var definition))
            {
                throw new InvalidOperationException($"Equipment definition '{definitionId}' is not configured.");
            }

            return definition;
        }

        public ResourceItemDefinition RequireResource(string definitionId)
        {
            if (!_resources.TryGetValue(definitionId, out var definition))
            {
                throw new InvalidOperationException($"Resource definition '{definitionId}' is not configured.");
            }

            return definition;
        }

        public bool TryGetEquipment(string definitionId, out EquipmentItemDefinition definition) =>
            _equipment.TryGetValue(definitionId, out definition);

        public bool TryGetResource(string definitionId, out ResourceItemDefinition definition) =>
            _resources.TryGetValue(definitionId, out definition);

        public InventoryState CreateStarterInventory(IReadOnlyList<string> actorIds)
        {
            if (actorIds == null || actorIds.Count == 0)
            {
                throw new ArgumentException("At least one roster actor is required.", nameof(actorIds));
            }

            var mappings = new HeroEquipmentState[actorIds.Count];
            var uniqueActors = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < mappings.Length; index++)
            {
                if (!uniqueActors.Add(actorIds[index]))
                {
                    throw new ArgumentException("Roster actor IDs must be unique.", nameof(actorIds));
                }

                mappings[index] = new HeroEquipmentState(actorIds[index]);
            }

            return new InventoryState(
                new[]
                {
                    new ItemInstanceState(TrainingBladeInstanceId, TrainingBladeDefinitionId),
                    new ItemInstanceState(WardenCoatInstanceId, WardenCoatDefinitionId),
                    new ItemInstanceState(PathfinderCharmInstanceId, PathfinderCharmDefinitionId)
                },
                Array.Empty<ResourceStackState>(),
                mappings);
        }

        private static void ValidateEquipment(
            EquipmentItemDefinition definition,
            IReadOnlyCollection<string> knownActorIds)
        {
            var expectedSlot = definition.Effect switch
            {
                EquipmentEffectKind.PrimaryDamage => EquipmentSlot.Weapon,
                EquipmentEffectKind.MaximumHealth => EquipmentSlot.Armor,
                EquipmentEffectKind.MovementSpeed => EquipmentSlot.Relic,
                _ => throw new ArgumentOutOfRangeException(nameof(definition), definition.Effect, "Unknown equipment effect.")
            };
            if (definition.Slot != expectedSlot)
            {
                throw new ArgumentException(
                    $"Equipment '{definition.DefinitionId}' effect '{definition.Effect}' is incompatible with slot '{definition.Slot}'.");
            }

            if (knownActorIds == null)
            {
                return;
            }

            foreach (var actorId in definition.EligibleActorIds)
            {
                var isKnown = false;
                foreach (var knownActorId in knownActorIds)
                {
                    if (string.Equals(knownActorId, actorId, StringComparison.Ordinal))
                    {
                        isKnown = true;
                        break;
                    }
                }

                if (!isKnown)
                {
                    throw new ArgumentException(
                        $"Equipment '{definition.DefinitionId}' references unknown actor '{actorId}'.");
                }
            }
        }
    }
}
