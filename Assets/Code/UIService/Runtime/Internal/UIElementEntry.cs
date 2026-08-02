using UnityEngine;

namespace Code.UIService
{
    internal sealed class UIElementEntry
    {
        public UIElementEntry(
            IUIElement element,
            GameObject gameObject,
            GameObject prefab,
            UIElementSettings settings)
        {
            Element = element;
            GameObject = gameObject;
            Prefab = prefab;
            Settings = settings;
            State = UIElementState.Hidden;
        }

        public IUIElement Element { get; }

        public GameObject GameObject { get; }

        public GameObject Prefab { get; }

        public UIElementSettings Settings { get; }

        public UIElementState State { get; set; }
    }
}
