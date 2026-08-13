using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DungeonTeam.Gameplay.Dungeon.Application;

namespace DungeonTeam.Gameplay.DungeonRun.Application
{
    public sealed class DungeonRunLaunchPreset
    {
        public DungeonRunLaunchPreset(
            string presetId,
            string displayName,
            string dungeonId,
            string scenarioId,
            string difficultyId,
            int defaultSeed)
        {
            PresetId = RequireValue(presetId, nameof(presetId));
            DisplayName = RequireValue(displayName, nameof(displayName));
            DungeonId = RequireValue(dungeonId, nameof(dungeonId));
            ScenarioId = RequireValue(scenarioId, nameof(scenarioId));
            DifficultyId = RequireValue(difficultyId, nameof(difficultyId));
            DefaultSeed = defaultSeed;
        }

        public string PresetId { get; }

        public string DisplayName { get; }

        public string DungeonId { get; }

        public string ScenarioId { get; }

        public string DifficultyId { get; }

        public int DefaultSeed { get; }

        private static string RequireValue(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be empty.", parameterName);
            }

            return value;
        }
    }

    public sealed class DungeonRunLaunchPresetCatalog
    {
        private readonly ReadOnlyCollection<DungeonRunLaunchPreset> _presets;
        private readonly Dictionary<string, DungeonRunLaunchPreset> _presetsById;

        public DungeonRunLaunchPresetCatalog(
            IReadOnlyList<DungeonRunLaunchPreset> presets,
            string defaultPresetId)
        {
            if (presets == null)
            {
                throw new ArgumentNullException(nameof(presets));
            }

            if (presets.Count == 0)
            {
                throw new ArgumentException(
                    "At least one dungeon run launch preset is required.",
                    nameof(presets));
            }

            if (string.IsNullOrWhiteSpace(defaultPresetId))
            {
                throw new ArgumentException(
                    "Default preset ID cannot be empty.",
                    nameof(defaultPresetId));
            }

            var copiedPresets = new DungeonRunLaunchPreset[presets.Count];
            _presetsById = new Dictionary<string, DungeonRunLaunchPreset>(
                presets.Count,
                StringComparer.Ordinal);
            for (var index = 0; index < presets.Count; index++)
            {
                var preset = presets[index] ?? throw new ArgumentException(
                    $"Launch preset at index {index} is missing.",
                    nameof(presets));
                if (!_presetsById.TryAdd(preset.PresetId, preset))
                {
                    throw new ArgumentException(
                        $"Launch preset ID '{preset.PresetId}' is configured more than once.",
                        nameof(presets));
                }

                copiedPresets[index] = preset;
            }

            if (!_presetsById.TryGetValue(defaultPresetId, out var defaultPreset))
            {
                throw new ArgumentException(
                    $"Default launch preset ID '{defaultPresetId}' is not configured.",
                    nameof(defaultPresetId));
            }

            _presets = Array.AsReadOnly(copiedPresets);
            DefaultPreset = defaultPreset;
        }

        public IReadOnlyList<DungeonRunLaunchPreset> Presets => _presets;

        public DungeonRunLaunchPreset DefaultPreset { get; }

        public DungeonRunLaunchPreset Require(string presetId)
        {
            if (string.IsNullOrWhiteSpace(presetId))
            {
                throw new ArgumentException("Preset ID cannot be empty.", nameof(presetId));
            }

            if (!_presetsById.TryGetValue(presetId, out var preset))
            {
                throw new KeyNotFoundException(
                    $"Dungeon run launch preset '{presetId}' is not configured.");
            }

            return preset;
        }

        public DungeonRunStartRequest CreateRequest(
            string presetId,
            int? seedOverride,
            DungeonRunTeamSelection team)
        {
            var preset = Require(presetId);
            return new DungeonRunStartRequest(
                new DungeonBuildRequest(
                    preset.DungeonId,
                    preset.ScenarioId,
                    preset.DifficultyId,
                    seedOverride ?? preset.DefaultSeed),
                team);
        }
    }
}
