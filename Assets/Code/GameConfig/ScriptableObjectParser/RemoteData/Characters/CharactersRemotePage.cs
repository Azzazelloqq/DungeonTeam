﻿using Code.Config;
using UnityEngine;

namespace Code.GameConfig.ScriptableObjectParser.RemoteData.Characters
{
[CreateAssetMenu(fileName = "Characters", menuName = "Config/Pages/Characters")]
public class CharactersRemotePage : ScriptableObjectConfig, IRemotePage
{
	[field: SerializeField]
	public CharactersGroup[] CharactersGroups { get; private set; }
}
}