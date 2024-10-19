using MVP;

namespace Code.DungeonTeam.CharacterSkill.Skills.InstantHealSkill.Base
{
public abstract class InstantHealViewBase : ViewMonoBehaviour<InstantHealSkillPresenterBase>
{
    public abstract void StartChargeSkill();
    public abstract void CancelActivateSkill();
    public abstract void ActivateSkill();
}
}