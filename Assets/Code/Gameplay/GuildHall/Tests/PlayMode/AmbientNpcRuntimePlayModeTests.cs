using System;
using System.Collections;
using System.Threading;
using DungeonTeam.Gameplay.AmbientNpc.Application;
using DungeonTeam.Gameplay.AmbientNpc.Runtime;
using DungeonTeam.Gameplay.AmbientNpc.Runtime.Presentation.UI.Dialogue;
using DungeonTeam.Gameplay.GuildHall.Runtime.Composition;
using NUnit.Framework;
using ResourceLoader.AddressableResourceLoader;
using UnityEngine;
using UnityEngine.TestTools;

namespace DungeonTeam.Gameplay.GuildHall.Tests.PlayMode
{
    public sealed class AmbientNpcRuntimePlayModeTests
    {
        [UnityTest]
        public IEnumerator AddressableGuildHall_AmbientNpcBindingsTickAndDispose()
        {
            var resourceLoader = new AddressableResourceLoader();
            var loader = new GuildHallWorldLoader(resourceLoader);
            var loadTask = loader.LoadAsync(CancellationToken.None);
            while (!loadTask.IsCompleted)
            {
                yield return null;
            }

            if (loadTask.IsFaulted)
            {
                throw loadTask.Exception;
            }

            var lease = loadTask.Result;
            try
            {
                lease.Activate();
                using var set = new AmbientNpcSet(
                    Snapshots(),
                    Profiles(),
                    lease.View.AmbientNpcViews,
                    lease.View.AmbientNpcVignettes);
                set.Initialize();
                var routeNpc = Find(lease.View, "npc.visitor");
                var startPosition = routeNpc.BodyTransform.position;

                set.Tick(1.1f);
                set.Tick(1f);

                Assert.That(routeNpc.BodyTransform.position, Is.Not.EqualTo(startPosition));
            }
            finally
            {
                lease.Dispose();
                resourceLoader.Dispose();
            }
        }

        [Test]
        public void DialogueViewModel_CloseIsIdempotentAndPublishesProvidedText()
        {
            var model = new DialogueModel();
            var closeCount = 0;
            var viewModel = new DialogueViewModel(model, () => closeCount++);
            viewModel.Initialize();
            model.Show("Регистратор", "Добро пожаловать");

            viewModel.Close();
            viewModel.Close();

            Assert.That(viewModel.Speaker.Value, Is.EqualTo("Регистратор"));
            Assert.That(viewModel.Line.Value, Is.EqualTo("Добро пожаловать"));
            Assert.That(viewModel.IsVisible.Value, Is.False);
            Assert.That(closeCount, Is.EqualTo(1));
            viewModel.Dispose();
        }

        [UnityTest]
        public IEnumerator AddressableDialogueView_RepeatedOpenCloseCycles_KeepOneViewBindingAlive()
        {
            var resourceLoader = new AddressableResourceLoader();
            var loader = new GuildHallWorldLoader(resourceLoader);
            var loadTask = loader.LoadAsync(CancellationToken.None);
            while (!loadTask.IsCompleted)
            {
                yield return null;
            }

            if (loadTask.IsFaulted)
            {
                throw loadTask.Exception;
            }

            var lease = loadTask.Result;
            var model = new DialogueModel();
            var closeCount = 0;
            var viewModel = new DialogueViewModel(model, () => closeCount++);
            viewModel.Initialize();
            var view = lease.View.DialogueView;
            view.Initialize(viewModel, disposeWithViewModel: false);

            try
            {
                model.Show("Регистратор", "Первая реплика");
                viewModel.Close();
                model.Show("Регистратор", "Вторая реплика");
                viewModel.Close();

                Assert.That(closeCount, Is.EqualTo(2));
                Assert.That(viewModel.Line.Value, Is.EqualTo("Вторая реплика"));
                Assert.That(viewModel.IsVisible.Value, Is.False);
            }
            finally
            {
                view.Dispose();
                viewModel.Dispose();
                lease.Dispose();
                resourceLoader.Dispose();
            }
        }

        private static AmbientNpcSnapshot[] Snapshots() => new[]
        {
            new AmbientNpcSnapshot("npc.registrar", new AmbientTextSnapshot("npc.registrar.name", "Регистратор"), "dialogue.registrar", "ambient.stationary"),
            new AmbientNpcSnapshot("npc.visitor", new AmbientTextSnapshot("npc.visitor.name", "Гость"), "dialogue.visitor", "ambient.route"),
            new AmbientNpcSnapshot("npc.debater", new AmbientTextSnapshot("npc.debater.name", "Спорщик"), "dialogue.debater", "ambient.stationary")
        };

        private static AmbientNpcProfileCatalog Profiles() => new(new[]
        {
            new AmbientNpcProfileSnapshot("ambient.stationary", 1.5f, 360f, 1f, 2f, 1f, 2f, false),
            new AmbientNpcProfileSnapshot("ambient.route", 1.6f, 360f, 1f, 2f, 1f, 2f, true)
        });

        private static DungeonTeam.Gameplay.AmbientNpc.Runtime.Presentation.Gameplay.AmbientNpc.Base.AmbientNpcViewBase Find(
            DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.Gameplay.GuildHall.Base.GuildHallViewBase view,
            string id)
        {
            for (var index = 0; index < view.AmbientNpcViews.Length; index++)
            {
                if (view.AmbientNpcViews[index].NpcId == id)
                {
                    return view.AmbientNpcViews[index];
                }
            }

            throw new InvalidOperationException($"Missing NPC view '{id}'.");
        }
    }
}
