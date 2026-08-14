using System;
using UnityEngine;

namespace DungeonTeam.Gameplay.Dungeon.Runtime.Authoring
{
    public sealed class DungeonMapAuthoring : MonoBehaviour
    {
        [SerializeField]
        private Transform _entry;

        [SerializeField]
        private Transform _exit;

        [SerializeField]
        private Transform[] _routeCheckpoints = Array.Empty<Transform>();

        [SerializeField]
        private DungeonCameraShotAuthoring[] _cameraShots =
            Array.Empty<DungeonCameraShotAuthoring>();

        [SerializeField]
        private Transform _encounterStart;

        [SerializeField]
        private Transform _encounterEnd;

        [SerializeField]
        private Transform[] _companionFormationAnchors = Array.Empty<Transform>();

        [SerializeField]
        private Transform[] _tacticalAnchors = Array.Empty<Transform>();

        [SerializeField]
        private DungeonVisibilityAuthoring _visibility;

        internal Transform Entry => _entry;
        internal Transform Exit => _exit;
        internal Transform[] RouteCheckpoints => _routeCheckpoints;
        internal DungeonCameraShotAuthoring[] CameraShots => _cameraShots;
        internal Transform EncounterStart => _encounterStart;
        internal Transform EncounterEnd => _encounterEnd;
        internal Transform[] CompanionFormationAnchors => _companionFormationAnchors;
        internal Transform[] TacticalAnchors => _tacticalAnchors;
        internal DungeonVisibilityAuthoring Visibility => _visibility;
        internal bool HasAnySpatialData =>
            (_routeCheckpoints != null && _routeCheckpoints.Length != 0) ||
            (_cameraShots != null && _cameraShots.Length != 0) ||
            _encounterStart != null ||
            _encounterEnd != null ||
            (_companionFormationAnchors != null &&
             _companionFormationAnchors.Length != 0) ||
            (_tacticalAnchors != null && _tacticalAnchors.Length != 0);

        private void OnDrawGizmosSelected()
        {
            DrawMarker(_entry, Color.green);
            DrawMarker(_exit, Color.red);
        }

        private static void DrawMarker(Transform marker, Color color)
        {
            if (marker == null)
            {
                return;
            }

            Gizmos.color = color;
            Gizmos.DrawWireSphere(marker.position, 0.35f);
            Gizmos.DrawLine(marker.position, marker.position + marker.forward);
        }
    }

    [Serializable]
    public sealed class DungeonCameraShotAuthoring
    {
        [SerializeField]
        private Transform _anchor;

        [SerializeField]
        private Transform _routeCheckpoint;

        [SerializeField, Min(0f)]
        private float _lookAheadDistance;

        [SerializeField, Min(0.01f)]
        private float _activationRange = 8f;

        [SerializeField, Min(0f)]
        private float _blendRange = 3f;

        internal Transform Anchor => _anchor;
        internal Transform RouteCheckpoint => _routeCheckpoint;
        internal float LookAheadDistance => _lookAheadDistance;
        internal float ActivationRange => _activationRange;
        internal float BlendRange => _blendRange;
    }
}
