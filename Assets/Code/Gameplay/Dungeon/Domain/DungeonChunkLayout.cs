using System;
using System.Collections.Generic;

namespace DungeonTeam.Gameplay.Dungeon.Domain
{
    public enum DungeonPortDirection
    {
        North = 0,
        East = 1,
        South = 2,
        West = 3
    }

    public readonly struct DungeonChunkBounds
    {
        private const float OverlapTolerance = 0.001f;

        public DungeonChunkBounds(float centerX, float centerZ, float sizeX, float sizeZ)
        {
            if (sizeX <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(sizeX));
            }

            if (sizeZ <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(sizeZ));
            }

            CenterX = centerX;
            CenterZ = centerZ;
            SizeX = sizeX;
            SizeZ = sizeZ;
        }

        public float CenterX { get; }
        public float CenterZ { get; }
        public float SizeX { get; }
        public float SizeZ { get; }

        public bool Overlaps(DungeonChunkBounds other)
        {
            return Math.Abs(CenterX - other.CenterX) <
                   (SizeX + other.SizeX) * 0.5f - OverlapTolerance &&
                   Math.Abs(CenterZ - other.CenterZ) <
                   (SizeZ + other.SizeZ) * 0.5f - OverlapTolerance;
        }
    }

    public readonly struct DungeonChunkPort
    {
        public DungeonChunkPort(
            string portId,
            string portType,
            float x,
            float z,
            DungeonPortDirection direction)
        {
            PortId = RequireId(portId, nameof(portId));
            PortType = RequireId(portType, nameof(portType));
            if (!Enum.IsDefined(typeof(DungeonPortDirection), direction))
            {
                throw new ArgumentOutOfRangeException(nameof(direction));
            }

            X = x;
            Z = z;
            Direction = direction;
        }

        public string PortId { get; }
        public string PortType { get; }
        public float X { get; }
        public float Z { get; }
        public DungeonPortDirection Direction { get; }

        private static string RequireId(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("ID cannot be empty.", parameterName);
            }

            return value;
        }
    }

    public sealed class DungeonChunkMetadata
    {
        public DungeonChunkMetadata(
            string chunkId,
            DungeonChunkBounds bounds,
            IReadOnlyList<DungeonChunkPort> ports)
        {
            if (string.IsNullOrWhiteSpace(chunkId))
            {
                throw new ArgumentException("ID cannot be empty.", nameof(chunkId));
            }

            if (ports == null)
            {
                throw new ArgumentNullException(nameof(ports));
            }

            var portIds = new HashSet<string>(StringComparer.Ordinal);
            var portCopy = new DungeonChunkPort[ports.Count];
            for (var index = 0; index < ports.Count; index++)
            {
                var port = ports[index];
                if (!portIds.Add(port.PortId))
                {
                    throw new ArgumentException(
                        $"Chunk '{chunkId}' contains duplicate port ID '{port.PortId}'.",
                        nameof(ports));
                }

                portCopy[index] = port;
            }

            ChunkId = chunkId;
            Bounds = bounds;
            Ports = portCopy;
        }

        public string ChunkId { get; }
        public DungeonChunkBounds Bounds { get; }
        public IReadOnlyList<DungeonChunkPort> Ports { get; }
    }

    public readonly struct DungeonChunkPlacement
    {
        public DungeonChunkPlacement(
            string chunkId,
            float x,
            float z,
            int rotationQuarterTurns,
            int connectedToPlacementIndex,
            DungeonChunkBounds worldBounds)
        {
            ChunkId = chunkId;
            X = x;
            Z = z;
            RotationQuarterTurns = rotationQuarterTurns;
            ConnectedToPlacementIndex = connectedToPlacementIndex;
            WorldBounds = worldBounds;
        }

        public string ChunkId { get; }
        public float X { get; }
        public float Z { get; }
        public int RotationQuarterTurns { get; }
        public int ConnectedToPlacementIndex { get; }
        public DungeonChunkBounds WorldBounds { get; }
    }

    public sealed class DungeonChunkLayout
    {
        public DungeonChunkLayout(IReadOnlyList<DungeonChunkPlacement> placements)
        {
            if (placements == null)
            {
                throw new ArgumentNullException(nameof(placements));
            }

            var copy = new DungeonChunkPlacement[placements.Count];
            for (var index = 0; index < placements.Count; index++)
            {
                copy[index] = placements[index];
            }

            Placements = copy;
        }

        public IReadOnlyList<DungeonChunkPlacement> Placements { get; }
    }

    public sealed class DungeonLayoutGenerationException : InvalidOperationException
    {
        public DungeonLayoutGenerationException(string message)
            : base(message)
        {
        }
    }
}
