using UnityEngine;

namespace DungeonTeam.Gameplay.Dungeon.Runtime.Authoring
{
    public sealed class DungeonMapAuthoring : MonoBehaviour
    {
        [SerializeField]
        private Transform _entry;

        [SerializeField]
        private Transform _exit;

        internal Transform Entry => _entry;
        internal Transform Exit => _exit;

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
}
