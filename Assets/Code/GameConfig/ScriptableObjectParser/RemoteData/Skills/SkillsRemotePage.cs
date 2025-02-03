using Code.Config;
using UnityEngine;

namespace Code.GameConfig.ScriptableObjectParser.RemoteData.Skills
{
[CreateAssetMenu(fileName = "Skills", menuName = "Config/Pages/Skills")]
public class SkillsRemotePage : ScriptableObject, IRemotePage
{
	[field: SerializeField]
	internal SkillsGroup[] Skills { get; private set; }
}
}