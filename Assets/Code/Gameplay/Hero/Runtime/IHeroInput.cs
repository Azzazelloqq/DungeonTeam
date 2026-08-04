using UnityEngine;

namespace DungeonTeam.Gameplay.Hero.Runtime
{
    public interface IHeroInput
    {
        Vector2 Movement { get; }

        bool TargetSelectionWasPressed { get; }

        Vector2 PointerPosition { get; }

        bool BasicAttackWasPressed { get; }
    }
}
