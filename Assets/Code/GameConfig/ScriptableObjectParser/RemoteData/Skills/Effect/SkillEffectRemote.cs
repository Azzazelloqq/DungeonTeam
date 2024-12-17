using System;
using UnityEngine;

namespace Code.GameConfig.ScriptableObjectParser.RemoteData.Skills.Effect
{
[Serializable]
public struct SkillEffectRemote
{
	[SerializeField] private EffectType _effectType;
	[SerializeField] private string _effectId;
	[SerializeField] private int _effectImpact;
	[SerializeField] private float _effectDuration;
	[SerializeField] private float _interval;

	public EffectType EffectType => _effectType;
	public string EffectId => _effectId;
	public int EffectImpact => _effectImpact;
	public float EffectDuration => _effectDuration;
	public float Interval => _interval;
}
}