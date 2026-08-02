using System;
using Code.Addressables.Generated;

namespace DungeonTeam.Gameplay.Dungeon.Runtime.Infrastructure
{
    internal static class DungeonMapAssetCatalog
    {
        public static string ResolveAddress(string mapAssetId)
        {
            return mapAssetId switch
            {
                "map.authored.demo" =>
                    AddressableIds.Dungeons.DungeonMapsAuthoredDungeonDemo,
                _ => throw new InvalidOperationException(
                    $"Dungeon map asset ID '{mapAssetId}' is not registered.")
            };
        }
    }
}
