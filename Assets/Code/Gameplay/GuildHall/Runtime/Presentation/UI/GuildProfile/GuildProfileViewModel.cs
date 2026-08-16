using System;
using System.Threading;
using System.Threading.Tasks;
using Azzazelloqq.MVVM.Core;
using Azzazelloqq.MVVM.ReactiveLibrary;
using DungeonTeam.Gameplay.GuildHall.Application;
using DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.UI.GuildProfile.Base;

namespace DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.UI.GuildProfile
{
    public sealed class GuildProfileViewModel : GuildProfileViewModelBase
    {
        private readonly Action _closed;

        public GuildProfileViewModel(GuildProfileModelBase model, Action closed) : base(model)
        {
            _closed = closed ?? throw new ArgumentNullException(nameof(closed));
            SelectHeroCommand = new RelayCommand<string>(model.Select);
            CloseCommand = new RelayCommand<object>(_ => Close());
            SelectHeroCommand.AddTo(compositeDisposable);
            CloseCommand.AddTo(compositeDisposable);
        }

        public override GuildProfileSnapshot Profile => model.Profile;
        public override IReadOnlyReactiveProperty<bool> IsVisible => model.IsVisible;
        public override IReadOnlyReactiveProperty<GuildHeroSnapshot> SelectedHero =>
            model.SelectedHero;
        public override IRelayCommand<string> SelectHeroCommand { get; }
        public override IRelayCommand<object> CloseCommand { get; }
        public override void Open() => model.Show();

        public override void Close()
        {
            if (model.IsVisible.Value)
            {
                _closed();
            }
        }

        protected override void OnInitialize() { }
        protected override ValueTask OnInitializeAsync(CancellationToken token) => default;
        protected override void OnDispose() => Close();

        protected override ValueTask OnDisposeAsync(CancellationToken token)
        {
            Close();
            return default;
        }
    }
}
