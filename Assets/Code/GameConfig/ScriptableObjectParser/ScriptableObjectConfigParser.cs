using System;
using System.Threading;
using System.Threading.Tasks;
using Code.Config;
using Code.DungeonTeam.TeamCharacter;
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
	private readonly IRemoteData[] _remoteData;
	private readonly IInGameLogger _logger;

	public ScriptableObjectConfigParser(IRemoteData[] remoteData, IInGameLogger logger)
	{
		_remoteData = remoteData;
		_logger = logger;
	}
	
	public IConfigData[] Parse()
	{
		var configData = new IConfigData[_remoteData.Length];
		
		for (var i = 0; i < _remoteData.Length; i++)
		{
			var data = _remoteData[i];
			configData[i] = GetConfig(data);
		}

		return configData;
	}

	public async Task<IConfigData[]> ParseAsync(CancellationToken token)
	{
		var configData = new IConfigData[_remoteData.Length];

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

	private IConfigData GetConfig(IRemoteData remoteData)
	{
		switch (remoteData)
		{
			case CharacterRemote characterRemote:
				return ParseCharacterRemote(characterRemote);
			case SkillRemote skillRemote:
				return ParseSkillRemote(skillRemote);
			case CharacterTeamPlaceRemote characterTeamPlaceRemote:
				return ParseCharacterTeamPlace(characterTeamPlaceRemote);
			default:
				_logger.LogException(new Exception($"Need add parse for {remoteData.GetType().Name}"));
				return null;
		}
	}

	private IConfigData ParseCharacterTeamPlace(CharacterTeamPlaceRemote characterTeamPlaceRemote)
	{
		var classesRemote = characterTeamPlaceRemote.ClassesForPlace;
		var placeNumberRemote = characterTeamPlaceRemote.PlaceNumber;
		var classes = new CharacterClass[classesRemote.Length];
		for (var i = 0; i < classesRemote.Length; i++)
		{
			var classRemote = classesRemote[i];
			if (classRemote == CharacterClassType.None)
			{
				_logger.LogError($"Class type in remote config {characterTeamPlaceRemote.GetType().Name} is None");
				continue;
			}
			
			var characterClass = (CharacterClass)classRemote;

			classes[i] = characterClass;
		}

		var config = new CharacterTeamPlaceConfig(classes, placeNumberRemote);

		return config;
	}

	private IConfigData ParseCharacterRemote(CharacterRemote characterRemote)
	{
		var characterRemoteId = characterRemote.Id;
		var characterRemoteSkills = characterRemote.Skills;
		
		var characterConfig = new CharacterConfig(characterRemoteId, characterRemoteSkills);

		return characterConfig;
	}

	private IConfigData ParseSkillRemote(SkillRemote skillRemote)
	{
		var skillRemoteId = skillRemote.Id;
		var skillRemoteImpact = skillRemote.ImpactsByLevel;

		var skillImpactByLevelConfig = new SkillImpact[skillRemoteImpact.Length];
		
		for (var i = 0; i < skillRemoteImpact.Length; i++)
		{
			var impactRemote = skillRemoteImpact[i];
			skillImpactByLevelConfig[i] = new SkillImpact(impactRemote.Level, impactRemote.Impact);
		}
		
		var skillConfig = new SkillConfig(skillRemoteId, skillRemoteImpact);
		
		return skillConfig;
	}
}
}