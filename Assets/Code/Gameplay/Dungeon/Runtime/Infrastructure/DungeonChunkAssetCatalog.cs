using System;
using Code.Addressables.Generated;

namespace DungeonTeam.Gameplay.Dungeon.Runtime.Infrastructure
{
    internal static class DungeonChunkAssetCatalog
    {
        public static string ResolveAddress(string chunkAssetId)
        {
            return chunkAssetId switch
            {
                "chunk.demo.entry" => AddressableIds.Dungeons.DungeonChunksEntry,
                "chunk.demo.exit" => AddressableIds.Dungeons.DungeonChunksExit,
                "chunk.demo.mandatory" => AddressableIds.Dungeons.DungeonChunksMandatory,
                "chunk.demo.room" => AddressableIds.Dungeons.DungeonChunksRoom,
                _ => throw new InvalidOperationException(
                    $"Dungeon chunk asset ID '{chunkAssetId}' is not registered.")
            };
        }
    }
}
