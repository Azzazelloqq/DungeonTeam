using System;
using UnityEngine;

namespace Code.GameConfig.ScriptableObjectParser.RemoteData.Skills
{
[Serializable]
public struct SkillsGroup
{
	[field: SerializeField]
	public SkillTypeRemote Type { get; private set; }
	
	[field: SerializeField]
	public SkillRemote[] Skills { get; private set; }
}
}