using System;
using System.Collections.Generic;
using DungeonTeam.Gameplay.DungeonRun.Application;

namespace DungeonTeam.DeveloperTools
{
    public sealed class DeveloperRunConsoleController
    {
        private sealed class ActorDraft
        {
            public bool IsIncluded;
            public int Level;
            public string LoadoutId;
        }

        private readonly DungeonRunLaunchPresetCatalog _presetCatalog;
        private readonly DungeonRunTeamSetup _teamSetup;
        private readonly Action<DungeonRunStartRequest> _runRequested;
        private readonly Action _stopRequested;
        private readonly Func<int> _seedGenerator;
        private readonly Dictionary<string, ActorDraft> _actorDrafts;
        private DungeonRunStartRequest _lastRun;
        private int _seed;

        public DeveloperRunConsoleController(
            DungeonRunLaunchPresetCatalog presetCatalog,
            DungeonRunTeamSetup teamSetup,
            Action<DungeonRunStartRequest> runRequested,
            Action stopRequested,
            Func<int> seedGenerator = null)
        {
            _presetCatalog = presetCatalog ?? throw new ArgumentNullException(nameof(presetCatalog));
            _teamSetup = teamSetup ?? throw new ArgumentNullException(nameof(teamSetup));
            _runRequested = runRequested ?? throw new ArgumentNullException(nameof(runRequested));
            _stopRequested = stopRequested ?? throw new ArgumentNullException(nameof(stopRequested));
            _seedGenerator = seedGenerator ?? (() => Environment.TickCount);
            _actorDrafts = new Dictionary<string, ActorDraft>(
                _teamSetup.Members.Count,
                StringComparer.Ordinal);

            for (var index = 0; index < _teamSetup.Members.Count; index++)
            {
                var member = _teamSetup.Members[index];
                _actorDrafts.Add(
                    member.ActorId,
                    new ActorDraft
                    {
                        Level = member.AvailableLevels[0],
                        LoadoutId = member.AvailableLoadoutIds[0]
                    });
            }

            Reset();
        }

        public IReadOnlyList<DungeonRunLaunchPreset> Presets => _presetCatalog.Presets;

        public IReadOnlyList<DungeonRunTeamMemberOption> Members => _teamSetup.Members;

        public string SelectedPresetId { get; private set; }

        public string LeaderActorId { get; private set; }

        public int Seed
        {
            get => _seed;
            set
            {
                _seed = value;
                ErrorMessage = string.Empty;
            }
        }

        public string ErrorMessage { get; private set; }

        public bool HasLastRun => _lastRun != null;

        public void SelectPreset(string presetId)
        {
            var preset = _presetCatalog.Require(presetId);
            SelectedPresetId = preset.PresetId;
            Seed = preset.DefaultSeed;
        }

        public bool IsActorIncluded(string actorId)
        {
            return RequireDraft(actorId).IsIncluded;
        }

        public int GetActorLevel(string actorId)
        {
            return RequireDraft(actorId).Level;
        }

        public string GetActorLoadout(string actorId)
        {
            return RequireDraft(actorId).LoadoutId;
        }

        public void SetActorIncluded(string actorId, bool included)
        {
            var draft = RequireDraft(actorId);
            draft.IsIncluded = included;
            if (!included && string.Equals(LeaderActorId, actorId, StringComparison.Ordinal))
            {
                LeaderActorId = null;
            }

            ErrorMessage = string.Empty;
        }

        public void SetLeader(string actorId)
        {
            var draft = RequireDraft(actorId);
            draft.IsIncluded = true;
            LeaderActorId = actorId;
            ErrorMessage = string.Empty;
        }

        public void SetActorLevel(string actorId, int level)
        {
            var member = RequireMember(actorId);
            if (!member.SupportsLevel(level))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(level),
                    $"Level {level} is not available for actor '{actorId}'.");
            }

