﻿using System.Collections.Generic;
using LocalSaveSystem;
using Unity.Plastic.Newtonsoft.Json;

namespace Code.SavesContainers.TeamSave {
public class PlayerTeamSave : ISavable {
	public string SaveId => "PlayerTeamSave";

	[JsonIgnore]
	public IReadOnlyList<CharacterSave> SelectedPlayerTeam => _selectedPlayerTeam;
	
	[JsonProperty("SelectedPlayerTeam")]
	private List<CharacterSave> _selectedPlayerTeam;
	
	public void InitializeAsNewSave() {
		_selectedPlayerTeam = new List<CharacterSave>();
	}

	public void CopyFrom(ISavable loadedSavable) {
		if (loadedSavable is PlayerTeamSave playerTeamSave) {
			_selectedPlayerTeam = new List<CharacterSave>(playerTeamSave.SelectedPlayerTeam);
		}
	}
	
	public void AddCharacter(CharacterSave characterSave) {
		_selectedPlayerTeam.Add(characterSave);
	}
	
	public void RemoveCharacter(CharacterSave characterSave) {
		_selectedPlayerTeam.Remove(characterSave);
	}

	public void UpdateCharacterHealth(string characterId, int health)
	{
		for (var i = 0; i < _selectedPlayerTeam.Count; i++)
		{
			var characterSave = _selectedPlayerTeam[i];
			var savedId = characterSave.Id;
			if (savedId != characterId)
			{
				continue;
			}

			var skills = characterSave.Skills;
			var level = characterSave.CurrentLevel;
			_selectedPlayerTeam[i] = new CharacterSave(savedId, level, health, skills);
		}
	}
	
	public void UpdateCharacterLevel(string characterId, int level)
	{
		for (var i = 0; i < _selectedPlayerTeam.Count; i++)
		{
			var characterSave = _selectedPlayerTeam[i];
			var savedId = characterSave.Id;
			if (savedId != characterId)
			{
				continue;
			}
			
			var skills = characterSave.Skills;
			var health = characterSave.CurrentHealth;
			_selectedPlayerTeam[i] = new CharacterSave(savedId, health, health, skills);
		}
	}

	public void IncreaseSkillLevel(string characterId, string skillId)
	{
		foreach (var characterSave in _selectedPlayerTeam)
		{
			if (characterSave.Id != characterId)
			{
				continue;
			}

			var skills = characterSave.Skills;
			for (var i = 0; i < skills.Length; i++)
			{
				var skill = skills[i];
				
				if(skill.Id != skillId)
				{
					continue;
				}
				
				skills[i] = new CharacterSkillSave(skill.Id, skill.SkillLevel + 1);
				break;
			}
			
			break;
		}
	}
	
	public void DecreaseSkillLevel(string characterId, string skillId)
	{
		foreach (var characterSave in _selectedPlayerTeam)
		{
			if (characterSave.Id != characterId)
			{
				continue;
			}

			var skills = characterSave.Skills;
			for (var i = 0; i < skills.Length; i++)
			{
				var skill = skills[i];
				
				if(skill.Id != skillId)
				{
					continue;
				}
				
				skills[i] = new CharacterSkillSave(skill.Id, skill.SkillLevel - 1);
				break;
			}
			
			break;
		}
	}
}
}