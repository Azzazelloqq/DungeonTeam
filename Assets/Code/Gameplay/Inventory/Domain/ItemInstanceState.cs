using System;

namespace DungeonTeam.Gameplay.Inventory.Domain
{
    public readonly struct ItemInstanceState
    {
        public ItemInstanceState(string instanceId, string definitionId)
        {
            InstanceId = InventoryValidation.RequireId(instanceId, nameof(instanceId));
            DefinitionId = InventoryValidation.RequireId(definitionId, nameof(definitionId));
        }

        public string InstanceId { get; }
        public string DefinitionId { get; }
    }
}
