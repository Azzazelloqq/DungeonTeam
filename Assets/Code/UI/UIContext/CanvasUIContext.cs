using UnityEngine;

namespace Code.UI.UIContext
{
public class CanvasUIContext : MonoBehaviour, IUIContext
{
	[field: SerializeField]
	public Transform UIElementsOverlay { get; private set; }
}
}