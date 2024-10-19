using Code.DungeonTeam.CharacterSkill.Skills.InstantHealSkill.Base;
using Code.Timer;

namespace Code.DungeonTeam.CharacterSkill.Skills.InstantHealSkill
{
public class InstantHealModel : InstantHealSkillModelBase
{
    private readonly float _chargeTime;
    private readonly float _cooldownTime;
    public override int HealPoints { get; }
    public override bool IsCanActivate => !IsCharging && !IsOnCooldown;
    public override bool IsCharging => _chargeSkillTimer.IsInProgress;
    public override bool IsOnCooldown => _cooldownSkillTimer.IsInProgress;
    
    private readonly ActionTimer _chargeSkillTimer = new();
    private readonly ActionTimer _cooldownSkillTimer = new();

    public InstantHealModel(int healPoints, float chargeTime, float cooldownTime)
    {
        HealPoints = healPoints;
        _chargeTime = chargeTime;
        _cooldownTime = cooldownTime;
    }
    
    public InstantHealModel(int healPoints)
    {
        HealPoints = healPoints;
    }

    public override void StartChargeSkill()
    {
        if (!IsCanActivate)
        {
            return;
        }
        
        _chargeSkillTimer.StartTimer(_chargeTime);
    }

    public override void Activate()
    {
        if (!IsCanActivate)
        {
            return;
        }
        
        _cooldownSkillTimer.StartTimer(_cooldownTime);
    }

    public override void CancelActivateSkill()
    {
        _chargeSkillTimer.StopTimer();
        _cooldownSkillTimer.StopTimer();
    }
}
}