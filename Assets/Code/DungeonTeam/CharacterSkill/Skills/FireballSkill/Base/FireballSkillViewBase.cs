using Code.DungeonTeam.CharacterSkill.Skills.FireballSkill.Fireball.BaseMVP;
using MVP;
using UnityEngine;

namespace Code.DungeonTeam.CharacterSkill.Skills.FireballSkill.Base
{
public abstract class FireballSkillViewBase : ViewMonoBehaviour<FireballSkillPresenterBase>
{
	public abstract void ActivateSkill();
	public abstract Vector3 Position { get; }
	public abstract void OnTargetReached();
	public abstract FireballViewBase CreateFireballView();
	public abstract void ChargeSkill();
}
}