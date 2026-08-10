using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace DungeonTeam.Gameplay.Skills.Runtime
{
    public interface ISkillViewLoader
    {
        UniTask<SkillViewSet> LoadAsync(
            IReadOnlyList<string> loadoutIds,
            CancellationToken token);
    }
}
