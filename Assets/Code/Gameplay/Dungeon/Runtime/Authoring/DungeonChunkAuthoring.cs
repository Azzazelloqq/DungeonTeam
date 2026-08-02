using UnityEngine;

namespace DungeonTeam.Gameplay.Dungeon.Runtime.Authoring
{
    public sealed class DungeonChunkAuthoring : MonoBehaviour
    {
        [SerializeField]
        private Vector3 _boundsCenter;

        [SerializeField]
        private Vector3 _boundsSize = new Vector3(6f, 3f, 6f);

        [SerializeField]
        private Transform _entry;

        [SerializeField]
        private Transform _exit;

        internal Vector3 BoundsCenter => _boundsCenter;
        internal Vector3 BoundsSize => _boundsSize;
        internal Transform Entry => _entry;
        internal Transform Exit => _exit;

        private void OnDrawGizmosSelected()
        {
            var previousMatrix = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(_boundsCenter, _boundsSize);
            Gizmos.matrix = previousMatrix;

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
}
