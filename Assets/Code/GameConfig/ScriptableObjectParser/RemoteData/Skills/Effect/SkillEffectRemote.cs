using System;
using UnityEngine;

namespace Code.GameConfig.ScriptableObjectParser.RemoteData.Skills.Effect
{
[Serializable]
internal struct SkillEffectRemote
{
	[SerializeField]
	private EffectType _effectType;

	[SerializeField]
	private string _effectId;

	[SerializeField]
	private int _effectImpact;

	[SerializeField]
	private float _effectDuration;

	[SerializeField]
	private float _interval;

	internal EffectType EffectType => _effectType;
	internal string EffectId => _effectId;
	internal int EffectImpact => _effectImpact;
	internal float EffectDuration => _effectDuration;
	internal float Interval => _interval;
}
}