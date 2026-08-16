using System;
using UnityEngine;

namespace DungeonTeam.Gameplay.GuildHall.Runtime.Input
{
    public interface IGuildHallInput : IDisposable
    {
        Vector2 Movement { get; }
        void Enable();
    }
}
