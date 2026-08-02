using DungeonTeam.Gameplay.Dungeon.Domain;
using UnityEngine;

namespace DungeonTeam.Gameplay.Dungeon.Runtime.Authoring
{
    public sealed class DungeonConnectionPortAuthoring : MonoBehaviour
    {
        [SerializeField]
        private string _portId;

        [SerializeField]
        private string _portType = "door";

        [SerializeField]
        private DungeonPortDirection _direction;

        internal string PortId => _portId;
        internal string PortType => _portType;
        internal DungeonPortDirection Direction => _direction;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, 0.25f);
            var localDirection = _direction switch
            {
                DungeonPortDirection.North => Vector3.forward,
                DungeonPortDirection.East => Vector3.right,
                DungeonPortDirection.South => Vector3.back,
                DungeonPortDirection.West => Vector3.left,
                _ => Vector3.zero
            };
            var directionSpace = transform.parent != null ? transform.parent : transform;
            Gizmos.DrawLine(
                transform.position,
                transform.position + directionSpace.TransformDirection(localDirection));
        }
    }
}
