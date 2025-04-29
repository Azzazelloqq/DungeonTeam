using System;
using Code.Skills.CharacterSkill.Core.EffectsCore.Base;
using Code.Skills.CharacterSkill.Core.SkillAffectable.Base;

namespace Code.Skills.CharacterSkill.Core.Skills.Base
{
public interface ISkill : IDisposable
{
	public event Action ChargeCompleted;

	public string SkillId { get; }
	public bool IsReadyToActivate { get; }
	public bool IsCharging { get; }
	public bool IsOnCooldown { get; }

	public void StartChargeSkill();
	public void Activate(ISkillAffectable skillAffectable);
	public void AddEffect(ISkillEffect skillEffect);
}
}