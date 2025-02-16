using Code.Skills.CharacterSkill.SkillPresenters.FireballSkill.Fireball.BaseMVP;
using MVP;
using UnityEngine;

namespace Code.Skills.CharacterSkill.SkillPresenters.Base
{
public abstract class SkillViewBase : ViewMonoBehaviour<SkillPresenterBase>
{
	public abstract void ActivateSkill();
	public abstract Vector3 Position { get; }
	public abstract float FireballSpeed { get; protected set; }
	public abstract void OnTargetReached();
	public abstract FireballViewBase CreateFireballView();
	public abstract void ChargeSkill();
    public abstract void CancelChargeSkill();
	public abstract void OnChargeCompleted();
}
}