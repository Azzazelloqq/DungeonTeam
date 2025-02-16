using MVP;

namespace Code.Skills.CharacterSkill.SkillPresenters.Base
{
public abstract class SkillModelBase : Model
{
	public abstract string SkillId { get; }
	public abstract bool IsCharging { get; protected set; }
	public abstract void ChargeSkill();
	public abstract void ActivateSkill();
	public abstract void OnChargeCompleted();
}
}