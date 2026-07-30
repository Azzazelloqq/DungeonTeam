using RootPattern;

namespace DungeonTeam.Architecture.Composition
{
    /// <summary>
    /// Owns application-lifetime services. Gameplay state belongs to scene and feature roots.
    /// </summary>
    public sealed class ApplicationRoot : Root<ApplicationContext>
    {
        public ApplicationRoot(ApplicationContext context) : base(context)
        {
        }

        protected override void OnInitialize()
        {
            Context.Logger.Log("Application root initialized.");
        }

        protected override void OnDispose()
        {
            Context.Container.Dispose();
        }
    }
}
