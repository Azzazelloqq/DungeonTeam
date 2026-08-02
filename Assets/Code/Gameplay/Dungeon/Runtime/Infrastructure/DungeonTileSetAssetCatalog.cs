using System;
using Code.Addressables.Generated;

namespace DungeonTeam.Gameplay.Dungeon.Runtime.Infrastructure
{
    internal static class DungeonTileSetAssetCatalog
    {
        public static string ResolveAddress(string tileSetId)
        {
            return tileSetId switch
            {
                "tileset.demo.procedural" =>
                    AddressableIds.Dungeons.DungeonTileSetsDemo,
                _ => throw new InvalidOperationException(
                    $"Dungeon tile set ID '{tileSetId}' is not registered.")
            };
        }
    }
}
