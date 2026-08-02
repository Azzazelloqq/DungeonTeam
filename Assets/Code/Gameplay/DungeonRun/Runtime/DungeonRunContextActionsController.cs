using System;
using DungeonTeam.Gameplay.ContextActions.Runtime;
using DungeonTeam.Gameplay.Team.Runtime;

namespace DungeonTeam.Gameplay.DungeonRun.Runtime
{
    internal sealed class DungeonRunContextActionsController : IDisposable
    {
        private readonly TeamController _teamController;
        private readonly ContextActionsModel _model;
        private readonly ContextAction[] _noActions = Array.Empty<ContextAction>();
        private readonly ContextAction[] _attackOnly;
        private readonly ContextAction[] _followOnly;
        private readonly ContextAction[] _attackAndFollow;

        private bool _isInitialized;
        private bool _isDisposed;

        public DungeonRunContextActionsController(
            TeamController teamController,
            ContextActionsModel model)
        {
            _teamController = teamController ?? throw new ArgumentNullException(nameof(teamController));
            _model = model ?? throw new ArgumentNullException(nameof(model));

            var attack = new ContextAction("ATTACK", ExecuteAttack);
            var follow = new ContextAction("FOLLOW", _teamController.OrderFollow);
            _attackOnly = new[] { attack };
            _followOnly = new[] { follow };
            _attackAndFollow = new[] { attack, follow };
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
            _isInitialized = true;
            Refresh();
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
                _teamController.CommandsChanged -= Refresh;
                _model.SetActions(_noActions);
            }
        }

        private void Refresh()
        {
            var actions = (_teamController.CanOrderAttack, _teamController.CanOrderFollow) switch
            {
                (true, true) => _attackAndFollow,
                (true, false) => _attackOnly,
                (false, true) => _followOnly,
                _ => _noActions
            };
            _model.SetActions(actions);
        }

        private void ExecuteAttack()
        {
            _teamController.TryOrderAttack();
        }
    }
}
