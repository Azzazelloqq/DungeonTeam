using System;

namespace DungeonTeam.Gameplay.Inventory.Domain
{
    public readonly struct ResourceStackState
    {
        public ResourceStackState(string definitionId, int quantity)
        {
            DefinitionId = InventoryValidation.RequireId(definitionId, nameof(definitionId));
            Quantity = quantity > 0
                ? quantity
                : throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        public string DefinitionId { get; }
        public int Quantity { get; }
    }
}
