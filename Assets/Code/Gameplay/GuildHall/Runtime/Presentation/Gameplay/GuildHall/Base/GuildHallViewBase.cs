using DungeonTeam.Gameplay.ContextActions.Runtime.Base;
using DungeonTeam.Gameplay.GuildHall.Runtime.Interaction;
using DungeonTeam.Gameplay.AmbientNpc.Runtime;
using DungeonTeam.Gameplay.AmbientNpc.Runtime.Presentation.Gameplay.AmbientNpc.Base;
using DungeonTeam.Gameplay.AmbientNpc.Runtime.Presentation.UI.Dialogue.Base;
using DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.UI.NoticeBoard;
using DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.UI.RunSummary;
using DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.UI.GuildProfile.Base;
using DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.UI.QuestRewardCollection.Base;
using MVP;
using UnityEngine;

namespace DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.Gameplay.GuildHall.Base
{
    public abstract class GuildHallViewBase : ViewMonoBehaviour<GuildHallPresenterBase>
    {
        public abstract Transform PlayerTransform { get; }
        public abstract Transform CameraTransform { get; }
        public abstract ContextActionsViewBase ContextActionsView { get; }
        public abstract GuildHallInteractionPoint[] InteractionPoints { get; }
        public virtual AmbientNpcViewBase[] AmbientNpcViews => System.Array.Empty<AmbientNpcViewBase>();
        public virtual AmbientNpcVignetteBinding[] AmbientNpcVignettes => System.Array.Empty<AmbientNpcVignetteBinding>();
        public virtual DialogueViewBase DialogueView => null;
        public virtual NoticeBoardViewBase NoticeBoardView => null;
        public virtual RunSummaryViewBase RunSummaryView => null;
        public virtual GuildProfileViewBase GuildProfileView => null;
        public virtual QuestRewardCollectionViewBase QuestRewardCollectionView => null;

        public abstract void ValidateBindings();
        public abstract void ResetPlayer();
        public abstract void Move(Vector3 displacement);
    }
}
