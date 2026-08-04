using System;
using DungeonTeam.Gameplay.Hero.Runtime;
using DungeonTeam.Gameplay.Team.Runtime;

namespace DungeonTeam.Gameplay.DungeonRun.Runtime
{
    public interface IDungeonRunInput : IHeroInput, ITeamCameraInput, IDisposable
    {
        void Enable();
    }
}
