using DungeonTeam.Architecture.Composition;
using InGameLogger;
using LightDI.Runtime;
using RootPattern;
using UnityEngine;

namespace DungeonTeam.Bootstrap
{
    /// <summary>
    /// The sole Unity entry point. Place it in the bootstrap scene.
    /// </summary>
    public sealed class GameBootstrapper : RootBehaviour
    {
        protected override IRoot CreateRoot()
        {
            var container = DiContainerFactory.CreateGlobalContainer();
            var logger = new UnityInGameLogger();
            container.RegisterAsSingleton<IInGameLogger>(logger);

            return new ApplicationRoot(new ApplicationContext(container, logger));
        }

        private void Awake()
        {
            InitializeRoot();
        }
    }
}
