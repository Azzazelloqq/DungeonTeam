using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Code.Config;
using Code.GameConfig.ScriptableObjectParser.ConfigData.Characters;
using Code.GameConfig.ScriptableObjectParser.ConfigData.CharacterTeamPlace;
using Code.GameConfig.ScriptableObjectParser.ConfigData.Skills;
using Code.GameConfig.ScriptableObjectParser.RemoteData.Characters;
using Code.GameConfig.ScriptableObjectParser.RemoteData.CharacterTeamPlace;
using Code.GameConfig.ScriptableObjectParser.RemoteData.Skills;
using InGameLogger;

namespace Code.GameConfig.ScriptableObjectParser
{
public class ScriptableObjectConfigParser : IConfigParser
{
	private readonly IRemotePage[] _remoteData;
	private readonly IInGameLogger _logger;

	public ScriptableObjectConfigParser(IRemotePage[] remoteData, IInGameLogger logger)
	{
		_remoteData = remoteData;
		_logger = logger;
	}
	
	public IConfigPage[] Parse()
	{
		var configData = new IConfigPage[_remoteData.Length];
		
		for (var i = 0; i < _remoteData.Length; i++)
		{
			var data = _remoteData[i];
			configData[i] = GetConfig(data);
		}

		return configData;
	}

	public async Task<IConfigPage[]> ParseAsync(CancellationToken token)
	{
		var configData = new IConfigPage[_remoteData.Length];

		await Task.Run(() =>
		{
			for (var i = 0; i < _remoteData.Length; i++)
			{
				var data = _remoteData[i];
				configData[i] = GetConfig(data);
			}
		}, token);

		return configData;
	}

	private IConfigPage GetConfig(IRemotePage remotePage)
	{
		switch (remotePage)
		{
			case CharactersRemotePage characterRemote:
				return ParseCharacterRemote(characterRemote);
			case SkillsRemotePage skillRemote:
				return ParseSkillRemote(skillRemote);
			case CharacterTeamPlacesRemotePage characterTeamPlaceRemote:
				return ParseCharacterTeamPlace(characterTeamPlaceRemote);
			default:
				_logger.LogException(new Exception($"Need add parse for {remotePage.GetType().Name}"));
				return null;
		}
	}

	private IConfigPage ParseCharacterTeamPlace(CharacterTeamPlacesRemotePage characterTeamPlacesRemotePage)
	{
		var placeLocalConfigs = new PlaceConfig[characterTeamPlacesRemotePage.Places.Length];
		for (var i = 0; i < characterTeamPlacesRemotePage.Places.Length; i++)
		{
			var remotePlace = characterTeamPlacesRemotePage.Places[i];
			var localPlace = new PlaceConfig(remotePlace.PlaceNumber, (CharacterClass)remotePlace.PreferredClass);
			placeLocalConfigs[i] = localPlace;
		}

		var teamSpeed = characterTeamPlacesRemotePage.TeamSpeed;
		var config = new CharacterTeamMoveConfigPage(placeLocalConfigs, teamSpeed);

		return config;
	}

	private IConfigPage ParseCharacterRemote(CharactersRemotePage characterRemotePage)
	{
		var charactersData = new Dictionary<string, CharacterConfig>();

		foreach (var characterGroup in characterRemotePage.CharactersGroups)
		{
			foreach (var characterRemote in characterGroup.Characters)
			{
				var healthByLevelRemote = characterRemote.HealthByLevel;
				var healthByLevel = new CharacterHealthByLevelConfig[healthByLevelRemote.Length];
				for (var i = 0; i < healthByLevelRemote.Length; i++)
				{
					var healthByLevelRemoteItem = healthByLevelRemote[i];
					var healthByLevelItem = new CharacterHealthByLevelConfig(
						healthByLevelRemoteItem.Level,
						healthByLevelRemoteItem.Health);
					
					healthByLevel[i] = healthByLevelItem;
				}
				
				var characterId = characterRemote.Id;
				var characterSkills = characterRemote.Skills;
				var characterClass = (CharacterClass)characterRemote.CharacterClass;
				var attackConfig = new CharacterAttackConfig(characterRemote.AttackInfo);
				var characterData = new CharacterConfig(characterId, characterSkills, characterClass, attackConfig, healthByLevel);
				
				charactersData[characterId] = characterData;
			}
		}

		var charactersConfigPage = new CharactersConfigPage(charactersData);

		return charactersConfigPage;
	}

	private IConfigPage ParseSkillRemote(SkillsRemotePage skillRemote)
	{
		var skillsGroups = new Dictionary<SkillType, SkillsGroupConfig>();
		
		foreach (var skillsGroupRemote in skillRemote.Skills)
		{
			var skillsConfig = new Dictionary<string, SkillConfig>();
			var skillGroupTypeRemote = skillsGroupRemote.Type;
			var skillType = (SkillType)skillGroupTypeRemote;
			
			foreach (var skillsInGroupRemote in skillsGroupRemote.Skills)
			{
				var skillImpacts = new Dictionary<int, SkillStatsConfig>(skillsInGroupRemote.ImpactsByLevel.Length);
				var skillId = skillsInGroupRemote.SkillId;

				for (var i = 0; i < skillsInGroupRemote.ImpactsByLevel.Length; i++)
				{
					var skillImpactRemote = skillsInGroupRemote.ImpactsByLevel[i];
					var cooldownPerMilliseconds = (int)skillImpactRemote.CooldownPerSeconds * 1000;
					var chargePerMilliseconds = (int)skillImpactRemote.ChargePerSeconds * 1000;
					var level = skillImpactRemote.Level;
					var impact = skillImpactRemote.Impact;
					
					var skillStatsConfig = new SkillStatsConfig(
						level,
						impact,
						cooldownPerMilliseconds,
						chargePerMilliseconds);
					
					skillImpacts[level] = skillStatsConfig;
				}

				skillsConfig[skillId] = new SkillConfig(skillId, skillImpacts, skillType);
			}

			var skillsGroup = new SkillsGroupConfig(skillType, skillsConfig);

			skillsGroups[skillType] = skillsGroup;
		}

		var skillsConfigPage = new SkillsConfigPage(skillsGroups);

		return skillsConfigPage;
	}
}
}