using System;
using UnityEngine;

namespace Code.UIService
{
	[Serializable]
	public struct UICanvasContext
	{
		[SerializeField]
		private RectTransform _background;

		[SerializeField]
		private RectTransform _fullScreen;

		[SerializeField]
		private RectTransform _popup;

		[SerializeField]
		private RectTransform _overlayElement;

		[SerializeField]
		private RectTransform _dynamicOverlayElement;

		public UICanvasContext(
			RectTransform background,
			RectTransform fullScreen,
			RectTransform popup,
			RectTransform overlayElement,
			RectTransform dynamicOverlayElement)
		{
			_background = RequireParent(background, nameof(background));
			_fullScreen = RequireParent(fullScreen, nameof(fullScreen));
			_popup = RequireParent(popup, nameof(popup));
			_overlayElement = RequireParent(overlayElement, nameof(overlayElement));
			_dynamicOverlayElement = RequireParent(dynamicOverlayElement, nameof(dynamicOverlayElement));
		}

		public readonly RectTransform GetParent(UIElementGroup group)
		{
			return group switch
			{
				UIElementGroup.Background => _background,
				UIElementGroup.FullScreen => _fullScreen,
				UIElementGroup.Popup => _popup,
				UIElementGroup.OverlayElement => _overlayElement,
				UIElementGroup.DynamicOverlayElement => _dynamicOverlayElement,
				_ => throw new ArgumentOutOfRangeException(nameof(group), group, "Unknown UI element group.")
			};
		}

		private static RectTransform RequireParent(RectTransform parent, string parameterName)
		{
			if (parent == null)
			{
				throw new ArgumentNullException(parameterName);
			}

			return parent;
		}
	}
}