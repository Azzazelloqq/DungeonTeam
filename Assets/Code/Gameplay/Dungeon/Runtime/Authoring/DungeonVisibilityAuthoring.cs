using UnityEngine;

namespace DungeonTeam.Gameplay.Dungeon.Runtime.Authoring
{
    public sealed class DungeonVisibilityAuthoring : MonoBehaviour
    {
        [SerializeField]
        private Transform _doorInteractionAnchor;

        [SerializeField]
        private GameObject _closedDoor;

        [SerializeField]
        private GameObject _unrevealedVeil;

        internal Transform DoorInteractionAnchor => _doorInteractionAnchor;
        internal GameObject ClosedDoor => _closedDoor;
        internal GameObject UnrevealedVeil => _unrevealedVeil;
    }
}
