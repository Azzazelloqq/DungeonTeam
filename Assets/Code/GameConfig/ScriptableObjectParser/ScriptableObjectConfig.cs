using Code.GameConfig.ScriptableObjectParser.RemoteData.Characters;
using Code.GameConfig.ScriptableObjectParser.RemoteData.CharacterTeamPlace;
using Code.GameConfig.ScriptableObjectParser.RemoteData.DetectPage;
using Code.GameConfig.ScriptableObjectParser.RemoteData.Skills;
using UnityEngine;

namespace Code.GameConfig.ScriptableObjectParser
{
[CreateAssetMenu(fileName = "MainConfig", menuName = "Config/MainConfig")]
public class ScriptableObjectConfig : ScriptableObject
{
	[field: SerializeField]
	public SkillsRemotePage SkillsRemotePage { get; private set; }
	
	[field: SerializeField]
	public CharacterTeamPlacesRemotePage CharacterTeamPlacesRemotePage { get; private set; }
	
	[field: SerializeField]
	public CharactersRemotePage CharactersRemotePage { get; private set; }
	
	[field: SerializeField]
	public DetectRemotePage DetectRemotePage { get; private set; }
}
}