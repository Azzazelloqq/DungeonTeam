using DungeonTeam.Gameplay.Dungeon.Domain;
using UnityEngine;

namespace DungeonTeam.Gameplay.Dungeon.Runtime.Authoring
{
    public sealed class EnemyPlacementAuthoring : MonoBehaviour
    {
        [SerializeField]
        private string _placementId;

        [SerializeField]
        private DungeonPlacementMode _mode;

        [SerializeField]
        private string _slotTag;

        [SerializeField]
        private string _fixedEnemyId;

        [SerializeField]
        private string _encounterGroupId;

        internal EnemyPlacement ToDomain()
        {
            return new EnemyPlacement(
                _placementId,
                _mode,
                _slotTag,
                _fixedEnemyId,
                _encounterGroupId,
                transform.ToDungeonPose());
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
        }
    }
}
