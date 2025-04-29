using Code.Skills.CharacterSkill.SkillPresenters.Base;

namespace Code.Skills.CharacterSkill.SkillPresenters.FireballSkill
{
public class BasicFireballSkillModel : SkillModelBase
{
	public override string SkillId { get; }
	public override bool IsCharging { get; protected set; }
	public override bool IsCasting { get; protected set; }

	public BasicFireballSkillModel(string skillId)
	{
		SkillId = skillId;
	}

	protected override void OnDispose()
	{
		base.OnDispose();
	}

	public override void ChargeSkill()
	{
		IsCharging = true;
	}

	public override void ActivateSkill()
	{
		if (IsCharging)
		{
			return;
		}

		IsCasting = true;
	}

	public override void OnChargeCompleted()
	{
		IsCharging = false;
	}

	public override void OnCastCompleted()
	{
		IsCasting = true;
	}
}
}