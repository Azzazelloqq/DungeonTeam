using System;
using Code.GameConfig.ScriptableObjectParser.RemoteData.Skills.Effect;
using UnityEngine;

namespace Code.GameConfig.ScriptableObjectParser.RemoteData.Skills
{
[Serializable]
internal struct SkillStatsRemote
{
	[field: SerializeField]
	internal int Level { get; private set; }

	[field: SerializeField]
	internal float CooldownPerSeconds { get; private set; }

	[field: SerializeField]
	internal float ChargePerSeconds { get; private set; }

	[field: SerializeField]
	internal SkillEffectRemote[] Effects { get; private set; }
}
}