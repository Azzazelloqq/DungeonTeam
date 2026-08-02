using System;
using UnityEngine;

namespace Code.UIService
{
    [Serializable]
    public struct UIElementSettings
    {
        [SerializeField]
        private UIElementGroup _group;

        [SerializeField]
        private UIElementHideBehavior _hideBehavior;

        public UIElementSettings(UIElementGroup group, UIElementHideBehavior hideBehavior)
        {
            _group = group;
            _hideBehavior = hideBehavior;
        }

        public UIElementGroup Group => _group;

        public UIElementHideBehavior HideBehavior => _hideBehavior;
    }
}
