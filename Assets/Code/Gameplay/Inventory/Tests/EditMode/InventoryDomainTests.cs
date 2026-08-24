using System;
using DungeonTeam.Gameplay.Inventory.Domain;
using NUnit.Framework;

namespace DungeonTeam.Gameplay.Inventory.Tests.EditMode
{
    public sealed class InventoryDomainTests
    {
        [Test]
        public void Equip_ReplacesOnlyTargetSlotAndLeavesInstanceOwned()
        {
            var state = CreateState();
            var equipped = state.Equip("hero-a", "blade", EquipmentSlot.Weapon);
            var replaced = equipped.Equip("hero-a", "blade-2", EquipmentSlot.Weapon);

            Assert.That(replaced.TryGetEquipment("hero-a", out var equipment), Is.True);
            Assert.That(equipment.WeaponInstanceId, Is.EqualTo("blade-2"));
            Assert.That(equipment.ArmorInstanceId, Is.Null);
            Assert.That(replaced.UniqueItems.Count, Is.EqualTo(2));
        }

        [Test]
        public void Equip_RejectsUnknownAndTransfersAlreadyEquippedInstance()
        {
            var state = CreateState().Equip("hero-a", "blade", EquipmentSlot.Weapon);
            Assert.Throws<ArgumentException>(() => state.Equip("hero-a", "missing", EquipmentSlot.Weapon));

            var transferred = state.Equip("hero-b", "blade", EquipmentSlot.Weapon);
            Assert.That(transferred.TryGetEquipment("hero-a", out var previous), Is.True);
            Assert.That(previous.WeaponInstanceId, Is.Null);
            Assert.That(transferred.TryGetEquipment("hero-b", out var target), Is.True);
            Assert.That(target.WeaponInstanceId, Is.EqualTo("blade"));
        }

        [Test]
        public void Unequip_RemovesCurrentAssignmentWithoutChangingOwnership()
        {
            var state = CreateState().Equip("hero-a", "blade", EquipmentSlot.Weapon);
            var unequipped = state.Unequip("hero-a", EquipmentSlot.Weapon);

            Assert.That(unequipped.TryGetEquipment("hero-a", out var equipment), Is.True);
            Assert.That(equipment.WeaponInstanceId, Is.Null);
            Assert.That(unequipped.ContainsInstance("blade"), Is.True);
        }

        [Test]
        public void Constructor_RejectsDuplicateResourceAndEquipmentReferences()
        {
            Assert.Throws<ArgumentException>(() => new InventoryState(
                new[] { new ItemInstanceState("one", "item") },
                new[] { new ResourceStackState("crystal", 1), new ResourceStackState("crystal", 2) },
                Array.Empty<HeroEquipmentState>()));
        }

        private static InventoryState CreateState() => new(
            new[]
            {
                new ItemInstanceState("blade", "weapon"),
                new ItemInstanceState("blade-2", "weapon")
            },
            new[] { new ResourceStackState("crystal", 3) },
            new[] { new HeroEquipmentState("hero-a"), new HeroEquipmentState("hero-b") });
    }
}
