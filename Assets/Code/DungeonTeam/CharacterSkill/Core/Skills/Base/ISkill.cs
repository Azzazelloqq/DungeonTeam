using System;
using Code.DungeonTeam.CharacterSkill.Core.SkillAffectable.Base;

namespace Code.DungeonTeam.CharacterSkill.Core.Skills.Base
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