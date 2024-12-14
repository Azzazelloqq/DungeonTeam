using Code.Config;
using UnityEngine;

namespace Code.GameConfig.ScriptableObjectParser.RemoteData.CharacterTeamPlace
{
[CreateAssetMenu(fileName = "TeamPlaces", menuName = "Config/Pages/TeamPlaces")]
public class CharacterTeamPlacesRemotePage : ScriptableObject, IRemotePage
{
	[field: SerializeField]
	public PlaceRemote[] Places { get; private set; }

	[field: SerializeField]
	public float TeamSpeed { get; private set; }
}
}