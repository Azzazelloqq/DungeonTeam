using System;
using System.Threading;
using System.Threading.Tasks;
using DungeonTeam.Gameplay.GuildHall.Application;
using DungeonTeam.Gameplay.GuildHall.Runtime.Input;
using DungeonTeam.Gameplay.GuildHall.Runtime.Interaction;
using DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.Gameplay.GuildHall.Base;
using DungeonTeam.Gameplay.AmbientNpc.Runtime;
using TickHandler;
using UnityEngine;

namespace DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.Gameplay.GuildHall
{
    internal sealed class GuildHallPresenter : GuildHallPresenterBase
    {
        private readonly IGuildHallInput _input;
        private readonly ITickHandler _tickHandler;
        private readonly GuildHallInteractionController _interactions;
        private readonly GuildHallMovementSettings _settings;
        private readonly AmbientNpcSet _ambientNpcSet;

        private bool _isInitialized;

        public GuildHallPresenter(
            GuildHallViewBase view,
            GuildHallModelBase model,
            IGuildHallInput input,
            ITickHandler tickHandler,
            GuildHallInteractionController interactions,
            GuildHallMovementSettings settings,
            AmbientNpcSet ambientNpcSet = null)
            : base(view, model)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _tickHandler = tickHandler ?? throw new ArgumentNullException(nameof(tickHandler));
            _interactions = interactions ?? throw new ArgumentNullException(nameof(interactions));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _ambientNpcSet = ambientNpcSet;
        }

        public override void SetWorldInputBlocked(bool isBlocked)
        {
            model.SetWorldInputBlocked(isBlocked);
            _interactions.SetBlocked(isBlocked);
            if (isBlocked)
            {
                model.SetVelocity(Vector3.zero);
            }
        }

        public bool IsWorldInputBlocked => model.IsWorldInputBlocked;

        protected override void OnInitialize()
        {
            view.ValidateBindings();
            view.ResetPlayer();
            _input.Enable();
            _tickHandler.SubscribeOnFrameUpdate(OnFrameUpdate);
            _isInitialized = true;
        }

        protected override ValueTask OnInitializeAsync(CancellationToken token)
        {
            OnInitialize();
            return default;
        }

        protected override void OnDispose()
        {
            if (_isInitialized)
            {
                _tickHandler.UnsubscribeOnFrameUpdate(OnFrameUpdate);
                _isInitialized = false;
            }

            _input.Dispose();
        }

        protected override ValueTask OnDisposeAsync(CancellationToken token)
        {
            OnDispose();
            return default;
        }

        private void OnFrameUpdate(float deltaTime)
        {
            if (model.IsWorldInputBlocked)
            {
                model.SetVelocity(Vector3.zero);
                _interactions.Tick(deltaTime);
                _ambientNpcSet?.Tick(deltaTime);
                return;
            }

            var movement = Vector2.ClampMagnitude(_input.Movement, 1f);
            var forward = view.CameraTransform.forward;
            forward.y = 0f;
            forward.Normalize();
            var right = view.CameraTransform.right;
            right.y = 0f;
            right.Normalize();
            var direction = forward * movement.y + right * movement.x;
            if (direction.sqrMagnitude > 1f)
            {
                direction.Normalize();
            }

            var targetVelocity = direction * _settings.Speed;
            var velocity = Vector3.MoveTowards(
                model.Velocity,
                targetVelocity,
                _settings.Acceleration * Mathf.Max(0f, deltaTime));
            model.SetVelocity(velocity);
            view.Move(velocity * Mathf.Max(0f, deltaTime));
            _interactions.Tick(deltaTime);
            _ambientNpcSet?.Tick(deltaTime);
        }
    }
}
