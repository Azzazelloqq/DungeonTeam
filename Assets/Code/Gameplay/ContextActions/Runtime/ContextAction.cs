using System;

namespace DungeonTeam.Gameplay.ContextActions.Runtime
{
    public sealed class ContextAction
    {
        public ContextAction(string label, Action execute)
        {
            if (string.IsNullOrWhiteSpace(label))
            {
                throw new ArgumentException("Context action label cannot be empty.", nameof(label));
            }

            Label = label;
            Execute = execute ?? throw new ArgumentNullException(nameof(execute));
        }

        public string Label { get; }

        internal Action Execute { get; }
    }
}
