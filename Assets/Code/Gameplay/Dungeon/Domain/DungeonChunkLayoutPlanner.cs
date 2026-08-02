using System;
using System.Collections.Generic;

namespace DungeonTeam.Gameplay.Dungeon.Domain
{
    public sealed class DungeonChunkLayoutPlanner
    {
        public DungeonChunkLayout Build(
            int seed,
            DungeonChunkMetadata entryChunk,
            IReadOnlyList<DungeonChunkMetadata> mandatoryChunks,
            IReadOnlyList<DungeonChunkMetadata> chunkPool,
            DungeonChunkMetadata exitChunk,
            int targetChunkCount,
            int maxGenerationAttempts)
        {
            ValidateArguments(
                entryChunk,
                mandatoryChunks,
                chunkPool,
                exitChunk,
                targetChunkCount,
                maxGenerationAttempts);

            for (var attempt = 0; attempt < maxGenerationAttempts; attempt++)
            {
                var random = new DungeonDeterministicRandom(
                    unchecked(seed + attempt * 486187739));
                if (TryBuild(
                        entryChunk,
                        mandatoryChunks,
                        chunkPool,
                        exitChunk,
                        targetChunkCount,
                        random,
                        out var layout))
                {
                    return layout;
                }
            }

            throw new DungeonLayoutGenerationException(
                $"Could not build a {targetChunkCount}-chunk layout after " +
                $"{maxGenerationAttempts} attempts.");
        }

        private static bool TryBuild(
            DungeonChunkMetadata entryChunk,
            IReadOnlyList<DungeonChunkMetadata> mandatoryChunks,
            IReadOnlyList<DungeonChunkMetadata> chunkPool,
            DungeonChunkMetadata exitChunk,
            int targetChunkCount,
            DungeonDeterministicRandom random,
            out DungeonChunkLayout layout)
        {
            var placements = new List<DungeonChunkPlacement>(targetChunkCount);
            var openPorts = new List<OpenPort>();
            var entryBounds = TransformBounds(entryChunk.Bounds, 0f, 0f, 0);
            placements.Add(new DungeonChunkPlacement(
                entryChunk.ChunkId,
                0f,
                0f,
                0,
                -1,
                entryBounds));
            AddOpenPorts(openPorts, entryChunk, 0f, 0f, 0, 0, consumedPortIndex: -1);

            for (var index = 0; index < mandatoryChunks.Count; index++)
            {
                if (!TryPlace(
                        new[] { mandatoryChunks[index] },
                        placements,
                        openPorts,
                        random))
                {
                    layout = null;
                    return false;
                }
            }

            var poolChunkCount = targetChunkCount - mandatoryChunks.Count - 2;
            for (var index = 0; index < poolChunkCount; index++)
            {
                if (!TryPlace(chunkPool, placements, openPorts, random))
                {
                    layout = null;
                    return false;
                }
            }

            if (!TryPlace(new[] { exitChunk }, placements, openPorts, random))
            {
                layout = null;
                return false;
            }

            layout = new DungeonChunkLayout(placements);
            return true;
        }

