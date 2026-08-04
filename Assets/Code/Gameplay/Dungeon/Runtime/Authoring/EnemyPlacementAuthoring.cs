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
        private string _fixedBehaviorId;

        [SerializeField]
        private string _encounterGroupId;

        internal EnemyPlacement ToDomain(
            string runtimePlacementId,
            string runtimeEncounterGroupId)
        {
            return new EnemyPlacement(
                runtimePlacementId,
                _placementId,
                _mode,
                _slotTag,
                _fixedEnemyId,
                _fixedBehaviorId,
                runtimeEncounterGroupId,
                transform.ToDungeonPose());
        }

        internal string PlacementId => _placementId;
        internal string EncounterGroupId => _encounterGroupId;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
        }
    }
}
