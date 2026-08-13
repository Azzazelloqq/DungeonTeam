using MVP;
using UnityEngine;

namespace DungeonTeam.Gameplay.Skills.Runtime.Presentation.Gameplay.SkillProjectile.Base
{
    public abstract class SkillProjectileViewBase : ViewMonoBehaviour<SkillProjectilePresenterBase>
    {
        public abstract Vector3 Position { get; set; }
        public abstract Quaternion Rotation { get; set; }
    }
}
