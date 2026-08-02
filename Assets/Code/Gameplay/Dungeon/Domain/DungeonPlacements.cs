using System;

namespace DungeonTeam.Gameplay.Dungeon.Domain
{
    public enum DungeonPlacementMode
    {
        Fixed = 0,
        Slot = 1,
        OptionalFixed = 2
    }

    public readonly struct EnemyPlacement
    {
        public EnemyPlacement(
            string placementId,
            DungeonPlacementMode mode,
            string slotTag,
            string fixedEnemyId,
            string encounterGroupId,
            DungeonPose pose)
            : this(
                placementId,
                placementId,
                mode,
                slotTag,
                fixedEnemyId,
                encounterGroupId,
                pose)
        {
        }

        public EnemyPlacement(
            string placementId,
            string authoringId,
            DungeonPlacementMode mode,
            string slotTag,
            string fixedEnemyId,
            string encounterGroupId,
            DungeonPose pose)
        {
            PlacementId = RequireId(placementId, nameof(placementId));
            AuthoringId = RequireId(authoringId, nameof(authoringId));
            ValidateMode(mode, slotTag, fixedEnemyId, nameof(fixedEnemyId));

            Mode = mode;
            SlotTag = slotTag;
            FixedEnemyId = fixedEnemyId;
            EncounterGroupId = encounterGroupId;
            Pose = pose;
        }

        public string PlacementId { get; }
        public string AuthoringId { get; }
        public DungeonPlacementMode Mode { get; }
        public string SlotTag { get; }
        public string FixedEnemyId { get; }
        public string EncounterGroupId { get; }
        public DungeonPose Pose { get; }

        private static void ValidateMode(
            DungeonPlacementMode mode,
            string slotTag,
            string fixedContentId,
            string fixedContentParameterName)
        {
            if (mode == DungeonPlacementMode.Slot)
            {
                RequireId(slotTag, nameof(slotTag));
                return;
            }

            if (mode != DungeonPlacementMode.Fixed && mode != DungeonPlacementMode.OptionalFixed)
            {
                throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown placement mode.");
            }

            RequireId(fixedContentId, fixedContentParameterName);
        }

        private static string RequireId(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("ID cannot be empty.", parameterName);
            }

            return value;
        }
    }

    public readonly struct InterestPointPlacement
    {
        public InterestPointPlacement(
            string placementId,
            DungeonPlacementMode mode,
            string slotTag,
            string fixedInterestPointId,
            string fixedRewardProfileId,
            DungeonPose pose)
            : this(
                placementId,
                placementId,
                mode,
                slotTag,
                fixedInterestPointId,
                fixedRewardProfileId,
                pose)
        {
        }

        public InterestPointPlacement(
            string placementId,
            string authoringId,
            DungeonPlacementMode mode,
            string slotTag,
            string fixedInterestPointId,
            string fixedRewardProfileId,
            DungeonPose pose)
        {
            if (string.IsNullOrWhiteSpace(placementId))
            {
                throw new ArgumentException("ID cannot be empty.", nameof(placementId));
            }

            if (mode == DungeonPlacementMode.Slot)
            {
                if (string.IsNullOrWhiteSpace(slotTag))
                {
                    throw new ArgumentException("Slot tag cannot be empty for a slot placement.", nameof(slotTag));
                }
            }
            else if (mode == DungeonPlacementMode.Fixed || mode == DungeonPlacementMode.OptionalFixed)
            {
                if (string.IsNullOrWhiteSpace(fixedInterestPointId))
                {
                    throw new ArgumentException(
                        "Fixed content ID cannot be empty for a fixed placement.",
                        nameof(fixedInterestPointId));
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown placement mode.");
            }

            if (string.IsNullOrWhiteSpace(authoringId))
            {
                throw new ArgumentException("ID cannot be empty.", nameof(authoringId));
            }

            PlacementId = placementId;
            AuthoringId = authoringId;
            Mode = mode;
            SlotTag = slotTag;
            FixedInterestPointId = fixedInterestPointId;
            FixedRewardProfileId = fixedRewardProfileId;
            Pose = pose;
        }

        public string PlacementId { get; }
        public string AuthoringId { get; }
        public DungeonPlacementMode Mode { get; }
        public string SlotTag { get; }
        public string FixedInterestPointId { get; }
        public string FixedRewardProfileId { get; }
        public DungeonPose Pose { get; }
    }

    public readonly struct ObjectivePlacement
    {
        public ObjectivePlacement(string placementId, string slotTag, DungeonPose pose)
        {
            if (string.IsNullOrWhiteSpace(placementId))
            {
                throw new ArgumentException("ID cannot be empty.", nameof(placementId));
            }

            if (string.IsNullOrWhiteSpace(slotTag))
            {
                throw new ArgumentException("Slot tag cannot be empty.", nameof(slotTag));
            }

            PlacementId = placementId;
            SlotTag = slotTag;
            Pose = pose;
        }

        public string PlacementId { get; }
        public string SlotTag { get; }
        public DungeonPose Pose { get; }
    }
}
