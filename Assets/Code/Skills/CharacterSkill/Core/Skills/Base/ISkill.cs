using System;
using Code.Skills.CharacterSkill.Core.SkillAffectable.Base;

namespace Code.Skills.CharacterSkill.Core.Skills.Base
{
public interface ISkill<in TSkillAffectable> where TSkillAffectable : ISkillAffectable
{
	public event Action ChargeCompleted;
	
	public bool IsReadyToActivate { get; }
	
	public string Name { get; }
	public void StartChargeSkill(TSkillAffectable skillAffectable);
	public void Activate(TSkillAffectable skillAffectable);
	public void CancelActivateSkill();
}
}