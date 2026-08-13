using System;
using Code.Configuration;
using DungeonTeam.Gameplay.DungeonRun.Application;
using UnityEngine;

namespace DungeonTeam.Gameplay.DungeonRun.Runtime
{
    [CreateAssetMenu(
        menuName = "DungeonTeam/Gameplay/Dungeon Run Launch Config",
        fileName = "DungeonRunLaunchConfig")]
    public sealed class DungeonRunLaunchConfigPage : ConfigPage
    {
        [SerializeField]
        private string _defaultPresetId;

        [SerializeField]
        private DungeonRunLaunchPresetConfig[] _presets =
            Array.Empty<DungeonRunLaunchPresetConfig>();

        public DungeonRunLaunchPresetCatalog CreateCatalog()
        {
            if (_presets == null)
            {
                throw new InvalidOperationException("Dungeon run launch presets cannot be null.");
            }

            var presets = new DungeonRunLaunchPreset[_presets.Length];
            for (var index = 0; index < _presets.Length; index++)
            {
                var preset = _presets[index] ?? throw new InvalidOperationException(
                    $"Dungeon run launch preset at index {index} is missing.");
                presets[index] = preset.ToDomain();
            }

            return new DungeonRunLaunchPresetCatalog(presets, _defaultPresetId);
        }
    }

    [Serializable]
    public sealed class DungeonRunLaunchPresetConfig
    {
        [SerializeField]
        private string _presetId;

        [SerializeField]
        private string _displayName;

        [SerializeField]
        private string _dungeonId;

        [SerializeField]
        private string _scenarioId;

        [SerializeField]
        private string _difficultyId;

        [SerializeField]
        private int _defaultSeed = 42;

        internal DungeonRunLaunchPreset ToDomain()
        {
            return new DungeonRunLaunchPreset(
                _presetId,
                _displayName,
                _dungeonId,
                _scenarioId,
                _difficultyId,
                _defaultSeed);
        }
    }
}
