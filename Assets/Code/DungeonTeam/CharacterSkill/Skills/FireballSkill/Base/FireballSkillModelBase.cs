using System;
using MVP;

namespace Code.DungeonTeam.CharacterSkill.Skills.FireballSkill.Base
{
public abstract class FireballSkillModelBase : Model
{
	public abstract string SkillName { get; }
	public abstract float FireballSpeed { get; }
	public abstract bool IsReadyToActivate { get; }
	public abstract int FireballDamage { get; }

    public abstract void ActivateSkill();
	public abstract void UpdateFireballData(FireballData fireballData);
	public abstract void ChargeSkill(Action onChargeCompleted);
    public abstract void CancelChargeSkill();
}
}