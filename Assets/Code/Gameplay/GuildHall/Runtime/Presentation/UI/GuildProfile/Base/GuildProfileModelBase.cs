using Azzazelloqq.MVVM.Core;
using Azzazelloqq.MVVM.ReactiveLibrary;
using DungeonTeam.Gameplay.GuildHall.Application;

namespace DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.UI.GuildProfile.Base
{
    public abstract class GuildProfileModelBase : ModelBase
    {
        public abstract GuildProfileSnapshot Profile { get; }
        public abstract IReadOnlyReactiveProperty<GuildProfileSnapshot> CurrentProfile { get; }
        public abstract IReadOnlyReactiveProperty<bool> IsVisible { get; }
        public abstract IReadOnlyReactiveProperty<GuildHeroSnapshot> SelectedHero { get; }
        public abstract IReadOnlyReactiveProperty<GuildTextSnapshot> Rejection { get; }
        public abstract void Show();
        public abstract void Hide();
        public abstract void Select(string actorId);
        public abstract void Apply(GuildProfileEditResult result);
    }
}
