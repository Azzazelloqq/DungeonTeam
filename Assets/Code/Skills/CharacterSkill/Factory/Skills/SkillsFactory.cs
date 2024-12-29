using Code.GameConfig.ScriptableObjectParser.ConfigData.Skills;
using Code.SavesContainers.TeamSave;
using Code.Skills.CharacterSkill.Core.Skills.DamageSkills;
using Code.Skills.CharacterSkill.Core.Skills.HealSkills;
using Code.Skills.CharacterSkill.Factory.Effects;
using InGameLogger;

namespace Code.Skills.CharacterSkill.Factory.Skills
{
/// <summary>
/// Concrete implementation of <see cref="ISkillsFactory"/> responsible for
/// creating skill instances such as damage or heal skills.
/// </summary>
internal class SkillsFactory : ISkillsFactory
{
	private readonly IInGameLogger _logger;
	private readonly ISkillEffectsFactory _skillEffectsFactory;
	private readonly PlayerTeamSave _playerTeamSave;
	private readonly SkillsConfigPage _skillsConfigPage;
	
	/// <summary>
	/// Initializes a new instance of the <see cref="SkillsFactory"/> class.
	/// </summary>
	/// <param name="logger">A logger to record skill-related messages.</param>
	/// <param name="skillEffectsFactory">Factory to create skill effects (e.g., damage, heal, buffs).</param>
	/// <param name="playerTeamSave">Contains data about the player's team composition and skill levels.</param>
	/// <param name="skillsConfigPage">A configuration page holding skill configuration data.</param>
	public SkillsFactory(
		IInGameLogger logger,
		ISkillEffectsFactory skillEffectsFactory,
		PlayerTeamSave playerTeamSave,
		SkillsConfigPage skillsConfigPage)
	{
		_logger = logger;
		_skillEffectsFactory = skillEffectsFactory;
		_playerTeamSave = playerTeamSave;
		_skillsConfigPage = skillsConfigPage;
	}

	/// <inheritdoc />
	/// <remarks>
	/// Depending on <typeparamref name="TSkill"/>, different skill types are created:
	/// <list type="bullet">
	/// <item>
	/// <description><see cref="GenericHealSkill"/> for healing abilities.</description>
	/// </item>
	/// <item>
	/// <description><see cref="GenericDamageSkill"/> for damage abilities.</description>
	/// </item>
	/// </list>
	/// If the type does not match a known skill type, an error is logged and the default value is returned.
	/// </remarks>
	TSkill ISkillsFactory.GetSkill<TSkill>(string skillId, string characterId)
	{
		var skillType = typeof(TSkill);
		
		if (typeof(GenericHealSkill) == skillType)
		{
			var skillConfig = GetSkillStatsByCharacterId(characterId, SkillType.Heal, skillId);
			var effects = _skillEffectsFactory.GetSkillEffects(skillConfig);
			var cooldownPerMilliseconds = skillConfig.CooldownPerMilliseconds;
			var chargeTimePerMilliseconds = skillConfig.ChargeTimePerMilliseconds;
			var genericHealSkill = new GenericHealSkill(_logger, effects, skillId, chargeTimePerMilliseconds,
				cooldownPerMilliseconds);
			
			if (genericHealSkill is TSkill skill)
			{
				return skill;
			}

			_logger.LogError($"Skill {skillType.FullName} does not have implementation");
			return default;
		}

		if(typeof(GenericDamageSkill) == skillType)
		{
			var skillConfig = GetSkillStatsByCharacterId(characterId, SkillType.Heal, skillId);
			var effects = _skillEffectsFactory.GetSkillEffects(skillConfig);
			var cooldownPerMilliseconds = skillConfig.CooldownPerMilliseconds;
			var chargeTimePerMilliseconds = skillConfig.ChargeTimePerMilliseconds;
			var genericDamageSkill = new GenericDamageSkill(_logger, effects, skillId, chargeTimePerMilliseconds,
				cooldownPerMilliseconds);
			
			if (genericDamageSkill is TSkill skill)
			{
				return skill;
			}

			_logger.LogError($"Skill {skillType.FullName} does not have implementation");
			
			return default;
		}

		_logger.LogError($"Skill {skillType.FullName} does not have implementation");
		
		return default;
	}

	private SkillStatsConfig GetSkillStatsByCharacterId(string characterId, SkillType skillType, string skillId)
	{
		var characterSkillSave = GetCharacterSkillSave(characterId, skillId);
		var skillLevel = characterSkillSave.SkillLevel;
		
		if (skillLevel < 0)
		{
			_logger.LogError("Skill with id {0} not found");
			return default;
		}

		var skillsGroup = _skillsConfigPage.SkillsGroups[skillType];
		var skill = skillsGroup.Skills[skillId];
		var skillImpactConfig = skill.ImpactByLevel[skillLevel];

		return skillImpactConfig;
	}

	private CharacterSkillSave GetCharacterSkillSave(string characterId, string skillId)
	{
		if (!TryGetCharacterSave(characterId, out var characterSave))
		{
			_logger.LogError("Character with id {0} not found");
			return default;
		}
		
		foreach (var characterSaveSkill in characterSave.Skills)
		{
			if (characterSaveSkill.Id != skillId)
			{
				continue;
			}

			return characterSaveSkill;
		}
		
		_logger.LogError("Skill with id {0} not found");

		return default;
	}

	private bool TryGetCharacterSave(string characterId, out CharacterSave character)
	{
		foreach (var characterSave in _playerTeamSave.SelectedPlayerTeam)
		{
			if (characterSave.Id != characterId)
			{
				continue;
			}

			character = characterSave;
			return true;
		}

		character = default;
		return false;
	}
}
}