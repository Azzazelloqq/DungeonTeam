using MVP;

namespace Code.DungeonTeam.CharacterSkill.Skills.InstantHealSkill.Base
{
public abstract class InstantHealSkillModelBase : Model
{
	public abstract int HealPoints { get; }
    public abstract bool IsCanActivate { get; }
    public abstract bool IsCharging { get; }
    public abstract bool IsOnCooldown { get; }
    public abstract void StartChargeSkill();
    public abstract void Activate();
    public abstract void CancelActivateSkill();
}
}