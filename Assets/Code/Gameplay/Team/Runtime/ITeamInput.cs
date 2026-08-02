using System;
using UnityEngine;

namespace DungeonTeam.Gameplay.Team.Runtime
{
    public interface ITeamInput : IDisposable
    {
        Vector2 Movement { get; }

        float CameraYawDelta { get; }

        void Enable();
    }
}
