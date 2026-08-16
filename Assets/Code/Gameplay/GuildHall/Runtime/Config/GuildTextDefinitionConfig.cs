using System;
using DungeonTeam.Gameplay.GuildHall.Application;
using UnityEngine;

namespace DungeonTeam.Gameplay.GuildHall.Runtime.Config
{
    [Serializable]
    public sealed class GuildTextDefinitionConfig
    {
        [SerializeField]
        private string _textId;

        [SerializeField, TextArea]
        private string _fallbackRu;

        internal GuildTextSnapshot ToSnapshot(string location)
        {
            try
            {
                return new GuildTextSnapshot(_textId, _fallbackRu);
            }
            catch (ArgumentException exception)
            {
                throw new InvalidOperationException($"{location}: {exception.Message}", exception);
            }
        }
    }
}
