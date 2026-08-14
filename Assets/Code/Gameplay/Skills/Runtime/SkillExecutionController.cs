using System;
using System.Collections.Generic;
using DungeonTeam.Gameplay.Actors.Domain;
using DungeonTeam.Gameplay.Actors.Runtime;
using DungeonTeam.Gameplay.Skills.Domain;
using TickHandler;
using UnityEngine;

namespace DungeonTeam.Gameplay.Skills.Runtime
{
    public sealed class SkillExecutionController : IDisposable
    {
        private readonly SkillViewSet _views;
        private readonly ITickHandler _tickHandler;
        private readonly Transform _projectileParent;
        private readonly SkillProjectileFactory _projectileFactory = new();
        private readonly SkillPresentationPlayer _presentation;
        private readonly List<SkillUseExecution> _executions = new();
        private readonly List<SkillProjectileInstance> _projectiles = new();
        private bool _isInitialized;
        private bool _isDisposed;

        public SkillExecutionController(
            SkillViewSet views,
            ITickHandler tickHandler,
            Transform projectileParent)
        {
            _views = views ?? throw new ArgumentNullException(nameof(views));
            _tickHandler = tickHandler ?? throw new ArgumentNullException(nameof(tickHandler));
            _projectileParent = projectileParent != null
                ? projectileParent
                : throw new ArgumentNullException(nameof(projectileParent));
            _presentation = new SkillPresentationPlayer(_projectileParent);
        }

        public int ActiveExecutionCount
        {
            get
            {
                var count = 0;
                for (var index = 0; index < _executions.Count; index++)
                {
                    if (_executions[index].IsActive)
                        count++;
                }

                return count;
            }
        }

        public int ActiveProjectileCount => _projectiles.Count;
        public int ActivePresentationVfxCount => _presentation.ActiveVfxCount;

        public void Initialize()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(SkillExecutionController));
            if (_isInitialized)
                throw new InvalidOperationException("Skill execution is already initialized.");

