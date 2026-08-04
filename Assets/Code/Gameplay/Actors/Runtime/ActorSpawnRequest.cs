using System;
using UnityEngine;

namespace DungeonTeam.Gameplay.Actors.Runtime
{
    public readonly struct ActorSpawnRequest
    {
        public ActorSpawnRequest(
            string instanceName,
            Vector3 position,
            Quaternion rotation)
        {
            if (string.IsNullOrWhiteSpace(instanceName))
            {
                throw new ArgumentException("Instance name cannot be empty.", nameof(instanceName));
            }

            InstanceName = instanceName;
            Position = position;
            Rotation = rotation;
        }

        public string InstanceName { get; }
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
    }
}
