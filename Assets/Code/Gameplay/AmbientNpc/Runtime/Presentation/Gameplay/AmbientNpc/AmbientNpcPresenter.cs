using System;
using System.Threading;
using System.Threading.Tasks;
using DungeonTeam.Gameplay.AmbientNpc.Application;
using DungeonTeam.Gameplay.AmbientNpc.Runtime.Presentation.Gameplay.AmbientNpc.Base;
using UnityEngine;

namespace DungeonTeam.Gameplay.AmbientNpc.Runtime.Presentation.Gameplay.AmbientNpc
{
    public sealed class AmbientNpcPresenter : AmbientNpcPresenterBase
    {
        private readonly AmbientNpcProfileSnapshot _profile;
        private readonly AmbientNpcRoutineStateMachine _stateMachine = new();
        private Vector3 _pausedFacingTarget;

        public AmbientNpcPresenter(
            AmbientNpcViewBase view,
            AmbientNpcModelBase model,
            AmbientNpcProfileSnapshot profile)
            : base(view, model)
        {
            _profile = profile ?? throw new ArgumentNullException(nameof(profile));
        }

        public override void Tick(float deltaTime)
        {
            if (model.IsPaused)
            {
                view.FaceTowards(_pausedFacingTarget, _profile.TurnSpeed, deltaTime);
                return;
            }

            switch (model.State)
            {
                case AmbientNpcRoutineState.Idle:
                    model.AdvanceElapsed(deltaTime);
                    if (model.StateElapsed >= _profile.IdleDurationMin)
                    {
                        AdvanceRoutine();
                    }
                    break;
                case AmbientNpcRoutineState.MoveToAnchor:
                    TickMove(deltaTime);
                    break;
                case AmbientNpcRoutineState.FaceAnchor:
                    TickFace(deltaTime);
                    break;
                case AmbientNpcRoutineState.Activity:
                    model.AdvanceElapsed(deltaTime);
                    if (model.StateElapsed >= _profile.ActivityDurationMin)
                    {
                        AdvanceRoutine();
                    }
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported routine state '{model.State}'.");
            }
        }

        public override void PauseAndFace(Vector3 playerPosition)
        {
            model.SetPaused(true);
            _pausedFacingTarget = playerPosition;
        }

        public override void ResumeRoutine()
        {
            model.SetPaused(false);
        }

        public override void FaceVignetteTarget(Vector3 target, float deltaTime)
        {
            if (!model.IsPaused)
            {
                view.FaceTowards(target, _profile.TurnSpeed, deltaTime);
            }
        }

        public override void SetVignetteActivity(bool isSpeaking)
        {
            if (!model.IsPaused)
            {
                view.SetActivityPose(isSpeaking ? AmbientNpcActivityPose.Talk : AmbientNpcActivityPose.Watch);
            }
        }

        protected override void OnInitialize()
        {
            view.ValidateBindings();
            model.SetState(_stateMachine.Current);
            view.SetActivityPose(AmbientNpcActivityPose.Stand);
        }

        protected override ValueTask OnInitializeAsync(CancellationToken token)
        {
            OnInitialize();
            return default;
        }

        protected override void OnDispose() { }
        protected override ValueTask OnDisposeAsync(CancellationToken token) => default;

        private void TickMove(float deltaTime)
        {
            var anchors = view.RouteAnchors;
            if (anchors.Length == 0)
            {
                AdvanceRoutine();
                return;
            }

            var anchor = anchors[model.RouteAnchorIndex % anchors.Length];
            if (view.MoveTowards(anchor.position, _profile.MovementSpeed, deltaTime))
            {
                AdvanceRoutine();
            }
        }

        private void TickFace(float deltaTime)
        {
            var target = view.ActivityAnchor != null
                ? view.ActivityAnchor.position
                : view.RouteAnchors[model.RouteAnchorIndex % view.RouteAnchors.Length].position;
            if (view.FaceTowards(target, _profile.TurnSpeed, deltaTime))
            {
                AdvanceRoutine();
            }
        }

        private void AdvanceRoutine()
        {
            _stateMachine.Advance(_profile.UsesAuthoredRoute && view.RouteAnchors.Length > 0);
            model.SetState(_stateMachine.Current);
            model.ResetElapsed();
            if (model.State == AmbientNpcRoutineState.Activity)
            {
                view.SetActivityPose(AmbientNpcActivityPose.Stand);
            }
            else if (model.State == AmbientNpcRoutineState.Idle && view.RouteAnchors.Length > 0)
            {
                model.SetRouteAnchorIndex((model.RouteAnchorIndex + 1) % view.RouteAnchors.Length);
            }
        }
    }
}
