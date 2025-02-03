using System;
using UnityEngine;

namespace Code.GameConfig.ScriptableObjectParser.RemoteData.Skills
{
[Serializable]
internal struct SkillsGroup
{
	[field: SerializeField]
	internal SkillTypeRemote Type { get; private set; }
	
	[field: SerializeField]
	internal SkillRemote[] Skills { get; private set; }
}
}