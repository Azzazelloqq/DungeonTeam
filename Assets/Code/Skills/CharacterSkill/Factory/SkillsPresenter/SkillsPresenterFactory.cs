using System;
using System.Threading;
using System.Threading.Tasks;
using Code.Config;
using Code.GameConfig.ScriptableObjectParser.ConfigData.Skills;
using Code.Generated.Addressables;
using Code.SavesContainers.TeamSave;
using Code.Skills.CharacterSkill.Core.Skills.DamageSkills;
using Code.Skills.CharacterSkill.Factory.Container;
using Code.Skills.CharacterSkill.Factory.Effects;
using Code.Skills.CharacterSkill.Factory.Skills;
using Code.Skills.CharacterSkill.SkillPresenters.Base;
using Code.Skills.CharacterSkill.SkillPresenters.FireballSkill;
using Code.Utils.AsyncUtils;
using InGameLogger;
using LocalSaveSystem;
using MVP;
using ResourceLoader;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Code.Skills.CharacterSkill.Factory.SkillsPresenter
{
/// <summary>
/// Concrete implementation of <see cref="ISkillsPresenterFactory"/> that creates skill presenters.
/// Handles both asynchronous and synchronous skill presenter instantiation logic.
/// </summary>
public class SkillsPresenterFactory : ISkillsPresenterFactory
{
	private readonly IResourceLoader _resourceLoader;
	private readonly IInGameLogger _logger;
	private readonly SkillDependencies _skillDependencies;
	private readonly ISkillsFactory _skillsFactory;
	private readonly SkillPresentersIdByTypeContainer _skillPresentersIdByTypeContainer;

	/// <summary>
	/// Initializes a new instance of the <see cref="SkillsPresenterFactory"/> class.
	/// </summary>
	/// <param name="skillDependencies">Common skill-related dependencies.</param>
	/// <param name="resourceLoader">An object responsible for loading resources (e.g., prefabs).</param>
	/// <param name="logger">An in-game logger for error or debug messages.</param>
	/// <param name="config">Config that contains settings and data about skills.</param>
	/// <param name="saveSystem">Local save system to retrieve data such as team configurations.</param>
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
		_skillPresentersIdByTypeContainer = new SkillPresentersIdByTypeContainer();
	}

	/// <inheritdoc />
	/// <remarks>
	/// This method instantiates a skill presenter of the resolved type asynchronously.
	/// If the resolved presenter type is <see cref="BasicFireballSkillPresenter"/>,
	/// it creates and returns an instance; otherwise, it logs an error and returns <c>null</c>.
	/// </remarks>
	public async Task<SkillPresenterBase> GetAsync(
		string characterId,
		string skillId,
		Transform parent,
		CancellationToken token)
	{
		try
		{
			var skillType = _skillPresentersIdByTypeContainer.GetTypeById(skillId);
			if (skillType == typeof(BasicFireballSkillPresenter))
			{
				var presenter = await GetCharacterGetBasicFireballSkillAsync(
					characterId,
					skillId,
					parent,
					token);

				return presenter;
			}

			_logger.LogError($"Skill presenter with type {0} does not have implementation + {skillType.FullName}");
			return null;
		}
		catch (Exception e)
		{
			_logger.LogException(e);
			return null;
		}
	}

	/// <inheritdoc />
	/// <remarks>
	/// This non-asynchronous version of the presenter retrieval method uses lambda callbacks, 
	/// which can be less performant compared to the async version. 
	/// If the specified presenter type is <see cref="BasicFireballSkillPresenter"/>,
	/// it creates an instance; otherwise, it logs an error.
	/// </remarks>
	public void Get(
		string characterId,
		string skillId,
		Transform parent,
		Action<SkillPresenterBase> onPresenterCreated,
		CancellationToken token)
	{
		var skillType = _skillPresentersIdByTypeContainer.GetTypeById(skillId);

		if (skillType == typeof(BasicFireballSkillPresenter))
		{
			GetCharacterGetBasicFireballSkill(
				characterId,
				skillId,
				parent,
				onPresenterCreated,
				token);
		}

		_logger.LogError($"Skill presenter with type {0} does not have implementation + {skillType.FullName}");
	}

	private async Task<BasicFireballSkillPresenter> GetCharacterGetBasicFireballSkillAsync(
		string characterId,
		string skillId,
		Transform parent,
		CancellationToken token)
	{
		var basicFireballResourceId = ResourceIdsContainer.CharacterSkills.BasicFireball;
		var basicFireballSkillView =
			await _resourceLoader.LoadAndCreateAsync<BasicSkillView, Transform>(basicFireballResourceId, parent, token);

		var skill = _skillsFactory.GetSkill<GenericDamageSkill>(skillId, characterId);
		var model = new BasicFireballSkillModel(skillId);
		var tickHandler = _skillDependencies.TickHandler;
		var movementService = _skillDependencies.MovementService;

		return new BasicFireballSkillPresenter(basicFireballSkillView, model, tickHandler, _logger, skill, movementService);
	}

	private void GetCharacterGetBasicFireballSkill(
		string characterId,
		string skillId,
		Transform parent,
		Action<BasicFireballSkillPresenter> onPresenterCreated,
		CancellationToken token)
	{
		var basicFireballResourceId = ResourceIdsContainer.CharacterSkills.BasicFireball;
		_resourceLoader.LoadResource<BasicSkillView>(basicFireballResourceId, (viewPrefab) =>
		{
			var basicFireballSkillView = Object.Instantiate(viewPrefab, parent);
			var skill = _skillsFactory.GetSkill<GenericDamageSkill>(skillId, characterId);
			var model = new BasicFireballSkillModel(skillId);
			var tickHandler = _skillDependencies.TickHandler;
			var movementService = _skillDependencies.MovementService;

			var presenter = new BasicFireballSkillPresenter(basicFireballSkillView, model, tickHandler, _logger, skill,
				movementService);
			onPresenterCreated?.Invoke(presenter);
		}, token);
	}
}
}