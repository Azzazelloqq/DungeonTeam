using InGameLogger;
using LightDI.Runtime;
using RootPattern;

namespace DungeonTeam.Architecture.Composition
{
    public readonly struct ApplicationContext : IRootContext
    {
        public ApplicationContext(IDiContainer container, IInGameLogger logger)
        {
            Container = container;
            Logger = logger;
        }

        public IDiContainer Container { get; }
        public IInGameLogger Logger { get; }
    }
}
