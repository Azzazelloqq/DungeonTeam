using Code.Skills.CharacterSkill.Skills.FireballSkill.Fireball.BaseMVP;
using MVP;
using UnityEngine;

namespace Code.Skills.CharacterSkill.Skills.FireballSkill.Base
{
public abstract class FireballSkillViewBase : ViewMonoBehaviour<FireballSkillPresenterBase>
{
	public abstract void ActivateSkill();
	public abstract Vector3 Position { get; }
	public abstract float FireballSpeed { get; protected set; }
	public abstract void OnTargetReached();
	public abstract FireballViewBase CreateFireballView();
	public abstract void ChargeSkill();
    public abstract void CancelChargeSkill();
}
}