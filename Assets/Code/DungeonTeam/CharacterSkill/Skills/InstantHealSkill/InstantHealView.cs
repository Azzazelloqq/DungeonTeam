using Code.DungeonTeam.CharacterSkill.Skills.InstantHealSkill.Base;
using UnityEngine;

namespace Code.DungeonTeam.CharacterSkill.Skills.InstantHealSkill
{
public class InstantHealView : InstantHealViewBase
{
    [SerializeField] private ParticleSystem _chargeEffect;
    [SerializeField] private ParticleSystem _activateEffect;
    
    public override void StartChargeSkill()
    {
        _chargeEffect.Play();
    }

    public override void CancelActivateSkill()
    {
        _chargeEffect.Stop();
        _activateEffect.Stop();
    }

    public override void ActivateSkill()
    {
        _activateEffect.Play();
    }
}
}