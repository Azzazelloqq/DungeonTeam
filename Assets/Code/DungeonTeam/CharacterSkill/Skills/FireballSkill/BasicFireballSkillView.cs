using System;
using Code.DungeonTeam.CharacterSkill.Skills.FireballSkill.Base;
using Code.DungeonTeam.CharacterSkill.Skills.FireballSkill.Fireball.BaseMVP;
using UnityEngine;

namespace Code.DungeonTeam.CharacterSkill.Skills.FireballSkill
{
public class BasicFireballSkillView : FireballSkillViewBase
{
	[SerializeField] private FireballViewBase _fireballViewPrefab;
	[SerializeField] private Transform _fireballsParent;
	[SerializeField] private ParticleSystem _activateSkillEffect;
	
	public override Vector3 Position => transform.position;

	public override void ActivateSkill()
	{
		
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

	private void PlayChargeSkillEffect()
	{
		
	}
}
}