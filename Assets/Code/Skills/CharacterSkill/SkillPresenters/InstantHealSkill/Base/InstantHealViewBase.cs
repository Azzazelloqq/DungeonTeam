using MVP;

namespace Code.Skills.CharacterSkill.Skills.InstantHealSkill.Base
{
public abstract class InstantHealViewBase : ViewMonoBehaviour<GenericHealSkillPresenterBase>
{
    public abstract void StartChargeSkill();
    public abstract void CancelActivateSkill();
    public abstract void ActivateSkill();
}
}