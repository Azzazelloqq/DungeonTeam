using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace DungeonTeam.Gameplay.Rewards.Runtime
{
    public interface IRewardPickupViewLoader
    {
        UniTask<RewardPickupViewSet> LoadAsync(
            IReadOnlyList<string> rewardIds,
            CancellationToken token);
    }
}
