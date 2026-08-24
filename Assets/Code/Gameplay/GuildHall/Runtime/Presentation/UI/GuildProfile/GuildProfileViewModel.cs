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
        private readonly Func<GuildProfileEditRequest, GuildProfileEditResult> _editRequested;

        public GuildProfileViewModel(
            GuildProfileModelBase model,
            Action closed,
            Func<GuildProfileEditRequest, GuildProfileEditResult> editRequested) : base(model)
        {
            _closed = closed ?? throw new ArgumentNullException(nameof(closed));
            _editRequested = editRequested ?? throw new ArgumentNullException(nameof(editRequested));
            SelectHeroCommand = new RelayCommand<string>(model.Select);
            CloseCommand = new RelayCommand<object>(_ => Close());
            SelectHeroCommand.AddTo(compositeDisposable);
            CloseCommand.AddTo(compositeDisposable);
            SetLeaderCommand = new RelayCommand<object>(_ => Edit(GuildProfileEditKind.SetLeader));
            AddCompanionCommand = new RelayCommand<object>(_ => Edit(GuildProfileEditKind.AddCompanion));
            RemoveCompanionCommand = new RelayCommand<object>(_ => Edit(GuildProfileEditKind.RemoveCompanion));
            SetLoadoutCommand = new RelayCommand<string>(loadoutId => Edit(
                GuildProfileEditKind.SetLoadout,
                loadoutId));
            EquipItemCommand = new RelayCommand<string>(instanceId => Edit(
                GuildProfileEditKind.EquipItem,
                itemInstanceId: instanceId));
            UnequipItemCommand = new RelayCommand<object>(slot => Edit(
                GuildProfileEditKind.UnequipItem,
                equipmentSlot: (GuildProfileEquipmentSlot)slot));
            SellUniqueItemCommand = new RelayCommand<string>(instanceId => Edit(
                GuildProfileEditKind.SellUniqueItem,
                itemInstanceId: instanceId));
            SellResourceCommand = new RelayCommand<string>(definitionId => Edit(
                GuildProfileEditKind.SellResource,
                definitionId: definitionId));
            SetLeaderCommand.AddTo(compositeDisposable);
            AddCompanionCommand.AddTo(compositeDisposable);
            RemoveCompanionCommand.AddTo(compositeDisposable);
            SetLoadoutCommand.AddTo(compositeDisposable);
            EquipItemCommand.AddTo(compositeDisposable);
            UnequipItemCommand.AddTo(compositeDisposable);
            SellUniqueItemCommand.AddTo(compositeDisposable);
            SellResourceCommand.AddTo(compositeDisposable);
        }

        public override GuildProfileSnapshot Profile => model.Profile;
        public override IReadOnlyReactiveProperty<GuildProfileSnapshot> CurrentProfile => model.CurrentProfile;
        public override IReadOnlyReactiveProperty<bool> IsVisible => model.IsVisible;
        public override IReadOnlyReactiveProperty<GuildHeroSnapshot> SelectedHero =>
            model.SelectedHero;
        public override IReadOnlyReactiveProperty<GuildTextSnapshot> Rejection => model.Rejection;
        public override IRelayCommand<string> SelectHeroCommand { get; }
        public override IRelayCommand<object> CloseCommand { get; }
        public override IRelayCommand<object> SetLeaderCommand { get; }
        public override IRelayCommand<object> AddCompanionCommand { get; }
        public override IRelayCommand<object> RemoveCompanionCommand { get; }
        public override IRelayCommand<string> SetLoadoutCommand { get; }
        public override IRelayCommand<string> EquipItemCommand { get; }
        public override IRelayCommand<object> UnequipItemCommand { get; }
        public override IRelayCommand<string> SellUniqueItemCommand { get; }
        public override IRelayCommand<string> SellResourceCommand { get; }
        public override void Open() => model.Show();

        public override void Close()
        {
            if (model.IsVisible.Value)
            {
                _closed();
            }
        }

        private void Edit(
            GuildProfileEditKind kind,
            string loadoutId = null,
            string itemInstanceId = null,
            GuildProfileEquipmentSlot? equipmentSlot = null,
            string definitionId = null)
        {
            model.Apply(_editRequested(new GuildProfileEditRequest(
                kind,
                kind == GuildProfileEditKind.SellUniqueItem || kind == GuildProfileEditKind.SellResource
                    ? null
                    : model.SelectedHero.Value.ActorId,
                loadoutId,
                itemInstanceId,
                equipmentSlot,
                definitionId)));
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
