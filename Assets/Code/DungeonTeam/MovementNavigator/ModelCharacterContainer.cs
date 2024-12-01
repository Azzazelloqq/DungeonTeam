using Code.DungeonTeam.TeamCharacter;

namespace Code.DungeonTeam.MovementNavigator {
	public struct ModelCharacterContainer {
		public string Id { get; }
		public CharacterClass CharacterClass { get; }
		
		public ModelCharacterContainer(string id, CharacterClass characterClass) {
			Id = id;
			CharacterClass = characterClass;
		}
	}
}