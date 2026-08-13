using Azzazelloqq.MVVM.Core;
using UnityEngine;

namespace DungeonTeam.UI.CombatHud.Base
{
    public abstract class CombatHudViewBase : ViewMonoBehavior<CombatHudViewModelBase>
    {
        public abstract RectTransform ContextActionsHost { get; }
    }
}
