using System.Collections.Generic;
using Code.Config;

namespace Code.GameConfig.ScriptableObjectParser.ConfigData.Characters
{
public readonly struct CharactersConfigPage : IConfigPage
{
	public IReadOnlyDictionary<string, CharacterConfig> Characters => _characters;

	private readonly Dictionary<string, CharacterConfig> _characters;

	public CharactersConfigPage(Dictionary<string, CharacterConfig> characters)
	{
		_characters = characters;
	}
}
}