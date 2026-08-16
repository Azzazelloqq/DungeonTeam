using System.Threading;
using System.Threading.Tasks;
using DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.Gameplay.GuildHall.Base;
using UnityEngine;

namespace DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.Gameplay.GuildHall
{
    public sealed class GuildHallModel : GuildHallModelBase
    {
        private bool _isWorldInputBlocked;
        private Vector3 _velocity;
        private string _currentInteractionId;

        public override bool IsWorldInputBlocked => _isWorldInputBlocked;
        public override Vector3 Velocity => _velocity;
        public override string CurrentInteractionId => _currentInteractionId;

        public override void SetWorldInputBlocked(bool isBlocked)
        {
            _isWorldInputBlocked = isBlocked;
        }

        public override void SetVelocity(Vector3 velocity)
        {
            _velocity = velocity;
        }

        public override void SetCurrentInteraction(string interactionId)
        {
            _currentInteractionId = interactionId;
        }

        protected override void OnInitialize()
        {
        }

        protected override ValueTask OnInitializeAsync(CancellationToken token)
        {
            return default;
        }

        protected override void OnDispose()
        {
            _velocity = Vector3.zero;
            _currentInteractionId = null;
            _isWorldInputBlocked = true;
        }

        protected override ValueTask OnDisposeAsync(CancellationToken token)
        {
            OnDispose();
            return default;
        }
    }
}