            RequireDraft(actorId).Level = level;
            ErrorMessage = string.Empty;
        }

        public void SetActorLoadout(string actorId, string loadoutId)
        {
            var member = RequireMember(actorId);
            if (!member.SupportsLoadout(loadoutId))
            {
                throw new ArgumentException(
                    $"Loadout '{loadoutId}' is not available for actor '{actorId}'.",
                    nameof(loadoutId));
            }

            RequireDraft(actorId).LoadoutId = loadoutId;
            ErrorMessage = string.Empty;
        }

        public void RandomizeSeed()
        {
            Seed = _seedGenerator();
        }

        public bool TrySetSeed(string value)
        {
            if (!int.TryParse(value, out var seed))
            {
                ErrorMessage = "Seed must be a whole number.";
                return false;
            }

            Seed = seed;
            return true;
        }

        public bool Run()
        {
            if (!TryCreateRequest(out var request, out var error))
            {
                ErrorMessage = error;
                return false;
            }

            _lastRun = request;
            ErrorMessage = string.Empty;
            _runRequested(request);
            return true;
        }

        public bool RunAgain()
        {
            if (_lastRun == null)
            {
                ErrorMessage = "There is no previous developer run.";
                return false;
            }

            ErrorMessage = string.Empty;
            _runRequested(_lastRun);
            return true;
        }

        public void Stop()
        {
            _stopRequested();
        }

        public void Reset()
        {
            for (var index = 0; index < _teamSetup.Members.Count; index++)
            {
                var member = _teamSetup.Members[index];
                var draft = _actorDrafts[member.ActorId];
                draft.IsIncluded = false;
                draft.Level = member.AvailableLevels[0];
                draft.LoadoutId = member.AvailableLoadoutIds[0];
            }

            ApplySelection(_teamSetup.DefaultSelection);
            SelectedPresetId = _presetCatalog.DefaultPreset.PresetId;
            _seed = _presetCatalog.DefaultPreset.DefaultSeed;
            _lastRun = null;
            ErrorMessage = string.Empty;
        }

        private bool TryCreateRequest(out DungeonRunStartRequest request, out string error)
        {
            if (string.IsNullOrWhiteSpace(LeaderActorId))
            {
                request = null;
                error = "A leader must be selected.";
                return false;
            }

            try
            {
                var leader = CreateSelection(LeaderActorId);
                var companions = new List<DungeonRunActorSelection>();
                for (var index = 0; index < _teamSetup.Members.Count; index++)
                {
                    var actorId = _teamSetup.Members[index].ActorId;
                    if (!_actorDrafts[actorId].IsIncluded ||
                        string.Equals(actorId, LeaderActorId, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    companions.Add(CreateSelection(actorId));
                }

                var team = new DungeonRunTeamSelection(leader, companions);
                _teamSetup.RequireValid(team);
                request = _presetCatalog.CreateRequest(SelectedPresetId, _seed, team);
                error = null;
                return true;
            }
            catch (Exception exception) when (
                exception is ArgumentException || exception is KeyNotFoundException)
            {
                request = null;
                error = exception.Message;
                return false;
            }
        }

        private DungeonRunActorSelection CreateSelection(string actorId)
        {
            var draft = _actorDrafts[actorId];
            return new DungeonRunActorSelection(actorId, draft.Level, draft.LoadoutId);
        }

        private void ApplySelection(DungeonRunTeamSelection selection)
        {
            ApplySelection(selection.Leader);
            LeaderActorId = selection.Leader.ActorId;
            for (var index = 0; index < selection.Companions.Count; index++)
            {
                ApplySelection(selection.Companions[index]);
            }
        }

        private void ApplySelection(DungeonRunActorSelection selection)
        {
            var draft = RequireDraft(selection.ActorId);
            draft.IsIncluded = true;
            draft.Level = selection.Level;
            draft.LoadoutId = selection.LoadoutId;
        }

        private ActorDraft RequireDraft(string actorId)
        {
            if (actorId == null || !_actorDrafts.TryGetValue(actorId, out var draft))
            {
                throw new ArgumentException(
                    $"Actor '{actorId}' is not available in the developer run roster.",
                    nameof(actorId));
            }

            return draft;
        }

        private DungeonRunTeamMemberOption RequireMember(string actorId)
        {
            for (var index = 0; index < _teamSetup.Members.Count; index++)
            {
                var member = _teamSetup.Members[index];
                if (string.Equals(member.ActorId, actorId, StringComparison.Ordinal))
                {
                    return member;
                }
            }

            throw new ArgumentException(
                $"Actor '{actorId}' is not available in the developer run roster.",
                nameof(actorId));
        }
    }
}
