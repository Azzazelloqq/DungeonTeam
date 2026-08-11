using DungeonTeam.Gameplay.Skills.Domain;
using UnityEngine;

namespace DungeonTeam.Gameplay.Hero.Runtime
{
    public interface IHeroInput
    {
        Vector2 Movement { get; }

        bool TryConsumeTargetSelection(out Vector2 screenPosition);

        bool TryConsumeSkillRequest(out SkillSlot slot);
    }
}
