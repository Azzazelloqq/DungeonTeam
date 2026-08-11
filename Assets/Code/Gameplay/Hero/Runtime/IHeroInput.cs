using DungeonTeam.Gameplay.Skills.Domain;
using UnityEngine;

namespace DungeonTeam.Gameplay.Hero.Runtime
{
    public interface IHeroInput
    {
        Vector2 Movement { get; }

        bool TryConsumeSkillRequest(out SkillSlot slot);
    }
}
