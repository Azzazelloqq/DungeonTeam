using NUnit.Framework;

namespace Code.UI.MainMenu.Tests
{
    public sealed class MainMenuViewModelTests
    {
        [Test]
        public void RequestQuitThenCancel_HidesConfirmationWithoutQuitting()
        {
            var wasQuit = false;
            using var viewModel = CreateViewModel(() => wasQuit = true);

            viewModel.RequestQuitCommand.Execute();
            viewModel.CancelQuitCommand.Execute();

            Assert.That(viewModel.IsQuitConfirmationVisible.Value, Is.False);
            Assert.That(wasQuit, Is.False);
        }

        [Test]
        public void ConfirmQuit_AfterRequest_InvokesQuitAction()
        {
            var quitCount = 0;
            using var viewModel = CreateViewModel(() => quitCount++);

            viewModel.RequestQuitCommand.Execute();
            viewModel.ConfirmQuitCommand.Execute();

            Assert.That(quitCount, Is.EqualTo(1));
        }

        [Test]
        public void ConfirmQuit_WithoutRequest_DoesNotInvokeQuitAction()
        {
            var quitCount = 0;
            using var viewModel = CreateViewModel(() => quitCount++);

            viewModel.ConfirmQuitCommand.Execute();

            Assert.That(quitCount, Is.EqualTo(0));
        }

        [Test]
        public void Play_InvokesPlayAction()
        {
            var playCount = 0;
            using var viewModel = new MainMenuViewModel(
                new MainMenuModel(),
                () => playCount++,
                () => { });

            viewModel.PlayCommand.Execute();

            Assert.That(playCount, Is.EqualTo(1));
        }

        private static MainMenuViewModel CreateViewModel(System.Action onQuit)
        {
            return new MainMenuViewModel(new MainMenuModel(), () => { }, onQuit);
        }
    }
}
