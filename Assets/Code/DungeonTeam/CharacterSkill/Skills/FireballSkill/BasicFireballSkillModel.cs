using System;
using Code.DungeonTeam.CharacterSkill.Skills.FireballSkill.Base;
using Code.Timer;

namespace Code.DungeonTeam.CharacterSkill.Skills.FireballSkill
{
public class BasicFireballSkillModel : FireballSkillModelBase
{
	public override float FireballSpeed => _fireballData.FireballSpeed;
	public override float FireballDamage => _fireballData.Damage;
	public override bool IsReadyToActivate { get; protected set; }
	public override string SkillName { get; }

	private FireballData _fireballData;
	private readonly ActionTimer _chargeSkillTime = new();

	protected BasicFireballSkillModel(string skillName, FireballData fireballData)
	{
		_fireballData = fireballData;
		SkillName = skillName;
	}

	public override void Dispose()
	{
		base.Dispose();
		_chargeSkillTime.Dispose();
	}

	public override void UpdateFireballData(FireballData fireballData)
	{
		_fireballData = fireballData;
	}

	public override void ChargeSkill(Action onChargeCompleted)
	{
		var chargeTime = _fireballData.ChargeTimeMilliseconds;
		_chargeSkillTime.StartTimer(chargeTime, onChargeCompleted);
	}
	
	public override void ActivateSkill()
	{
		
	}
}
}