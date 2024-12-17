using System;
using System.Threading;
using System.Threading.Tasks;
using Code.Config;
using Code.GameConfig.ScriptableObjectParser.ConfigData.Skills;
using Code.Generated.Addressables;
using Code.SavesContainers.TeamSave;
using Code.Skills.CharacterSkill.Core.SkillAffectable.Base;
using Code.Skills.CharacterSkill.Core.Skills.Base;
using Code.Skills.CharacterSkill.Skills.FireballSkill;
using Code.Utils.AsyncUtils;
using InGameLogger;
using LocalSaveSystem;
using ResourceLoader;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Code.Skills.CharacterSkill.Factory
{
public class SkillsFactory : ISkillsFactory
{
	private readonly IResourceLoader _resourceLoader;
	private readonly IInGameLogger _logger;
	private readonly IConfig _config;
	private readonly ILocalSaveSystem _saveSystem;
	private readonly SkillDependencies _skillDependencies;
	private readonly PlayerTeamSave _playerTeamSave;
	private readonly SkillsConfigPage _skillsConfigPage;

	public SkillsFactory(
		SkillDependencies skillDependencies,
		IResourceLoader resourceLoader,
		IInGameLogger logger,
		IConfig config,
		ILocalSaveSystem saveSystem)
	{
		_skillDependencies = skillDependencies;
		_resourceLoader = resourceLoader;
		_logger = logger;
		_config = config;
		_saveSystem = saveSystem;
		_playerTeamSave = _saveSystem.Load<PlayerTeamSave>();
		_skillsConfigPage = _config.GetConfigPage<SkillsConfigPage>();
	}

	public async Task<TSkill> GetSkillAsync<TSkill>(string skillId, Transform parent, CancellationToken token)
	{
		if (typeof(TSkill) == typeof(FireballSkillPresenter))
		{
		}

		return default;
	}

	public void GetSkill<TSkill, TAffectable>(Transform parent, Action<TSkill> onSkillLoaded, CancellationToken token)
	{
	}

	private async Task<FireballSkillPresenter> GetCharacterGetBasicFireballSkill(
		string characterId,
		string skillId,
		Transform parent,
		CancellationToken token)
	{
		var basicFireballResourceId = ResourceIdsContainer.CharacterSkills.BasicFireball;
		var viewPrefab = await _resourceLoader.LoadResourceAsync<BasicFireballSkillView>(basicFireballResourceId, token);
		var instantiateViewAsyncOperation = await Object.InstantiateAsync(viewPrefab, parent).AsTask();
		var basicFireballSkillView = instantiateViewAsyncOperation[0];

		const SkillType skillType = SkillType.Attack;
		var skillStats = GetSkillStatsByCharacterId(characterId, skillType, skillId);
		var fireballData = new FireballData(skillStats.Impact, skillStats.CooldownPerMilliseconds,
			skillStats.ChargeTimePerMilliseconds);

		var model = new BasicFireballSkillModel(skillId);
		var tickHandler = _skillDependencies.TickHandler;

		return new FireballSkillPresenter(basicFireballSkillView, model, tickHandler, _logger);
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