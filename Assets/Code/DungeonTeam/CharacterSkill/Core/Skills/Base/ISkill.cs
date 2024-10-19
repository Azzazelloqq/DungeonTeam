using System;
using Code.DungeonTeam.CharacterSkill.Core.SkillAffectable.Base;

namespace Code.DungeonTeam.CharacterSkill.Core.Skills.Base
{
public interface ISkill<in TSkillAffectable> where TSkillAffectable : ISkillAffectable
{
    public event Action ChargeCompleted;
	public string Name { get; }
    public void StartChargeSkill(TSkillAffectable skillAttackable);
	public void Activate(TSkillAffectable skillAffectable);
    public void CancelActivateSkill();
}
}