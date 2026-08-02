using NUnit.Framework;
using DungeonTeam.Gameplay.ContextActions.Runtime;

namespace DungeonTeam.Gameplay.ContextActions.Tests
{
    public sealed class ContextActionsTests
    {
        [Test]
        public void SetActions_PublishesLabelsInProvidedOrder()
        {
            var model = new ContextActionsModel();
            var viewModel = new ContextActionsViewModel(model);
            viewModel.Initialize();

            model.SetActions(new[]
            {
                new ContextAction("ATTACK", () => { }),
                new ContextAction("FOLLOW", () => { })
            });

            Assert.That(model.Labels.Value, Is.EqualTo(new[] { "ATTACK", "FOLLOW" }));
            viewModel.Dispose();
        }

        [Test]
        public void Execute_InvokesActionAtRequestedIndex()
        {
            var executions = 0;
            var model = new ContextActionsModel();
            var viewModel = new ContextActionsViewModel(model);
            viewModel.Initialize();
            model.SetActions(new[]
            {
                new ContextAction("ATTACK", () => executions++)
            });

            model.Execute(0);

            Assert.That(executions, Is.EqualTo(1));
            viewModel.Dispose();
        }

        [Test]
        public void ViewModelCommand_ExecutesCurrentModelAction()
        {
            var executions = 0;
            var model = new ContextActionsModel();
            var viewModel = new ContextActionsViewModel(model);
            viewModel.Initialize();
            model.SetActions(new[]
            {
                new ContextAction("ATTACK", () => executions++)
            });

            viewModel.ExecuteCommand.Execute(0);

            Assert.That(executions, Is.EqualTo(1));
            viewModel.Dispose();
        }
    }
}