            _tickHandler.SubscribeOnFrameUpdate(OnFrameUpdate);
            _isInitialized = true;
        }

        public SkillUseHandle Begin(
            ActorInstance source,
            ActorInstance target,
            SkillDefinition skill,
            SkillLevelDefinition level)
        {
            if (skill is AreaDamageSkillDefinition)
            {
                throw new InvalidOperationException(
                    $"Area damage skill '{skill.SkillId}' requires area opponents.");
            }

            return BeginInternal(source, target, skill, level, areaOpponents: null);
        }

        public SkillUseHandle BeginArea(
            ActorInstance source,
            ActorInstance target,
            IReadOnlyList<ActorInstance> opponents,
            AreaDamageSkillDefinition skill,
            AreaDamageSkillLevelDefinition level)
        {
            if (opponents == null)
                throw new ArgumentNullException(nameof(opponents));

            return BeginInternal(source, target, skill, level, opponents);
        }

        private SkillUseHandle BeginInternal(
            ActorInstance source,
            ActorInstance target,
            SkillDefinition skill,
            SkillLevelDefinition level,
            IReadOnlyList<ActorInstance> areaOpponents)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(SkillExecutionController));
            if (!_isInitialized)
                throw new InvalidOperationException("Skill execution is not initialized.");
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (skill == null) throw new ArgumentNullException(nameof(skill));
            if (level == null) throw new ArgumentNullException(nameof(level));

            ValidateMechanic(skill, level);
            var sequence = _views.RequirePresentation(skill.SkillId);
            if (level is AreaDamageSkillLevelDefinition areaLevel)
            {
                ValidateAreaTelegraph(skill, areaLevel, sequence);
            }

            var execution = new SkillUseExecution(
                source,
                target,
                skill,
                level,
                sequence,
                _presentation,
                areaOpponents);
            var handle = new SkillUseHandle(execution);
            _executions.Add(execution);
            try
            {
                ProcessAdvance(execution, execution.Start());
                RemoveIfTerminal(execution);
                return handle;
            }
            catch
            {
                _executions.Remove(execution);
                execution.Dispose();
                throw;
            }
        }

        public void Execute(
            ActorInstance source,
            ActorInstance target,
            SkillDefinition skill,
            SkillLevelDefinition level)
        {
            Begin(source, target, skill, level);
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            if (_isInitialized)
            {
                _tickHandler.UnsubscribeOnFrameUpdate(OnFrameUpdate);
            }

            for (var index = _executions.Count - 1; index >= 0; index--)
            {
                _executions[index].Dispose();
            }

            _executions.Clear();
            for (var index = _projectiles.Count - 1; index >= 0; index--)
            {
                _projectiles[index].Dispose();
            }

            _projectiles.Clear();
            _presentation.Dispose();
        }

        private void OnFrameUpdate(float deltaTime)
        {
            _presentation.Tick(deltaTime);
            TickProjectiles(deltaTime);
            for (var index = _executions.Count - 1; index >= 0; index--)
            {
                var execution = _executions[index];
                ProcessAdvance(execution, execution.Advance(deltaTime));
                if (execution.IsActive)
                    continue;

                execution.Dispose();
                _executions.RemoveAt(index);
            }
        }

        private void ProcessAdvance(
            SkillUseExecution execution,
            SkillUseAdvanceResult result)
        {
            if (result.Committed)
            {
                execution.PlayPhase(SkillPresentationPhase.Commit);
                Commit(execution);
            }

            if (result.Completed)
            {
                execution.PlayPhase(SkillPresentationPhase.Complete);
            }
        }

        private void Commit(SkillUseExecution execution)
        {
            var skill = execution.Skill;
            var level = execution.Level;
            switch (skill)
            {
                case DirectDamageSkillDefinition:
                    if (level is not DirectDamageSkillLevelDefinition directLevel)
                    {
                        throw LevelTypeMismatch(skill, level);
                    }

                    execution.Target.ApplyDamage(directLevel.Damage, execution.Source);
                    var hitAnchor = execution.Target.HitVfxAnchor;
                    execution.PlayPhase(
                        SkillPresentationPhase.Impact,
                        hitAnchor != null
                            ? hitAnchor.position
                            : execution.Target.Position + Vector3.up);
                    break;
                case AreaDamageSkillDefinition:
                    if (level is not AreaDamageSkillLevelDefinition areaLevel)
                    {
                        throw LevelTypeMismatch(skill, level);
                    }

                    var radiusSqr = areaLevel.Radius * areaLevel.Radius;
                    var opponents = execution.AreaOpponents;
                    for (var index = 0; index < opponents.Count; index++)
                    {
                        var opponent = opponents[index];
                        if (opponent == null ||
                            ReferenceEquals(opponent, execution.Source) ||
                            !opponent.IsAlive ||
                            PlanarSqrDistance(opponent.Position, execution.AreaCenter) > radiusSqr)
                        {
                            continue;
                        }

                        opponent.ApplyDamage(areaLevel.Damage, execution.Source);
                    }

                    execution.PlayPhase(
                        SkillPresentationPhase.Impact,
                        execution.AreaCenter);
                    break;
                case ProjectileDamageSkillDefinition:
                    if (level is not ProjectileDamageSkillLevelDefinition projectileLevel)
                    {
                        throw LevelTypeMismatch(skill, level);
                    }

                    var projectile = _projectileFactory.Create(
                        skill.SkillId,
                        projectileLevel,
                        execution.Source,
                        execution.Target,
                        _views,
                        _projectileParent,
                        impactPosition => _presentation.PlayPhase(
                            execution.PresentationGroupId,
                            execution.Sequence,
                            SkillPresentationPhase.Impact,
                            execution.Source,
                            execution.Target,
                            impactPosition));
                    _projectiles.Add(projectile);
                    break;
                case DirectHealSkillDefinition:
                    if (level is not DirectHealSkillLevelDefinition healLevel)
                    {
                        throw LevelTypeMismatch(skill, level);
                    }

                    if (execution.Target.ApplyHeal(healLevel.HealAmount) ==
                        ActorHealResult.Healed)
                    {
                        var healAnchor = execution.Target.HitVfxAnchor;
                        execution.PlayPhase(
                            SkillPresentationPhase.Impact,
                            healAnchor != null
                                ? healAnchor.position
                                : execution.Target.Position + Vector3.up);
                    }

                    break;
                default:
                    throw new InvalidOperationException(
                        $"Skill '{skill.SkillId}' uses unsupported mechanic type " +
                        $"'{skill.GetType().Name}'.");
            }
        }

        private void RemoveIfTerminal(SkillUseExecution execution)
        {
            if (execution.IsActive)
                return;

            _executions.Remove(execution);
            execution.Dispose();
        }

        private void TickProjectiles(float deltaTime)
        {
            for (var index = _projectiles.Count - 1; index >= 0; index--)
            {
                var projectile = _projectiles[index];
                projectile.Tick(deltaTime);
                if (!projectile.IsCompleted)
                    continue;

                projectile.Dispose();
                _projectiles.RemoveAt(index);
            }
        }

        private static void ValidateMechanic(
            SkillDefinition skill,
            SkillLevelDefinition level)
        {
            switch (skill)
            {
                case DirectDamageSkillDefinition when level is DirectDamageSkillLevelDefinition:
                case AreaDamageSkillDefinition when level is AreaDamageSkillLevelDefinition:
                case ProjectileDamageSkillDefinition when level is ProjectileDamageSkillLevelDefinition:
                case DirectHealSkillDefinition when level is DirectHealSkillLevelDefinition:
                    return;
                case DirectDamageSkillDefinition:
                case AreaDamageSkillDefinition:
                case ProjectileDamageSkillDefinition:
                case DirectHealSkillDefinition:
                    throw LevelTypeMismatch(skill, level);
                default:
                    throw new InvalidOperationException(
                        $"Skill '{skill.SkillId}' uses unsupported mechanic type " +
                        $"'{skill.GetType().Name}'.");
            }
        }

        private static InvalidOperationException LevelTypeMismatch(
            SkillDefinition skill,
            SkillLevelDefinition level)
        {
            return new InvalidOperationException(
                $"Skill '{skill.SkillId}' cannot use level type '{level.GetType().Name}'.");
        }

        private static void ValidateAreaTelegraph(
            SkillDefinition skill,
            AreaDamageSkillLevelDefinition level,
            SkillPresentationSequence sequence)
        {
            for (var index = 0; index < sequence.VfxCues.Count; index++)
            {
                var cue = sequence.VfxCues[index];
                if (cue.Phase == SkillPresentationPhase.Start &&
                    cue.Delay == 0f &&
                    cue.Lifetime >= level.UseTiming.CommitDelay &&
                    cue.Anchor == SkillVfxAnchor.SourceOrigin &&
                    cue.FollowAnchor)
                {
                    return;
                }
            }

            throw new InvalidOperationException(
                $"Area damage skill '{skill.SkillId}' requires an immediate source-centered " +
                "telegraph that lasts through its pre-commit window.");
        }

        private static float PlanarSqrDistance(Vector3 first, Vector3 second)
        {
            var difference = first - second;
            difference.y = 0f;
            return difference.sqrMagnitude;
        }
    }
}
