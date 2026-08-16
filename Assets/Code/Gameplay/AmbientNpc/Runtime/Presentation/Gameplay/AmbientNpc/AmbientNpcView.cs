using System;
using System.Threading;
using System.Threading.Tasks;
using DungeonTeam.Gameplay.AmbientNpc.Runtime.Presentation.Gameplay.AmbientNpc.Base;
using UnityEngine;

namespace DungeonTeam.Gameplay.AmbientNpc.Runtime.Presentation.Gameplay.AmbientNpc
{
    public enum AmbientNpcActivityPose { Stand, Watch, Sit, Drink, Talk }

    public sealed class AmbientNpcView : AmbientNpcViewBase
    {
        [SerializeField] private string _npcId;
        [SerializeField] private Transform _body;
        [SerializeField] private Transform _interactionAnchor;
        [SerializeField] private Transform _activityAnchor;
        [SerializeField] private Transform[] _routeAnchors = Array.Empty<Transform>();
        [SerializeField] private AmbientNpcActivityPose _activityPose;

        public override string NpcId => _npcId;
        public override Transform BodyTransform => _body;
        public override Transform InteractionAnchor => _interactionAnchor;
        public override Transform ActivityAnchor => _activityAnchor;
        public override Transform[] RouteAnchors => _routeAnchors;

        public override void ValidateBindings()
        {
            if (string.IsNullOrWhiteSpace(_npcId) || _body == null || _interactionAnchor == null)
            {
                throw new InvalidOperationException("Ambient NPC requires npc ID, body and interaction anchor.");
            }

            if (_routeAnchors == null)
            {
                throw new InvalidOperationException($"Ambient NPC '{_npcId}' route anchors cannot be null.");
            }

            for (var index = 0; index < _routeAnchors.Length; index++)
            {
                if (_routeAnchors[index] == null)
                {
                    throw new InvalidOperationException($"Ambient NPC '{_npcId}' route anchor at {index} is missing.");
                }
            }
        }

        public override bool MoveTowards(Vector3 target, float speed, float deltaTime)
        {
            var bodyPosition = _body.position;
            target.y = bodyPosition.y;
            _body.position = Vector3.MoveTowards(bodyPosition, target, speed * Mathf.Max(0f, deltaTime));
            return (_body.position - target).sqrMagnitude <= 0.0001f;
        }

        public override bool FaceTowards(Vector3 target, float turnSpeed, float deltaTime)
        {
            var direction = target - _body.position;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return true;
            }

            var targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            _body.rotation = Quaternion.RotateTowards(
                _body.rotation,
                targetRotation,
                turnSpeed * Mathf.Max(0f, deltaTime));
            return Quaternion.Angle(_body.rotation, targetRotation) <= 0.1f;
        }

        public override void SetActivityPose(AmbientNpcActivityPose pose)
        {
            // GH-4 uses primitive authoring; the pose remains an authored semantic marker.
        }

        protected override void OnInitialize() { }
        protected override ValueTask OnInitializeAsync(CancellationToken token) => default;
        protected override void OnDispose() { }
        protected override ValueTask OnDisposeAsync(CancellationToken token) => default;
    }
}
