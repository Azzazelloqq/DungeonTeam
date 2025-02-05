using Code.Config;
using UnityEngine;

namespace Code.GameConfig.ScriptableObjectParser.ConfigData.DetectConfig
{
public struct DetectConfigPage : IConfigPage
{
	public LayerMask DetectLayerMask { get; }

	internal DetectConfigPage(LayerMask detectLayerMask)
	{
		DetectLayerMask = detectLayerMask;
	}
}
}