using System;
using Code.Configuration;
using DungeonTeam.Gameplay.AmbientNpc.Application;
using UnityEngine;

namespace DungeonTeam.Gameplay.AmbientNpc.Runtime.Config
{
    [CreateAssetMenu(menuName = "DungeonTeam/Gameplay/Dialogue Config", fileName = "DialogueConfig")]
    public sealed class DialogueConfigPage : ConfigPage
    {
        [SerializeField] private DialoguePoolDefinitionConfig[] _pools = Array.Empty<DialoguePoolDefinitionConfig>();

        public DialogueCatalog CreateCatalog()
        {
            if (_pools == null)
            {
                throw new InvalidOperationException("Dialogue pools cannot be null.");
            }

            var pools = new DialoguePoolSnapshot[_pools.Length];
            for (var index = 0; index < _pools.Length; index++)
            {
                pools[index] = (_pools[index] ?? throw new InvalidOperationException(
                        $"Dialogue pool at index {index} is missing."))
                    .ToSnapshot(index);
            }

            return new DialogueCatalog(pools);
        }
    }

    [Serializable]
    public sealed class DialoguePoolDefinitionConfig
    {
        [SerializeField] private string _dialoguePoolId;
        [SerializeField] private DialogueLineDefinitionConfig[] _lines = Array.Empty<DialogueLineDefinitionConfig>();

        internal DialoguePoolSnapshot ToSnapshot(int index)
        {
            if (_lines == null)
            {
                throw new InvalidOperationException($"Dialogue pool at index {index} has null lines.");
            }

            var lines = new DialogueLineSnapshot[_lines.Length];
            for (var lineIndex = 0; lineIndex < _lines.Length; lineIndex++)
            {
                lines[lineIndex] = (_lines[lineIndex] ?? throw new InvalidOperationException(
                        $"Dialogue pool '{_dialoguePoolId}' has a missing line at index {lineIndex}."))
                    .ToSnapshot();
            }

            return new DialoguePoolSnapshot(_dialoguePoolId, lines);
        }
    }

    [Serializable]
    public sealed class DialogueLineDefinitionConfig
    {
        [SerializeField] private string _textId;
        [SerializeField, TextArea] private string _fallbackRu;

        internal DialogueLineSnapshot ToSnapshot() => new(_textId, _fallbackRu);
    }
}
