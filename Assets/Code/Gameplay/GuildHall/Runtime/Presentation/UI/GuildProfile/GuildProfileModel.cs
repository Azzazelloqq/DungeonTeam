using System;
using System.Threading;
using System.Threading.Tasks;
using Azzazelloqq.MVVM.ReactiveLibrary;
using DungeonTeam.Gameplay.GuildHall.Application;
using DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.UI.GuildProfile.Base;

namespace DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.UI.GuildProfile
{
    public sealed class GuildProfileModel : GuildProfileModelBase
    {
        private readonly ReactiveProperty<bool> _isVisible = new(false);
        private readonly ReactiveProperty<GuildProfileSnapshot> _profile;
        private readonly ReactiveProperty<GuildHeroSnapshot> _selectedHero;
        private readonly ReactiveProperty<GuildTextSnapshot> _rejection = new(null);

        public GuildProfileModel(GuildProfileSnapshot profile)
        {
            _profile = new ReactiveProperty<GuildProfileSnapshot>(
                profile ?? throw new ArgumentNullException(nameof(profile)));
            _selectedHero = new ReactiveProperty<GuildHeroSnapshot>(profile.Leader);
            _isVisible.AddTo(compositeDisposable);
            _profile.AddTo(compositeDisposable);
            _selectedHero.AddTo(compositeDisposable);
            _rejection.AddTo(compositeDisposable);
        }

        public override GuildProfileSnapshot Profile => _profile.Value;
        public override IReadOnlyReactiveProperty<GuildProfileSnapshot> CurrentProfile => _profile;
        public override IReadOnlyReactiveProperty<bool> IsVisible => _isVisible;
        public override IReadOnlyReactiveProperty<GuildHeroSnapshot> SelectedHero => _selectedHero;
        public override IReadOnlyReactiveProperty<GuildTextSnapshot> Rejection => _rejection;
        public override void Show() => _isVisible.SetValue(true);
        public override void Hide() => _isVisible.SetValue(false);

        public override void Select(string actorId)
        {
            if (string.IsNullOrWhiteSpace(actorId))
            {
                throw new ArgumentException("Actor ID cannot be empty.", nameof(actorId));
            }

            for (var index = 0; index < Profile.Roster.Count; index++)
            {
                if (string.Equals(
                        Profile.Roster[index].ActorId,
                        actorId,
                        StringComparison.Ordinal))
                {
                    _selectedHero.SetValue(Profile.Roster[index]);
                    return;
                }
            }

            throw new ArgumentException(
                $"Unknown profile roster actor ID '{actorId}'.",
                nameof(actorId));
        }

        public override void Apply(GuildProfileEditResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            if (!result.Accepted)
            {
                _rejection.SetValue(result.Rejection);
                return;
            }

            var selectedActorId = _selectedHero.Value.ActorId;
            _profile.SetValue(result.Profile);
            _rejection.SetValue(null);
            Select(selectedActorId);
        }

        protected override void OnInitialize() { }
        protected override ValueTask OnInitializeAsync(CancellationToken token) => default;
        protected override void OnDispose() { }
        protected override ValueTask OnDisposeAsync(CancellationToken token) => default;
    }
}
