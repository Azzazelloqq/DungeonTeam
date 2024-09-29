using Code.DungeonTeam.TeamCharacter.Base;

namespace Code.DungeonTeam.TeamCharacter
{
public class TeamCharacterModel : TeamCharacterModelBase
{
	public CharacterClass HeroClass { get; }

	public TeamCharacterModel(CharacterClass heroClass) {
		HeroClass = heroClass;
	}
}
}