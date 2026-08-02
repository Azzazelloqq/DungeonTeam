using System;
using UnityEngine;

namespace DungeonTeam.Gameplay.Dungeon.Runtime.Authoring
{
    [CreateAssetMenu(
        menuName = "DungeonTeam/Gameplay/Dungeon Tile Set",
        fileName = "DungeonTileSet")]
    public sealed class DungeonTileSetAsset : ScriptableObject
    {
        [SerializeField]
        private GameObject _floorPrefab;

        [SerializeField]
        private GameObject _wallPrefab;

        [SerializeField]
        private GameObject _passagePrefab;

        [SerializeField, Min(0.01f)]
        private float _cellSize = 6f;

        [SerializeField]
        private Vector3 _enemySlotOffset;

        [SerializeField]
        private Vector3 _interestPointSlotOffset = new Vector3(1.5f, 0f, 1.5f);

        [SerializeField]
        private Vector3 _objectiveSlotOffset;

        internal GameObject FloorPrefab => _floorPrefab;
        internal GameObject WallPrefab => _wallPrefab;
        internal GameObject PassagePrefab => _passagePrefab;
        internal float CellSize => _cellSize;
        internal Vector3 EnemySlotOffset => _enemySlotOffset;
        internal Vector3 InterestPointSlotOffset => _interestPointSlotOffset;
        internal Vector3 ObjectiveSlotOffset => _objectiveSlotOffset;

        internal void Validate()
        {
            if (_floorPrefab == null)
            {
                throw new InvalidOperationException($"Dungeon tile set '{name}' has no floor prefab.");
            }

            if (_wallPrefab == null)
            {
                throw new InvalidOperationException($"Dungeon tile set '{name}' has no wall prefab.");
            }

            if (_passagePrefab == null)
            {
                throw new InvalidOperationException(
                    $"Dungeon tile set '{name}' has no passage prefab.");
            }

            if (_cellSize <= 0f)
            {
                throw new InvalidOperationException(
                    $"Dungeon tile set '{name}' must have a positive cell size.");
            }
        }
    }
}
