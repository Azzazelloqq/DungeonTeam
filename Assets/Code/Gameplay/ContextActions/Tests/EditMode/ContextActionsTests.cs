using NUnit.Framework;
using DungeonTeam.Gameplay.ContextActions.Runtime;
using UnityEngine;
using UnityEngine.UI;

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
                new ContextAction("FOLLOW", () => { }),
                new ContextAction("PICK UP", () => { })
            });

            Assert.That(model.Labels.Value, Is.EqualTo(new[] { "FOLLOW", "PICK UP" }));
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
                new ContextAction("OPEN", () => executions++)
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
                new ContextAction("EXIT", () => executions++)
            });

            viewModel.ExecuteCommand.Execute(0);

            Assert.That(executions, Is.EqualTo(1));
            viewModel.Dispose();
        }

        [Test]
        public void View_WithAvailableActions_CreatesCompactTouchTargetGrid()
        {
            var viewObject = new GameObject(
                "ContextActionsTestView",
                typeof(RectTransform),
                typeof(ContextActionsView));
            var model = new ContextActionsModel();
            var viewModel = new ContextActionsViewModel(model);
            viewModel.Initialize();
            var view = viewObject.GetComponent<ContextActionsView>();
            view.Initialize(viewModel, disposeWithViewModel: false);

            try
            {
                model.SetActions(new[]
                {
                    new ContextAction("FOLLOW", () => { }),
                    new ContextAction("OPEN", () => { }),
                    new ContextAction("EXIT", () => { })
                });

                var panel = view.transform.Find("Actions");
                var layout = panel.GetComponent<GridLayoutGroup>();
                Assert.That(layout, Is.Not.Null);
                Assert.That(layout.constraint, Is.EqualTo(
                    GridLayoutGroup.Constraint.FixedColumnCount));
                Assert.That(layout.constraintCount, Is.EqualTo(2));
                Assert.That(layout.cellSize.x, Is.GreaterThanOrEqualTo(112f));
                Assert.That(layout.cellSize.y, Is.GreaterThanOrEqualTo(112f));
                Assert.That(panel.childCount, Is.EqualTo(3));
            }
            finally
            {
                view.Dispose();
                viewModel.Dispose();
                UnityEngine.Object.DestroyImmediate(viewObject);
            }
        }
    }
}
