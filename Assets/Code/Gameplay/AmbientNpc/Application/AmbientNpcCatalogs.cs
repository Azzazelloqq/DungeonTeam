using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DungeonTeam.Gameplay.AmbientNpc.Application
{
    public sealed class AmbientTextSnapshot
    {
        public AmbientTextSnapshot(string textId, string displayText)
        {
            TextId = AmbientNpcId.Require(textId, nameof(textId));
            if (string.IsNullOrWhiteSpace(displayText))
            {
                throw new ArgumentException("Display text cannot be empty.", nameof(displayText));
            }

            DisplayText = displayText;
        }

        public string TextId { get; }
        public string DisplayText { get; }
    }

    public sealed class AmbientNpcSnapshot
    {
        public AmbientNpcSnapshot(
            string npcId,
            AmbientTextSnapshot displayName,
            string dialoguePoolId,
            string ambientProfileId)
        {
            NpcId = AmbientNpcId.Require(npcId, nameof(npcId));
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            DialoguePoolId = AmbientNpcId.Require(dialoguePoolId, nameof(dialoguePoolId));
            AmbientProfileId = AmbientNpcId.Require(ambientProfileId, nameof(ambientProfileId));
        }

        public string NpcId { get; }
        public AmbientTextSnapshot DisplayName { get; }
        public string DialoguePoolId { get; }
        public string AmbientProfileId { get; }
    }

    public sealed class AmbientNpcProfileSnapshot
    {
        public AmbientNpcProfileSnapshot(
            string ambientProfileId,
            float movementSpeed,
            float turnSpeed,
            float idleDurationMin,
            float idleDurationMax,
            float activityDurationMin,
            float activityDurationMax,
            bool usesAuthoredRoute)
        {
            AmbientProfileId = AmbientNpcId.Require(ambientProfileId, nameof(ambientProfileId));
            if (movementSpeed <= 0f || turnSpeed <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(movementSpeed));
            }

            ValidateRange(idleDurationMin, idleDurationMax, nameof(idleDurationMin));
            ValidateRange(activityDurationMin, activityDurationMax, nameof(activityDurationMin));
            MovementSpeed = movementSpeed;
            TurnSpeed = turnSpeed;
            IdleDurationMin = idleDurationMin;
            IdleDurationMax = idleDurationMax;
            ActivityDurationMin = activityDurationMin;
            ActivityDurationMax = activityDurationMax;
            UsesAuthoredRoute = usesAuthoredRoute;
        }

        public string AmbientProfileId { get; }
        public float MovementSpeed { get; }
        public float TurnSpeed { get; }
        public float IdleDurationMin { get; }
        public float IdleDurationMax { get; }
        public float ActivityDurationMin { get; }
        public float ActivityDurationMax { get; }
        public bool UsesAuthoredRoute { get; }

        private static void ValidateRange(float minimum, float maximum, string parameterName)
        {
            if (minimum < 0f || maximum < minimum)
            {
                throw new ArgumentOutOfRangeException(parameterName);
            }
        }
    }

    public sealed class AmbientNpcProfileCatalog
    {
        private readonly IReadOnlyDictionary<string, AmbientNpcProfileSnapshot> _profiles;

        public AmbientNpcProfileCatalog(IReadOnlyList<AmbientNpcProfileSnapshot> profiles)
        {
            var snapshot = AmbientNpcSnapshotList.Copy(profiles, nameof(profiles));
            var byId = new Dictionary<string, AmbientNpcProfileSnapshot>(StringComparer.Ordinal);
            for (var index = 0; index < snapshot.Count; index++)
            {
                if (!byId.TryAdd(snapshot[index].AmbientProfileId, snapshot[index]))
                {
                    throw new ArgumentException(
                        $"Ambient profile ID '{snapshot[index].AmbientProfileId}' is duplicated.",
                        nameof(profiles));
                }
            }

            _profiles = new ReadOnlyDictionary<string, AmbientNpcProfileSnapshot>(byId);
        }

        public bool Contains(string profileId) =>
            _profiles.ContainsKey(AmbientNpcId.Require(profileId, nameof(profileId)));

        public AmbientNpcProfileSnapshot Require(string profileId)
        {
            if (!_profiles.TryGetValue(AmbientNpcId.Require(profileId, nameof(profileId)), out var profile))
            {
                throw new KeyNotFoundException($"Unknown ambient profile ID '{profileId}'.");
            }

            return profile;
        }
    }

    public sealed class DialogueLineSnapshot
    {
        public DialogueLineSnapshot(string lineId, string displayText)
        {
            LineId = AmbientNpcId.Require(lineId, nameof(lineId));
            if (string.IsNullOrWhiteSpace(displayText))
            {
                throw new ArgumentException("Dialogue text cannot be empty.", nameof(displayText));
            }

            DisplayText = displayText;
        }

        public string LineId { get; }
        public string DisplayText { get; }
    }

    public sealed class DialoguePoolSnapshot
    {
        public DialoguePoolSnapshot(string dialoguePoolId, IReadOnlyList<DialogueLineSnapshot> lines)
        {
            DialoguePoolId = AmbientNpcId.Require(dialoguePoolId, nameof(dialoguePoolId));
            Lines = AmbientNpcSnapshotList.Copy(lines, nameof(lines));
            if (Lines.Count == 0)
            {
                throw new ArgumentException("Dialogue pool requires at least one line.", nameof(lines));
            }

            var lineIds = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < Lines.Count; index++)
            {
                if (!lineIds.Add(Lines[index].LineId))
                {
                    throw new ArgumentException(
                        $"Dialogue line ID '{Lines[index].LineId}' is duplicated in pool '{DialoguePoolId}'.",
                        nameof(lines));
                }
            }
        }

        public string DialoguePoolId { get; }
        public IReadOnlyList<DialogueLineSnapshot> Lines { get; }
    }

    public sealed class DialogueCatalog
    {
        private readonly IReadOnlyDictionary<string, DialoguePoolSnapshot> _pools;

        public DialogueCatalog(IReadOnlyList<DialoguePoolSnapshot> pools)
        {
            var snapshot = AmbientNpcSnapshotList.Copy(pools, nameof(pools));
            var byId = new Dictionary<string, DialoguePoolSnapshot>(StringComparer.Ordinal);
            for (var index = 0; index < snapshot.Count; index++)
            {
                if (!byId.TryAdd(snapshot[index].DialoguePoolId, snapshot[index]))
                {
                    throw new ArgumentException(
                        $"Dialogue pool ID '{snapshot[index].DialoguePoolId}' is duplicated.",
                        nameof(pools));
                }
            }

            _pools = new ReadOnlyDictionary<string, DialoguePoolSnapshot>(byId);
        }

        public bool Contains(string dialoguePoolId) =>
            _pools.ContainsKey(AmbientNpcId.Require(dialoguePoolId, nameof(dialoguePoolId)));

        public DialoguePoolSnapshot Require(string dialoguePoolId)
        {
            if (!_pools.TryGetValue(AmbientNpcId.Require(dialoguePoolId, nameof(dialoguePoolId)), out var pool))
            {
                throw new KeyNotFoundException($"Unknown dialogue pool ID '{dialoguePoolId}'.");
            }

            return pool;
        }
    }

    public sealed class DialogueLineSelector
    {
        private readonly Random _random;

        public DialogueLineSelector(Random random)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public DialogueLineSnapshot Select(DialoguePoolSnapshot pool)
        {
            if (pool == null)
            {
                throw new ArgumentNullException(nameof(pool));
            }

            return pool.Lines[_random.Next(pool.Lines.Count)];
        }
    }

    internal static class AmbientNpcId
    {
        public static string Require(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Stable ID cannot be empty.", parameterName);
            }

            return value;
        }
    }

    internal static class AmbientNpcSnapshotList
    {
        public static IReadOnlyList<T> Copy<T>(IReadOnlyList<T> source, string parameterName)
            where T : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            var copy = new T[source.Count];
            for (var index = 0; index < source.Count; index++)
            {
                copy[index] = source[index] ?? throw new ArgumentException(
                    $"Entry at index {index} is missing.", parameterName);
            }

            return new ReadOnlyCollection<T>(copy);
        }
    }
}
