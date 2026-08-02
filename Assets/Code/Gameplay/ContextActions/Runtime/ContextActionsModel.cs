using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azzazelloqq.MVVM.ReactiveLibrary;
using DungeonTeam.Gameplay.ContextActions.Runtime.Base;

namespace DungeonTeam.Gameplay.ContextActions.Runtime
{
    public sealed class ContextActionsModel : ContextActionsModelBase
    {
        private readonly ReactiveProperty<IReadOnlyList<string>> _labels =
            new(Array.Empty<string>());
        private IReadOnlyList<ContextAction> _actions = Array.Empty<ContextAction>();

        public ContextActionsModel()
        {
            _labels.AddTo(compositeDisposable);
        }

        public override IReadOnlyReactiveProperty<IReadOnlyList<string>> Labels => _labels;

        public override void SetActions(IReadOnlyList<ContextAction> actions)
        {
            if (actions == null)
            {
                throw new ArgumentNullException(nameof(actions));
            }

            var actionSnapshot = new ContextAction[actions.Count];
            var labelSnapshot = new string[actions.Count];
            for (var index = 0; index < actions.Count; index++)
            {
                var action = actions[index] ?? throw new ArgumentException(
                    $"Context action at index {index} is missing.",
                    nameof(actions));
                actionSnapshot[index] = action;
                labelSnapshot[index] = action.Label;
            }

            _actions = actionSnapshot;
            _labels.SetValue(labelSnapshot);
        }

        public override void Execute(int index)
        {
            if (index < 0 || index >= _actions.Count)
            {
                return;
            }

            _actions[index].Execute();
        }

        protected override void OnInitialize()
        {
        }

        protected override ValueTask OnInitializeAsync(CancellationToken token)
        {
            return default;
        }

        protected override void OnDispose()
        {
            _actions = Array.Empty<ContextAction>();
        }

        protected override ValueTask OnDisposeAsync(CancellationToken token)
        {
            _actions = Array.Empty<ContextAction>();
            return default;
        }
    }
}
