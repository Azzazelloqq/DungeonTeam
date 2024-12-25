using System.Threading;
using System.Threading.Tasks;
using Code.Config;
using Code.GameConfig.ScriptableObjectParser.ConfigData.Skills;
using Code.Generated.Addressables;
using Code.SavesContainers.TeamSave;
using Code.Skills.CharacterSkill.Core.Skills.Base;
using Code.Skills.CharacterSkill.Core.Skills.DamageSkills;
using Code.Skills.CharacterSkill.Core.Skills.HealSkills;
using Code.Skills.CharacterSkill.Factory.Effects;
using Code.Skills.CharacterSkill.Factory.Skills;
using Code.Skills.CharacterSkill.Skills.FireballSkill;
using Code.Utils.AsyncUtils;
using InGameLogger;
using LocalSaveSystem;
using ResourceLoader;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Code.Skills.CharacterSkill.Factory.SkillsPresenter
{
public class SkillsPresenterFactory : ISkillsPresenterFactory
{
	private readonly IResourceLoader _resourceLoader;
	private readonly IInGameLogger _logger;
	private readonly SkillDependencies _skillDependencies;
	private readonly ISkillsFactory _skillsFactory;

	public SkillsPresenterFactory(
		SkillDependencies skillDependencies,
		IResourceLoader resourceLoader,
		IInGameLogger logger,
		IConfig config,
		ILocalSaveSystem saveSystem)
	{
		_skillDependencies = skillDependencies;
		_resourceLoader = resourceLoader;
		_logger = logger;
		var playerTeamSave = saveSystem.Load<PlayerTeamSave>();
		var skillsConfigPage = config.GetConfigPage<SkillsConfigPage>();
		var skillEffectsFactory = new SkillEffectsFactory(logger);

		_skillsFactory = new SkillsFactory(logger, skillEffectsFactory, playerTeamSave, skillsConfigPage);
	}

	public async Task<TPresenter> GetAsync<TPresenter>(
		string characterId,
		string skillId,
		Transform parent,
		CancellationToken token)
	{
		if(typeof(TPresenter) == typeof(BasicFireballSkillPresenter))
		{
			return (TPresenter)(object)await GetCharacterGetBasicFireballSkill(
				characterId,
				skillId,
				parent,
				token);
		}
		
		_logger.LogError($"Skill presenter with type {0} does not have implementation + {typeof(TPresenter).FullName}");
		return default;
	}

	private async Task<BasicFireballSkillPresenter> GetCharacterGetBasicFireballSkill(
		string characterId,
		string skillId,
		Transform parent,
		CancellationToken token)
	{
		var basicFireballResourceId = ResourceIdsContainer.CharacterSkills.BasicFireball;
		var viewPrefab = await _resourceLoader.LoadResourceAsync<BasicSkillView>(basicFireballResourceId, token);
		var instantiateViewAsyncOperation = await Object.InstantiateAsync(viewPrefab, parent).AsTask();
		var basicFireballSkillView = instantiateViewAsyncOperation[0];

		var skill = _skillsFactory.GetSkill<GenericDamageSkill>(skillId, characterId);
		var model = new BasicFireballSkillModel(skillId);
		var tickHandler = _skillDependencies.TickHandler;
		var movementService = _skillDependencies.MovementService;
		
		return new BasicFireballSkillPresenter(basicFireballSkillView, model, tickHandler, _logger, skill, movementService);
	}
	
	
}
}