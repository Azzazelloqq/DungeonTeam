using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace DungeonTeam.Gameplay.Actors.Runtime
{
    public interface IActorDefinitionLoader
    {
        UniTask<ActorDefinitionSet> LoadAsync(
            IReadOnlyList<string> actorIds,
            CancellationToken token);
    }
}
