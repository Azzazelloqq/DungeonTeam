using System;
using System.Collections.Generic;
using DungeonTeam.Gameplay.GuildHall.Application;
using DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.UI.NoticeBoard;
using NUnit.Framework;

namespace DungeonTeam.Gameplay.GuildHall.Tests.EditMode
{
    public sealed class NoticeBoardViewModelTests
    {
        [TestCase(1)]
        [TestCase(4)]
        public void ViewModel_VariableOfferCount_PreservesInputOrder(int count)
        {
            var offers = CreateOffers(count);
            var viewModel = CreateViewModel(offers, null, _ => { }, () => { });

            Assert.That(viewModel.Items, Has.Count.EqualTo(count));
            for (var index = 0; index < count; index++)
            {
                Assert.That(viewModel.Items[index].ContractId, Is.EqualTo(offers[index].ContractId));
            }

            viewModel.Dispose();
        }

        [Test]
        public void Model_EmptyOffers_IsAllowedAndKeepsConfiguredEmptyText()
        {
            var model = new NoticeBoardModel(
                Array.Empty<NoticeBoardOfferSnapshot>(),
                null,
                CreateText());
            var viewModel = new NoticeBoardViewModel(model, _ => { }, () => { });
            viewModel.Initialize();

            Assert.That(viewModel.Items, Is.Empty);
            Assert.That(viewModel.Text.Empty.DisplayText, Is.EqualTo("Нет доступных контрактов"));

            viewModel.Dispose();
        }

        [Test]
        public void Select_AvailableOffer_PublishesExactIdOnceAndUpdatesAllSelectedStates()
        {
            var offers = new[]
            {
                CreateOffer("contract.first", true),
                CreateOffer("contract.second", true),
                CreateOffer("contract.locked", false)
            };
            var selectedIds = new List<string>();
            NoticeBoardViewModel viewModel = null;
            viewModel = CreateViewModel(offers, "contract.first", contractId =>
            {
                Assert.That(contractId, Is.EqualTo("contract.second"));
                Assert.That(viewModel.Items[0].IsSelected.Value, Is.True);
                Assert.That(viewModel.Items[1].IsSelected.Value, Is.False);
                selectedIds.Add(contractId);
            }, () => { });

            viewModel.Items[1].SelectCommand.Execute(null);
            viewModel.Items[1].SelectCommand.Execute(null);
            viewModel.Items[2].SelectCommand.Execute(null);

            Assert.That(selectedIds, Is.EqualTo(new[] { "contract.second" }));
            Assert.That(viewModel.Items[0].IsSelected.Value, Is.False);
            Assert.That(viewModel.Items[1].IsSelected.Value, Is.True);
            Assert.That(viewModel.Items[2].IsSelected.Value, Is.False);

            viewModel.Dispose();
        }

        [Test]
        public void Close_RepeatedCommand_OnlyPublishesWhileVisible()
        {
            var closeCount = 0;
            var model = new NoticeBoardModel(CreateOffers(2), null, CreateText());
            var viewModel = new NoticeBoardViewModel(model, _ => { }, () =>
            {
                closeCount++;
                model.Hide();
            });
            viewModel.Initialize();
            model.Show();

            viewModel.CloseCommand.Execute(null);
            viewModel.CloseCommand.Execute(null);

            Assert.That(closeCount, Is.EqualTo(1));
            Assert.That(model.IsVisible.Value, Is.False);
            viewModel.Dispose();
        }

        [Test]
        public void Model_UnknownOrDisabledOffer_DoesNotChangeSelection()
        {
            var model = new NoticeBoardModel(
                new[] { CreateOffer("contract.locked", false) },
                null,
                CreateText());

            Assert.That(model.TrySelect("contract.missing"), Is.False);
            Assert.That(model.TrySelect("contract.locked"), Is.False);
            Assert.That(model.SelectedContractId.Value, Is.Null);
            model.Dispose();
        }

        private static NoticeBoardViewModel CreateViewModel(
            IReadOnlyList<NoticeBoardOfferSnapshot> offers,
            string selectedContractId,
            Action<string> contractSelected,
            Action closed)
        {
            var viewModel = new NoticeBoardViewModel(
                new NoticeBoardModel(offers, selectedContractId, CreateText()),
                contractSelected,
                closed);
            viewModel.Initialize();
            return viewModel;
        }

        private static NoticeBoardOfferSnapshot[] CreateOffers(int count)
        {
            var offers = new NoticeBoardOfferSnapshot[count];
            for (var index = 0; index < count; index++)
            {
                offers[index] = CreateOffer($"contract.{index}", index % 2 == 0);
            }

            return offers;
        }

        private static NoticeBoardOfferSnapshot CreateOffer(string contractId, bool isAvailable)
        {
            return new NoticeBoardOfferSnapshot(
                contractId,
                new GuildTextSnapshot($"{contractId}.title", contractId),
                new GuildTextSnapshot($"{contractId}.summary", "Описание"),
                "location.test",
                isAvailable,
                isAvailable ? null : new GuildTextSnapshot(
                    $"{contractId}.reason", "Недоступно"));
        }

        private static NoticeBoardTextSnapshot CreateText()
        {
            return new NoticeBoardTextSnapshot(
                new GuildTextSnapshot("notice.header", "Контракты"),
                new GuildTextSnapshot("notice.select", "Выбрать"),
                new GuildTextSnapshot("notice.selected", "Выбрано"),
                new GuildTextSnapshot("notice.close", "Закрыть"),
                new GuildTextSnapshot("notice.empty", "Нет доступных контрактов"));
        }
    }
}
