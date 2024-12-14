using System.Collections.Generic;
using LocalSaveSystem;
using Unity.Plastic.Newtonsoft.Json;

namespace Code.SavesContainers {
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
			if (characterSave.Id != characterId)
			{
				continue;
			}
			
			_selectedPlayerTeam[i] = new CharacterSave(characterSave.Id, characterSave.CurrentLevel, health);
		}
	}
	
	public void UpdateCharacterLevel(string characterId, int level)
	{
		for (var i = 0; i < _selectedPlayerTeam.Count; i++)
		{
			var characterSave = _selectedPlayerTeam[i];
			if (characterSave.Id != characterId)
			{
				continue;
			}
			
			_selectedPlayerTeam[i] = new CharacterSave(characterSave.Id, level, characterSave.CurrentHealth);
		}
	}
}
}