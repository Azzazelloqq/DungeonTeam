using System;
using System.Collections.Generic;
using DungeonTeam.Gameplay.Actors.Runtime;
using DungeonTeam.Gameplay.Skills.Domain;

namespace DungeonTeam.Gameplay.Skills.Runtime
{
    public sealed class SkillUseHandle
    {
        private readonly SkillUseExecution _execution;

        internal SkillUseHandle(SkillUseExecution execution)
        {
            _execution = execution ?? throw new ArgumentNullException(nameof(execution));
        }

        public SkillUsePhase Phase => _execution.Phase;
        public bool IsActive => _execution.IsActive;
        public bool HasCommitted => _execution.HasCommitted;

        public bool TryCancel()
        {
            return _execution.TryCancel();
        }
    }

    internal sealed class SkillUseExecution : IDisposable
    {
        private readonly SkillUseTimeline _timeline;
        private readonly SkillPresentationPlayer _presentation;
        private readonly SkillPresentationSequence _sequence;
        private int _presentationGroupId;
        private bool _isStarted;
        private bool _isDisposed;

        public SkillUseExecution(
            ActorInstance source,
            ActorInstance target,
            SkillDefinition skill,
            SkillLevelDefinition level,
            SkillPresentationSequence sequence,
            SkillPresentationPlayer presentation,
            IReadOnlyList<ActorInstance> areaOpponents,
            int primaryDamageBonus)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            Target = target ?? throw new ArgumentNullException(nameof(target));
            Skill = skill ?? throw new ArgumentNullException(nameof(skill));
            Level = level ?? throw new ArgumentNullException(nameof(level));
            _sequence = sequence ?? throw new ArgumentNullException(nameof(sequence));
            _presentation = presentation ?? throw new ArgumentNullException(nameof(presentation));
            PrimaryDamageBonus = primaryDamageBonus >= 0
                ? primaryDamageBonus
                : throw new ArgumentOutOfRangeException(nameof(primaryDamageBonus));
            _timeline = new SkillUseTimeline(level.UseTiming);
            if (skill is AreaDamageSkillDefinition)
            {
                if (areaOpponents == null || areaOpponents.Count == 0)
                {
                    throw new ArgumentException(
                        "Area damage execution requires at least one opponent.",
                        nameof(areaOpponents));
                }

                var copiedOpponents = new ActorInstance[areaOpponents.Count];
                var containsTarget = false;
                for (var index = 0; index < areaOpponents.Count; index++)
                {
                    var opponent = areaOpponents[index] ?? throw new ArgumentException(
                        $"Area opponent at index {index} is missing.",
                        nameof(areaOpponents));
                    copiedOpponents[index] = opponent;
                    containsTarget |= ReferenceEquals(opponent, target);
                }

                if (!containsTarget)
                {
                    throw new ArgumentException(
                        "Area opponents must contain the selected target.",
                        nameof(areaOpponents));
                }

                AreaOpponents = copiedOpponents;
                AreaCenter = source.Position;
            }
            else
            {
                AreaOpponents = Array.Empty<ActorInstance>();
            }
        }

        public ActorInstance Source { get; }
        public ActorInstance Target { get; }
        public SkillDefinition Skill { get; }
        public SkillLevelDefinition Level { get; }
        public int PrimaryDamageBonus { get; }
        public SkillUsePhase Phase => _timeline.Phase;
        public bool IsActive => !_isDisposed && _timeline.IsActive;
        public bool HasCommitted => _timeline.HasCommitted;
        public int PresentationGroupId => _presentationGroupId;
        public SkillPresentationSequence Sequence => _sequence;
        public IReadOnlyList<ActorInstance> AreaOpponents { get; }
        public UnityEngine.Vector3 AreaCenter { get; }

        public SkillUseAdvanceResult Start()
        {
            RequireNotDisposed();
            if (_isStarted)
                throw new InvalidOperationException("Skill use execution is already started.");

            _isStarted = true;
            _presentationGroupId = _presentation.Begin(_sequence, Source, Target);
            return _timeline.Advance(0f);
        }

        public SkillUseAdvanceResult Advance(float deltaTime)
        {
            RequireStarted();
            if (!_timeline.HasCommitted &&
                (!Source.IsAlive ||
                 Skill is not AreaDamageSkillDefinition && !Target.IsAlive))
            {
                TryCancel();
                return default;
            }

            return _timeline.Advance(deltaTime);
        }

        public void PlayPhase(SkillPresentationPhase phase, UnityEngine.Vector3? impactPosition = null)
        {
            RequireStarted();
            _presentation.PlayPhase(
                _presentationGroupId,
                _sequence,
                phase,
                Source,
                Target,
                impactPosition);
        }

        public bool TryCancel()
        {
            if (_isDisposed || !_isStarted || !_timeline.TryCancel())
                return false;

            _presentation.Cancel(
                _presentationGroupId,
                _sequence,
                Source,
                Target);
            return true;
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            if (_isStarted)
            {
                TryCancel();
            }

            _isDisposed = true;
        }

        private void RequireStarted()
        {
            RequireNotDisposed();
            if (!_isStarted)
                throw new InvalidOperationException("Skill use execution is not started.");
        }

        private void RequireNotDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(SkillUseExecution));
        }
    }
}
