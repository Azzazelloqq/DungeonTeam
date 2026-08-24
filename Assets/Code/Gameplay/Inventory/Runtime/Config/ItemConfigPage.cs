using System;
using Code.Configuration;
using DungeonTeam.Gameplay.Inventory.Application;
using DungeonTeam.Gameplay.Inventory.Domain;
using UnityEngine;

namespace DungeonTeam.Gameplay.Inventory.Runtime.Config
{
    [CreateAssetMenu(menuName = "DungeonTeam/Gameplay/Item Config", fileName = "ItemConfig")]
    public sealed class ItemConfigPage : ConfigPage
    {
        [SerializeField]
        private EquipmentItemDefinitionConfig[] _equipment = Array.Empty<EquipmentItemDefinitionConfig>();

        [SerializeField]
        private ResourceItemDefinitionConfig[] _resources = Array.Empty<ResourceItemDefinitionConfig>();

        public ItemCatalog CreateCatalog()
        {
            return CreateCatalog(null);
        }

        public ItemCatalog CreateCatalog(System.Collections.Generic.IReadOnlyCollection<string> knownActorIds)
        {
            var equipment = new EquipmentItemDefinition[_equipment.Length];
            for (var index = 0; index < equipment.Length; index++)
            {
                equipment[index] = (_equipment[index] ?? throw new InvalidOperationException(
                    $"Equipment item at index {index} is missing.")).ToDefinition(index);
            }

            var resources = new ResourceItemDefinition[_resources.Length];
            for (var index = 0; index < resources.Length; index++)
            {
                resources[index] = (_resources[index] ?? throw new InvalidOperationException(
                    $"Resource item at index {index} is missing.")).ToDefinition(index);
            }

            return new ItemCatalog(equipment, resources, knownActorIds);
        }
    }

    [Serializable]
    public sealed class EquipmentItemDefinitionConfig
    {
        [SerializeField] private string _definitionId;
        [SerializeField] private string _displayName;
        [SerializeField, Min(0)] private int _saleValue;
        [SerializeField] private EquipmentSlot _slot;
        [SerializeField] private EquipmentEffectKind _effect;
        [SerializeField, Min(0.01f)] private float _effectValue = 1f;
        [SerializeField] private string[] _eligibleActorIds = Array.Empty<string>();

        public EquipmentItemDefinitionConfig(
            string definitionId,
            string displayName,
            int saleValue,
            EquipmentSlot slot,
            EquipmentEffectKind effect,
            float effectValue,
            string[] eligibleActorIds)
        {
            _definitionId = definitionId;
            _displayName = displayName;
            _saleValue = saleValue;
            _slot = slot;
            _effect = effect;
            _effectValue = effectValue;
            _eligibleActorIds = eligibleActorIds;
        }

        internal EquipmentItemDefinition ToDefinition(int index)
        {
            if (_eligibleActorIds == null)
            {
                throw new ArgumentException($"Equipment item at index {index} has no eligible actors.");
            }

            return new EquipmentItemDefinition(
                _definitionId,
                _displayName,
                _saleValue,
                _slot,
                _effect,
                _effectValue,
                _eligibleActorIds);
        }
    }

    [Serializable]
    public sealed class ResourceItemDefinitionConfig
    {
        [SerializeField] private string _definitionId;
        [SerializeField] private string _displayName;
        [SerializeField, Min(0)] private int _saleValue;

        public ResourceItemDefinitionConfig(string definitionId, string displayName, int saleValue)
        {
            _definitionId = definitionId;
            _displayName = displayName;
            _saleValue = saleValue;
        }

        internal ResourceItemDefinition ToDefinition(int index)
        {
            return new ResourceItemDefinition(_definitionId, _displayName, _saleValue);
        }
    }
}
