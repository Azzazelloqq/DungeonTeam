using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DungeonTeam.Gameplay.Dungeon.Domain;

namespace DungeonTeam.Gameplay.Dungeon.Application
{
    public interface IDungeonFactory
    {
        UniTask<IDungeonInstance> CreateAsync(
            DungeonBuildRequest request,
            CancellationToken ownerToken);
    }

    public interface IDungeonInstance : IDisposable
    {
        DungeonMapSnapshot MapSnapshot { get; }
        DungeonContentPlan ContentPlan { get; }
    }
}
