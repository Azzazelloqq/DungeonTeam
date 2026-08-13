using System;
using System.Collections.Generic;
using DungeonTeam.Gameplay.Actors.Runtime;
using DungeonTeam.Gameplay.Chests.Runtime;
using DungeonTeam.Gameplay.ContextActions.Runtime;
using DungeonTeam.Gameplay.Rewards.Runtime;
using DungeonTeam.Gameplay.Team.Runtime;
using TickHandler;
using UnityEngine;

namespace DungeonTeam.Gameplay.DungeonRun.Runtime
{
    internal sealed class DungeonRunContextActionsController : IDisposable
    {
        private readonly TeamController _teamController;
        private readonly ActorInstance _leader;
        private readonly DungeonRunProgress _progress;
        private readonly IReadOnlyList<RewardPickupInstance> _rewardPickups;
        private readonly IReadOnlyList<ChestInstance> _chests;
        private readonly ITickHandler _tickHandler;
        private readonly ContextActionsModel _model;
        private readonly Action<RewardGrant> _rewardCollected;
        private readonly Action<ChestInstance> _chestOpened;
        private readonly Action _exitRequested;
        private readonly float _rewardPickupDistanceSqr;
        private readonly float _chestOpenDistanceSqr;
        private readonly float _exitDistanceSqr;
        private readonly Vector3 _exitPosition;
        private readonly List<ContextAction> _availableActions = new(4);
        private readonly ContextAction _followAction;
        private readonly ContextAction _pickupAction;
        private readonly ContextAction _openAction;
        private readonly ContextAction _exitAction;

        private RewardPickupInstance _availableRewardPickup;
        private ChestInstance _availableChest;
        private bool _isExitAvailable;
        private bool _isRunFinished;
        private bool _isInitialized;
        private bool _isDisposed;

        public DungeonRunContextActionsController(
            TeamController teamController,
            ActorInstance leader,
            DungeonRunProgress progress,
            IReadOnlyList<RewardPickupInstance> rewardPickups,
            IReadOnlyList<ChestInstance> chests,
            ITickHandler tickHandler,
            ContextActionsModel model,
            float rewardPickupDistance,
            float chestOpenDistance,
            Vector3 exitPosition,
            float exitDistance,
            Action<RewardGrant> rewardCollected,
            Action<ChestInstance> chestOpened,
            Action exitRequested)
        {
            _teamController = teamController ?? throw new ArgumentNullException(nameof(teamController));
            _leader = leader ?? throw new ArgumentNullException(nameof(leader));
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            _rewardPickups = rewardPickups ?? throw new ArgumentNullException(nameof(rewardPickups));
            _chests = chests ?? throw new ArgumentNullException(nameof(chests));
            _tickHandler = tickHandler ?? throw new ArgumentNullException(nameof(tickHandler));
            _model = model ?? throw new ArgumentNullException(nameof(model));
            if (rewardPickupDistance <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(rewardPickupDistance));
            }

            if (chestOpenDistance <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(chestOpenDistance));
            }

