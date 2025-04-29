using System;
using System.Collections.Generic;
using LocalSaveSystem;

namespace Code.SavesContainers.TeamSave
{
[Serializable]
public class PlayerTeamSave : ISavable
{
	public string SaveId => "PlayerTeamSave";

	public IReadOnlyDictionary<string, CharacterSave> SelectedPlayerTeam => _selectedPlayerTeam;

	private Dictionary<string, CharacterSave> _selectedPlayerTeam;

	public void InitializeAsNewSave()
	{
		_selectedPlayerTeam = new Dictionary<string, CharacterSave>();
	}

	public void CopyFrom(ISavable loadedSavable)
	{
		if (loadedSavable is PlayerTeamSave playerTeamSave)
		{
			_selectedPlayerTeam = new Dictionary<string, CharacterSave>(playerTeamSave.SelectedPlayerTeam);
		}
	}

	public void AddCharacter(CharacterSave characterSave)
	{
		var id = characterSave.Id;

		_selectedPlayerTeam[id] = characterSave;
	}

	public void RemoveCharacter(CharacterSave characterSave)
	{
		var characterId = characterSave.Id;
		_selectedPlayerTeam.Remove(characterId);
	}

	public void UpdateCharacterHealth(string characterId, int health)
	{
		var characterSave = _selectedPlayerTeam[characterId];
		characterSave.CurrentHealth = health;
	}

	public void UpdateCharacterLevel(string characterId, int level)
	{
		var characterSave = _selectedPlayerTeam[characterId];
		characterSave.CurrentLevel = level;
	}

	public void IncreaseSkillLevel(string characterId, string skillId)
	{
		var characterSave = _selectedPlayerTeam[characterId];
		var skills = characterSave.Skills;
		for (var i = 0; i < skills.Count; i++)
		{
			var skill = skills[i];
			if (skill.Id != skillId)
			{
				continue;
			}

			skills[i] = new CharacterSkillSave(skillId, skill.SkillLevel + 1);
		}
	}

	public void DecreaseSkillLevel(string characterId, string skillId)
	{
		var characterSave = _selectedPlayerTeam[characterId];
		var skills = characterSave.Skills;
		for (var i = 0; i < skills.Count; i++)
		{
			var skill = skills[i];
			if (skill.Id != skillId)
			{
				continue;
			}

			skills[i] = new CharacterSkillSave(skillId, skill.SkillLevel - 1);
		}
	}
}
}