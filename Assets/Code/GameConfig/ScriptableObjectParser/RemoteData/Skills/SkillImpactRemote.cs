﻿using System;
using UnityEngine;

namespace Code.GameConfig.ScriptableObjectParser.RemoteData.Skills
{
[Serializable]
public struct SkillImpactRemote
{
	[field: SerializeField]
	public int Level { get; private set; }

	[field: SerializeField]
	public int Impact { get; private set; }

	[field: SerializeField]
	public float CooldownPerSeconds { get; private set; }

	[field: SerializeField]
	public float ChargePerSeconds { get; private set; }
}
}