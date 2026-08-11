using System;
using DungeonTeam.Gameplay.Hero.Runtime;

namespace DungeonTeam.Gameplay.DungeonRun.Runtime
{
    public interface IDungeonRunInput : IHeroInput, IDisposable
    {
        void Enable();
    }
}
