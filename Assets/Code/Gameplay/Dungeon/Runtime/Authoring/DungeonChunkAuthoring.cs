using UnityEngine;

namespace DungeonTeam.Gameplay.Dungeon.Runtime.Authoring
{
    public sealed class DungeonChunkAuthoring : MonoBehaviour
    {
        [SerializeField]
        private Vector3 _boundsCenter;

        [SerializeField]
        private Vector3 _boundsSize = new Vector3(6f, 3f, 6f);

        internal Vector3 BoundsCenter => _boundsCenter;
        internal Vector3 BoundsSize => _boundsSize;

        private void OnDrawGizmosSelected()
        {
            var previousMatrix = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(_boundsCenter, _boundsSize);
            Gizmos.matrix = previousMatrix;

        }
    }
}
