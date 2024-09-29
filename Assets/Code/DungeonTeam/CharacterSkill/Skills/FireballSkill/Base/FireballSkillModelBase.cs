using System;
using MVP;

namespace Code.DungeonTeam.CharacterSkill.Skills.FireballSkill.Base
{
public abstract class FireballSkillModelBase : Model
{
	public abstract string SkillName { get; }
	public abstract float FireballSpeed { get; }
	public abstract bool IsReadyToActivate { get; protected set; }
	public abstract float FireballDamage { get; }
	
	public abstract void ActivateSkill();
	public abstract void UpdateFireballData(FireballData fireballData);
	public abstract void ChargeSkill(Action onChargeCompleted);
}
}