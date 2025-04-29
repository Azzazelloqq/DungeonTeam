using System;
using UnityEngine;

namespace Code.GameConfig.ScriptableObjectParser.RemoteData.Characters
{
[Serializable]
internal struct CharactersGroup
{
	[field: SerializeField]
	internal string CharactersGroupId { get; private set; }

	[field: SerializeField]
	internal CharacterRemote[] Characters { get; private set; }
}
}