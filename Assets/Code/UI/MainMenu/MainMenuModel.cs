using System.Threading;
using System.Threading.Tasks;

namespace Code.UI.MainMenu
{
    public sealed class MainMenuModel : MainMenuModelBase
    {
        protected override void OnInitialize()
        {
        }

        protected override ValueTask OnInitializeAsync(CancellationToken token)
        {
            return default;
        }

        protected override void OnDispose()
        {
        }

        protected override ValueTask OnDisposeAsync(CancellationToken token)
        {
            return default;
        }
    }
}
