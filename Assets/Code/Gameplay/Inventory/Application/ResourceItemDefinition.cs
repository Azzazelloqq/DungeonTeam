using System;

namespace DungeonTeam.Gameplay.Inventory.Application
{
    public sealed class ResourceItemDefinition
    {
        public ResourceItemDefinition(string definitionId, string displayName, int saleValue)
        {
            DefinitionId = !string.IsNullOrWhiteSpace(definitionId)
                ? definitionId
                : throw new ArgumentException("Definition ID cannot be empty.", nameof(definitionId));
            DisplayName = !string.IsNullOrWhiteSpace(displayName)
                ? displayName
                : throw new ArgumentException("Display name cannot be empty.", nameof(displayName));
            SaleValue = saleValue >= 0 ? saleValue : throw new ArgumentOutOfRangeException(nameof(saleValue));
        }

        public string DefinitionId { get; }
        public string DisplayName { get; }
        public int SaleValue { get; }
    }
}
