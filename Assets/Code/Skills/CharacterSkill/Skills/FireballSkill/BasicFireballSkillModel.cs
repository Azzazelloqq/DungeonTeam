using Code.Skills.CharacterSkill.Skills.FireballSkill.Base;
using Code.Utils.ModelUtils;

namespace Code.Skills.CharacterSkill.Skills.FireballSkill
{
public class BasicFireballSkillModel : FireballSkillModelBase
{
	public override string SkillId { get; }
	public override bool IsCharging { get; protected set; }

	public BasicFireballSkillModel(string skillId)
	{
        SkillId = skillId;
	}

	public override void Dispose()
	{
		base.Dispose();
	}

	public override void ChargeSkill()
	{
		IsCharging = true;
	}
}
}