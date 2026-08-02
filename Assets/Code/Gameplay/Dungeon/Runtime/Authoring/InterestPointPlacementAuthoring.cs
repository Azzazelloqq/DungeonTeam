using DungeonTeam.Gameplay.Dungeon.Domain;
using UnityEngine;

namespace DungeonTeam.Gameplay.Dungeon.Runtime.Authoring
{
    public sealed class InterestPointPlacementAuthoring : MonoBehaviour
    {
        [SerializeField]
        private string _placementId;

        [SerializeField]
        private DungeonPlacementMode _mode;

        [SerializeField]
        private string _slotTag;

        [SerializeField]
        private string _fixedInterestPointId;

        [SerializeField]
        private string _fixedRewardProfileId;

        internal InterestPointPlacement ToDomain()
        {
            return new InterestPointPlacement(
                _placementId,
                _mode,
                _slotTag,
                _fixedInterestPointId,
                _fixedRewardProfileId,
                transform.ToDungeonPose());
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
        }
    }
}
