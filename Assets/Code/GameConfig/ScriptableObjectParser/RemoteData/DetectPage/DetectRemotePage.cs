using Code.Config;
using UnityEngine;

namespace Code.GameConfig.ScriptableObjectParser.RemoteData.DetectPage
{
[CreateAssetMenu(fileName = "DetectConfig", menuName = "Config/Pages/DetectConfig")]
public class DetectRemotePage : ScriptableObject, IRemotePage
{
	[field: SerializeField]
	public LayerMask ObstacleLayer { get; private set; }
}
}