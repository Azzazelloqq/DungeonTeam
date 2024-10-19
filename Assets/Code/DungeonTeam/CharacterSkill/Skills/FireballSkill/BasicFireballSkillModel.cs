using System;
using Code.DungeonTeam.CharacterSkill.Skills.FireballSkill.Base;
using Code.Timer;

namespace Code.DungeonTeam.CharacterSkill.Skills.FireballSkill
{
public class BasicFireballSkillModel : FireballSkillModelBase
{
	public override float FireballSpeed => _fireballData.FireballSpeed;
	public override float FireballDamage => _fireballData.Damage;
    public override bool IsReadyToActivate => !_cooldownSkillTimer.IsInProgress && !_chargeSkillTimer.IsInProgress;
	public override string SkillName { get; }

	private FireballData _fireballData;
    private readonly float _skillCooldown;
    private readonly ActionTimer _chargeSkillTimer = new();
    private readonly ActionTimer _cooldownSkillTimer = new();

	protected BasicFireballSkillModel(string skillName, FireballData fireballData, float skillCooldown)
	{
		_fireballData = fireballData;
        _skillCooldown = skillCooldown;
        SkillName = skillName;
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
        
        _cooldownSkillTimer.StartTimer(_skillCooldown);
	}
}
}