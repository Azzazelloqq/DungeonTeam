using System;
using Code.Addressables.Generated;

namespace DungeonTeam.Gameplay.Chests.Runtime
{
    internal static class ChestViewAssetCatalog
    {
        public static bool TryResolveAddress(string chestId, out string address)
        {
            switch (chestId)
            {
                case "interest.chest.basic":
                    address = AddressableIds.Chests.ChestsCommonChest;
                    return true;
                default:
                    address = null;
                    return false;
            }
        }

        public static string ResolveAddress(string chestId)
        {
            return TryResolveAddress(chestId, out var address)
                ? address
                : throw new InvalidOperationException(
                    $"Chest view for ID '{chestId}' is not registered.");
        }
    }
}
