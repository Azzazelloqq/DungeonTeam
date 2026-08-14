using System;
using DungeonTeam.Gameplay.Dungeon.Domain;
using UnityEngine;

namespace DungeonTeam.Gameplay.Dungeon.Runtime.Authoring
{
    internal static class DungeonVisibilityAuthoringReader
    {
        public static DungeonVisibilityLayout Read(
            Transform mapRoot,
            DungeonVisibilityAuthoring authoring)
        {
            if (authoring == null)
            {
                return DungeonVisibilityLayout.Empty;
            }

            var anchor = RequireChild(mapRoot, authoring.DoorInteractionAnchor, "door anchor");
            RequireChild(mapRoot, authoring.ClosedDoor, "closed door");
            RequireChild(mapRoot, authoring.UnrevealedVeil, "unrevealed veil");
            return new DungeonVisibilityLayout(
                zoneCount: 2,
                new[] { new DungeonVisibilityDoor(anchor.ToDungeonPose(), revealedZoneIndex: 1) });
        }

        private static Transform RequireChild(Transform mapRoot, Component component, string name)
        {
            if (component == null)
            {
                throw new InvalidOperationException($"Dungeon visibility {name} is missing.");
            }

            return RequireChild(mapRoot, component.transform, name);
        }

        private static Transform RequireChild(Transform mapRoot, GameObject value, string name)
        {
            return RequireChild(mapRoot, value != null ? value.transform : null, name);
        }

        private static Transform RequireChild(Transform mapRoot, Transform value, string name)
        {
            if (value == null || (value != mapRoot && !value.IsChildOf(mapRoot)))
            {
                throw new InvalidOperationException(
                    $"Dungeon visibility {name} must belong to the map hierarchy.");
            }

            return value;
        }
    }
}
