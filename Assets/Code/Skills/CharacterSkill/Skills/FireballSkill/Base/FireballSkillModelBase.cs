using MVP;

namespace Code.Skills.CharacterSkill.Skills.FireballSkill.Base
{
public abstract class FireballSkillModelBase : Model
{
	public abstract string SkillId { get; }
	public abstract bool IsCharging { get; protected set; }
	public abstract void ChargeSkill();
}
}