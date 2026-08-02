using DungeonTeam.Gameplay.Dungeon.Domain;
using UnityEngine;

namespace DungeonTeam.Gameplay.Dungeon.Runtime.Authoring
{
    public sealed class ObjectivePlacementAuthoring : MonoBehaviour
    {
        [SerializeField]
        private string _placementId;

        [SerializeField]
        private string _slotTag;

        internal ObjectivePlacement ToDomain(string runtimePlacementId)
        {
            return new ObjectivePlacement(runtimePlacementId, _slotTag, transform.ToDungeonPose());
        }

        internal string PlacementId => _placementId;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.4f);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up);
        }
    }
}