            if (exitDistance <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(exitDistance));
            }

            _rewardPickupDistanceSqr = rewardPickupDistance * rewardPickupDistance;
            _chestOpenDistanceSqr = chestOpenDistance * chestOpenDistance;
            _exitPosition = exitPosition;
            _exitDistanceSqr = exitDistance * exitDistance;
            _rewardCollected = rewardCollected ?? throw new ArgumentNullException(
                nameof(rewardCollected));
            _chestOpened = chestOpened ?? throw new ArgumentNullException(nameof(chestOpened));
            _exitRequested = exitRequested ?? throw new ArgumentNullException(nameof(exitRequested));

            _followAction = new ContextAction("FOLLOW", _teamController.OrderFollow);
            _pickupAction = new ContextAction("PICK UP", ExecutePickup);
            _openAction = new ContextAction("OPEN", ExecuteOpen);
            _exitAction = new ContextAction("EXIT", ExecuteExit);
        }

        public void Initialize()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(DungeonRunContextActionsController));
            }

            if (_isInitialized)
            {
                throw new InvalidOperationException(
                    "Dungeon Run context actions are already initialized.");
            }

            _teamController.CommandsChanged += Refresh;
            _tickHandler.SubscribeOnFrameUpdate(OnFrameUpdate);
            _isInitialized = true;
            RefreshAvailableRewardPickup();
            RefreshAvailableChest();
            RefreshAvailableExit();
            Refresh();
        }

        public void SetRunFinished()
        {
            if (_isRunFinished)
            {
                return;
            }

            _isRunFinished = true;
            _isExitAvailable = false;
            _model.SetActions(Array.Empty<ContextAction>());
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            if (_isInitialized)
            {
                _tickHandler.UnsubscribeOnFrameUpdate(OnFrameUpdate);
                _teamController.CommandsChanged -= Refresh;
                _model.SetActions(Array.Empty<ContextAction>());
            }

            _availableRewardPickup = null;
            _availableChest = null;
            _isExitAvailable = false;
        }

        private void Refresh()
        {
            _availableActions.Clear();
            if (_isRunFinished)
            {
                _model.SetActions(_availableActions);
                return;
            }

            if (_teamController.CanOrderFollow)
            {
                _availableActions.Add(_followAction);
            }

            if (_availableRewardPickup != null)
            {
                _availableActions.Add(_pickupAction);
            }

            if (_availableChest != null)
            {
                _availableActions.Add(_openAction);
            }

            if (_isExitAvailable)
            {
                _availableActions.Add(_exitAction);
            }

            _model.SetActions(_availableActions);
        }

        private void ExecutePickup()
        {
            var pickup = _availableRewardPickup;
            if (!_leader.IsAlive ||
                pickup == null ||
                pickup.IsCollected ||
                SqrDistanceToLeader(pickup.Position) > _rewardPickupDistanceSqr ||
                !pickup.TryCollect(out var reward))
            {
                RefreshAvailableRewardPickup();
                Refresh();
                return;
            }

            _rewardCollected(reward);
            _availableRewardPickup = null;
            RefreshAvailableRewardPickup();
            Refresh();
        }

        private void ExecuteOpen()
        {
            var chest = _availableChest;
            if (!_leader.IsAlive ||
                chest == null ||
                chest.IsOpened ||
                SqrDistanceToLeader(chest.Position) > _chestOpenDistanceSqr ||
                !chest.TryOpen())
            {
                RefreshAvailableChest();
                Refresh();
                return;
            }

            _chestOpened(chest);
            _availableChest = null;
            RefreshAvailableChest();
            RefreshAvailableRewardPickup();
            Refresh();
        }

        private void ExecuteExit()
        {
            if (_isRunFinished ||
                !_isExitAvailable ||
                !_leader.IsAlive ||
                !_progress.CanExit ||
                SqrDistanceToLeader(_exitPosition) > _exitDistanceSqr)
            {
                RefreshAvailableExit();
                Refresh();
                return;
            }

            _exitRequested();
        }

        private void OnFrameUpdate(float deltaTime)
        {
            if (_isRunFinished)
            {
                return;
            }

            var rewardAvailabilityChanged = RefreshAvailableRewardPickup();
            var chestAvailabilityChanged = RefreshAvailableChest();
            var exitAvailabilityChanged = RefreshAvailableExit();
            if (rewardAvailabilityChanged ||
                chestAvailabilityChanged ||
                exitAvailabilityChanged)
            {
                Refresh();
            }
        }

        private bool RefreshAvailableRewardPickup()
        {
            RewardPickupInstance nearest = null;
            var nearestDistanceSqr = _rewardPickupDistanceSqr;
            if (_leader.IsAlive)
            {
                for (var index = 0; index < _rewardPickups.Count; index++)
                {
                    var pickup = _rewardPickups[index];
                    if (pickup.IsCollected)
                    {
                        continue;
                    }

                    var distanceSqr = SqrDistanceToLeader(pickup.Position);
                    if (distanceSqr <= nearestDistanceSqr)
                    {
                        nearest = pickup;
                        nearestDistanceSqr = distanceSqr;
                    }
                }
            }

            if (ReferenceEquals(_availableRewardPickup, nearest))
            {
                return false;
            }

            _availableRewardPickup = nearest;
            return true;
        }

        private bool RefreshAvailableChest()
        {
            ChestInstance nearest = null;
            var nearestDistanceSqr = _chestOpenDistanceSqr;
            if (_leader.IsAlive)
            {
                for (var index = 0; index < _chests.Count; index++)
                {
                    var chest = _chests[index];
                    if (chest.IsOpened)
                    {
                        continue;
                    }

                    var distanceSqr = SqrDistanceToLeader(chest.Position);
                    if (distanceSqr <= nearestDistanceSqr)
                    {
                        nearest = chest;
                        nearestDistanceSqr = distanceSqr;
                    }
                }
            }

            if (ReferenceEquals(_availableChest, nearest))
            {
                return false;
            }

            _availableChest = nearest;
            return true;
        }

        private bool RefreshAvailableExit()
        {
            var isAvailable = _leader.IsAlive &&
                              _progress.CanExit &&
                              SqrDistanceToLeader(_exitPosition) <= _exitDistanceSqr;
            if (_isExitAvailable == isAvailable)
            {
                return false;
            }

            _isExitAvailable = isAvailable;
            return true;
        }

        private float SqrDistanceToLeader(Vector3 position)
        {
            var difference = position - _leader.Position;
            difference.y = 0f;
            return difference.sqrMagnitude;
        }
    }
}
