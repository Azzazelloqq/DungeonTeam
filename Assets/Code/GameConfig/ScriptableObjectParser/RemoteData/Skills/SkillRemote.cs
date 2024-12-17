using System;
using UnityEngine;

namespace Code.GameConfig.ScriptableObjectParser.RemoteData.Skills
{
[Serializable]
public struct SkillRemote
{
	[field: SerializeField]
	public string SkillId { get; private set; }

	[field: SerializeField]
	public SkillStatsRemote[] ImpactsByLevel { get; private set; }

}
}