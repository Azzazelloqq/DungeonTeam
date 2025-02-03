using System;
using UnityEngine;

namespace Code.GameConfig.ScriptableObjectParser.RemoteData.Skills
{
[Serializable]
internal struct SkillRemote
{
	[field: SerializeField]
	internal string SkillId { get; private set; }

	[field: SerializeField]
	internal SkillStatsRemote[] ImpactsByLevel { get; private set; }

}
}