        private static bool TryPlace(
            IReadOnlyList<DungeonChunkMetadata> candidates,
            List<DungeonChunkPlacement> placements,
            List<OpenPort> openPorts,
            DungeonDeterministicRandom random)
        {
            if (candidates.Count == 0 || openPorts.Count == 0)
            {
                return false;
            }

            var candidateStart = random.Next(candidates.Count);
            var openPortStart = random.Next(openPorts.Count);
            for (var candidateOffset = 0; candidateOffset < candidates.Count; candidateOffset++)
            {
                var candidate = candidates[(candidateStart + candidateOffset) % candidates.Count];
                if (candidate.Ports.Count == 0)
                {
                    continue;
                }

                var candidatePortStart = random.Next(candidate.Ports.Count);
                var rotationStart = random.Next(4);
                for (var openOffset = 0; openOffset < openPorts.Count; openOffset++)
                {
                    var openIndex = (openPortStart + openOffset) % openPorts.Count;
                    var openPort = openPorts[openIndex];
                    for (var portOffset = 0; portOffset < candidate.Ports.Count; portOffset++)
                    {
                        var candidatePortIndex =
                            (candidatePortStart + portOffset) % candidate.Ports.Count;
                        var candidatePort = candidate.Ports[candidatePortIndex];
                        if (!string.Equals(
                                candidatePort.PortType,
                                openPort.PortType,
                                StringComparison.Ordinal))
                        {
                            continue;
                        }

                        for (var rotationOffset = 0; rotationOffset < 4; rotationOffset++)
                        {
                            var rotation = (rotationStart + rotationOffset) % 4;
                            if (Rotate(candidatePort.Direction, rotation) !=
                                Opposite(openPort.Direction))
                            {
                                continue;
                            }

                            Rotate(candidatePort.X, candidatePort.Z, rotation,
                                out var rotatedPortX, out var rotatedPortZ);
                            var x = openPort.X - rotatedPortX;
                            var z = openPort.Z - rotatedPortZ;
                            var bounds = TransformBounds(candidate.Bounds, x, z, rotation);
                            if (OverlapsAny(bounds, placements))
                            {
                                continue;
                            }

                            var placementIndex = placements.Count;
                            placements.Add(new DungeonChunkPlacement(
                                candidate.ChunkId,
                                x,
                                z,
                                rotation,
                                openPort.PlacementIndex,
                                bounds));
                            openPorts.RemoveAt(openIndex);
                            AddOpenPorts(
                                openPorts,
                                candidate,
                                x,
                                z,
                                rotation,
                                placementIndex,
                                candidatePortIndex);
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static void AddOpenPorts(
            ICollection<OpenPort> openPorts,
            DungeonChunkMetadata chunk,
            float x,
            float z,
            int rotation,
            int placementIndex,
            int consumedPortIndex)
        {
            for (var index = 0; index < chunk.Ports.Count; index++)
            {
                if (index == consumedPortIndex)
                {
                    continue;
                }

                var port = chunk.Ports[index];
                Rotate(port.X, port.Z, rotation, out var portX, out var portZ);
                openPorts.Add(new OpenPort(
                    port.PortType,
                    x + portX,
                    z + portZ,
                    Rotate(port.Direction, rotation),
                    placementIndex));
            }
        }

        private static bool OverlapsAny(
            DungeonChunkBounds bounds,
            IReadOnlyList<DungeonChunkPlacement> placements)
        {
            for (var index = 0; index < placements.Count; index++)
            {
                if (bounds.Overlaps(placements[index].WorldBounds))
                {
                    return true;
                }
            }

            return false;
        }

        private static DungeonChunkBounds TransformBounds(
            DungeonChunkBounds bounds,
            float x,
            float z,
            int rotation)
        {
            Rotate(bounds.CenterX, bounds.CenterZ, rotation, out var centerX, out var centerZ);
            var swapSize = rotation % 2 != 0;
            return new DungeonChunkBounds(
                x + centerX,
                z + centerZ,
                swapSize ? bounds.SizeZ : bounds.SizeX,
                swapSize ? bounds.SizeX : bounds.SizeZ);
        }

        private static void Rotate(
            float x,
            float z,
            int rotation,
            out float rotatedX,
            out float rotatedZ)
        {
            switch (rotation)
            {
                case 0:
                    rotatedX = x;
                    rotatedZ = z;
                    return;
                case 1:
                    rotatedX = z;
                    rotatedZ = -x;
                    return;
                case 2:
                    rotatedX = -x;
                    rotatedZ = -z;
                    return;
                case 3:
                    rotatedX = -z;
                    rotatedZ = x;
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(rotation));
            }
        }

        private static DungeonPortDirection Rotate(
            DungeonPortDirection direction,
            int rotation)
        {
            return (DungeonPortDirection)(((int)direction + rotation) % 4);
        }

        private static DungeonPortDirection Opposite(DungeonPortDirection direction)
        {
            return (DungeonPortDirection)(((int)direction + 2) % 4);
        }

        private static void ValidateArguments(
            DungeonChunkMetadata entryChunk,
            IReadOnlyList<DungeonChunkMetadata> mandatoryChunks,
            IReadOnlyList<DungeonChunkMetadata> chunkPool,
            DungeonChunkMetadata exitChunk,
            int targetChunkCount,
            int maxGenerationAttempts)
        {
            if (entryChunk == null)
            {
                throw new ArgumentNullException(nameof(entryChunk));
            }

            if (mandatoryChunks == null)
            {
                throw new ArgumentNullException(nameof(mandatoryChunks));
            }

            if (chunkPool == null)
            {
                throw new ArgumentNullException(nameof(chunkPool));
            }

            if (exitChunk == null)
            {
                throw new ArgumentNullException(nameof(exitChunk));
            }

            var minimumChunkCount = mandatoryChunks.Count + 2;
            if (targetChunkCount < minimumChunkCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(targetChunkCount),
                    $"Target count must be at least {minimumChunkCount}.");
            }

            if (targetChunkCount > minimumChunkCount && chunkPool.Count == 0)
            {
                throw new ArgumentException(
                    "Chunk pool cannot be empty when the target requires pool chunks.",
                    nameof(chunkPool));
            }

            if (maxGenerationAttempts <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxGenerationAttempts));
            }

            for (var index = 0; index < mandatoryChunks.Count; index++)
            {
                if (mandatoryChunks[index] == null)
                {
                    throw new ArgumentException(
                        $"Mandatory chunk at index {index} is empty.",
                        nameof(mandatoryChunks));
                }
            }

            for (var index = 0; index < chunkPool.Count; index++)
            {
                if (chunkPool[index] == null)
                {
                    throw new ArgumentException(
                        $"Pool chunk at index {index} is empty.",
                        nameof(chunkPool));
                }
            }
        }

        private readonly struct OpenPort
        {
            public OpenPort(
                string portType,
                float x,
                float z,
                DungeonPortDirection direction,
                int placementIndex)
            {
                PortType = portType;
                X = x;
                Z = z;
                Direction = direction;
                PlacementIndex = placementIndex;
            }

            public string PortType { get; }
            public float X { get; }
            public float Z { get; }
            public DungeonPortDirection Direction { get; }
            public int PlacementIndex { get; }
        }

    }
}
