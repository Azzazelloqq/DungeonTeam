using System;
using Code.Skills.CharacterSkill.Skills.FireballSkill.Base;
using Code.Timer;

namespace Code.Skills.CharacterSkill.Skills.FireballSkill
{
public class BasicFireballSkillModel : FireballSkillModelBase
{
	public override int FireballDamage => _fireballData.Damage;
    public override bool IsReadyToActivate => !_cooldownSkillTimer.IsInProgress && !_chargeSkillTimer.IsInProgress;
	public override string SkillId { get; }

	private FireballData _fireballData;
    private readonly ActionTimer _chargeSkillTimer = new();
    private readonly ActionTimer _cooldownSkillTimer = new();

	public BasicFireballSkillModel(string skillId, FireballData fireballData)
	{
		_fireballData = fireballData;
        SkillId = skillId;
	}

	public override void Dispose()
	{
		base.Dispose();
        
		_chargeSkillTimer.Dispose();
        _cooldownSkillTimer.Dispose();
	}

	public override void UpdateFireballData(FireballData fireballData)
	{
		_fireballData = fireballData;
	}

	public override void ChargeSkill(Action onChargeCompleted)
	{
		var chargeTime = _fireballData.ChargeTimeMilliseconds;
		_chargeSkillTimer.StartTimer(chargeTime, onChargeCompleted);
	}

    public override void CancelChargeSkill()
    {
        if (!_chargeSkillTimer.IsInProgress)
        {
            return;
        }
        
        _chargeSkillTimer.StopTimer();
    }

    public override void ActivateSkill()
	{
        if (_chargeSkillTimer.IsInProgress)
        {
            return;
        }
        
        _cooldownSkillTimer.StartTimer(_fireballData.CooldownPerMilliseconds);
	}
}
}