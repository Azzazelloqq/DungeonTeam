using NUnit.Framework;

namespace Code.UI.LoadingScreen.Tests
{
    public sealed class LoadingScreenViewModelTests
    {
        [Test]
        public void SetStatusText_WithText_PublishesText()
        {
            using var viewModel = new LoadingScreenViewModel(new LoadingScreenModel());

            viewModel.SetStatusText("Preparing dungeon");

            Assert.That(viewModel.StatusText.Value, Is.EqualTo("Preparing dungeon"));
        }

        [Test]
        public void SetStatusText_WithWhitespace_RestoresDefaultText()
        {
            using var viewModel = new LoadingScreenViewModel(new LoadingScreenModel());

            viewModel.SetStatusText(" ");

            Assert.That(viewModel.StatusText.Value, Is.EqualTo("Loading..."));
        }
    }
}
