using System;
using System.Collections.Generic;
using Code.Skills.CharacterSkill.SkillPresenters.FireballSkill;

namespace Code.Skills.CharacterSkill.Factory.Container
{
internal class SkillPresentersIdByTypeContainer
{
	private readonly Dictionary<string, Type> _skillsIdByType;

	internal SkillPresentersIdByTypeContainer()
	{
		_skillsIdByType = new Dictionary<string, Type>
		{
			{ "basicFireballAttackSkill", typeof(BasicFireballSkillPresenter) }
		};
	}

	internal Type GetTypeById(string skillId)
	{
		return _skillsIdByType[skillId];
	}
}
}