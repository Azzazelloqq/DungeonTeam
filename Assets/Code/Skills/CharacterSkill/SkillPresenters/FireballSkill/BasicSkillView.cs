using Code.Skills.CharacterSkill.SkillPresenters.Base;
using Code.Skills.CharacterSkill.SkillPresenters.FireballSkill.Fireball.BaseMVP;
using UnityEngine;

namespace Code.Skills.CharacterSkill.SkillPresenters.FireballSkill
{
public class BasicSkillView : SkillViewBase
{
	[field: SerializeField]
	public override float FireballSpeed { get; protected set; }

	[SerializeField]
	private FireballViewBase _fireballViewPrefab;

	[SerializeField]
	private Transform _fireballsParent;

	[SerializeField]
	private ParticleSystem _chargeSkillEffect;
	
	[SerializeField]
	private ParticleSystem _activateSkillEffect;

	public override Vector3 Position => transform.position;

	public override void ActivateSkill()
	{
		_activateSkillEffect.Play();
		presenter.OnActivateSkillAnimationCompleted();
	}

	public override void OnTargetReached()
	{
	}

	public override FireballViewBase CreateFireballView()
	{
		var fireballViewBase = Instantiate(_fireballViewPrefab, _fireballsParent);

		return fireballViewBase;
	}

	public override void ChargeSkill()
	{
		PlayChargeSkillEffect();
	}

	public override void CancelChargeSkill()
	{
		_chargeSkillEffect.Stop();
	}

	public override void OnChargeCompleted()
	{
		_chargeSkillEffect.Stop();
	}

	private void PlayChargeSkillEffect()
	{
		_chargeSkillEffect.Play();
	}
}
}