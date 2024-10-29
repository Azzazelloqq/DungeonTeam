using System;
using Code.Config;
using UnityEngine;

namespace Code.GameConfig.ScriptableObjectParser.RemoteData.Skills
{
[Serializable]
public struct SkillRemote : IRemoteData
{
	[field: SerializeField]
	public string Id { get; private set; }
	
	[field: SerializeField]
	public SkillImpact[] ImpactsByLevel { get; private set; }
}
}