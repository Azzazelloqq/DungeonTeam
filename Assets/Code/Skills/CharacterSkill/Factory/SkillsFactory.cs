using System;
using System.Threading;
using System.Threading.Tasks;
using Code.Config;
using Code.GameConfig.ScriptableObjectParser.ConfigData.Skills;
using Code.Generated.Addressables;
using Code.Skills.CharacterSkill.Core.SkillAffectable.Base;
using Code.Skills.CharacterSkill.Core.Skills.Base;
using Code.Skills.CharacterSkill.Skills.FireballSkill;
using Code.Utils.AsyncUtils;
using InGameLogger;
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
	private readonly SkillDependencies _skillDependencies;

	public SkillsFactory(SkillDependencies skillDependencies, IResourceLoader resourceLoader, IInGameLogger logger, IConfig config)
	{
		_skillDependencies = skillDependencies;
		_resourceLoader = resourceLoader;
		_logger = logger;
		_config = config;
	}

	public async Task<TSkill> GetSkillAsync<TSkill, TAffectable>(string skillId, Transform parent, CancellationToken token) 
		where TSkill : ISkill<TAffectable>
		where TAffectable : ISkillAffectable
	{
		if (typeof(TSkill) == typeof(BasicFireballSkillPresenter))
		{
			
		}

		return default;
	}

	public void GetSkill<TSkill, TAffectable>(Transform parent, Action<TSkill> onSkillLoaded, CancellationToken token)
	{
		
	} 

	private async Task<BasicFireballSkillPresenter> GetBasicFireballSkill(string skillId, Transform parent, CancellationToken token)
	{
		var basicFireballResourceId = ResourceIdsContainer.CharacterSkills.BasicFireball;
		var viewPrefab = await _resourceLoader.LoadResourceAsync<BasicFireballSkillView>(basicFireballResourceId, token);
		var basicFireballSkillViewPrefab = Object.Instantiate(viewPrefab, parent);
		var instantiateViewAsyncOperation = await Object.InstantiateAsync(viewPrefab, parent).AsTask();
		var basicFireballSkillView = instantiateViewAsyncOperation[0];

		return null;
		//покумекай над конфигом, я думаю, что скорость файрбола - вьюшная тема
		// var skillConfig = GetSkillConfig(SkillType.Attack, skillId);
		// new BasicFireballSkillModel()
		// var tickHandler = _skillDependencies.TickHandler;
		// return new BasicFireballSkillPresenter();
	}

	private SkillConfig GetSkillConfig(SkillType skillType, string skillId)
	{
		var skillsConfigPage = _config.GetConfigPage<SkillsConfigPage>();
		var skillsGroupConfig = skillsConfigPage.SkillsGroups[skillType];
		var skillConfig = skillsGroupConfig.Skills[skillId];

		return skillConfig;
	}
}
}