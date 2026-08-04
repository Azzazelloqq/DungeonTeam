using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace DungeonTeam.Gameplay.Chests.Runtime
{
    public interface IChestViewLoader
    {
        bool Supports(string chestId);

        UniTask<ChestViewSet> LoadAsync(
            IReadOnlyList<string> chestIds,
            CancellationToken token);
    }
}
