using System;
using DungeonTeam.Gameplay.Inventory.Application;
using DungeonTeam.Gameplay.Inventory.Domain;
using NUnit.Framework;

namespace DungeonTeam.Gameplay.Inventory.Tests.EditMode
{
    public sealed class InventoryApplicationTests
    {
        [Test]
        public void Resolver_MapsConcreteEffectsToTheirDocumentedStats()
        {
            var catalog = new ItemCatalog(
                new EquipmentItemDefinition[]
                {
                    new("blade", "Blade", 1, EquipmentSlot.Weapon, EquipmentEffectKind.PrimaryDamage, 4, new[] { "hero" }),
                    new("coat", "Coat", 1, EquipmentSlot.Armor, EquipmentEffectKind.MaximumHealth, 20, new[] { "hero" }),
                    new("charm", "Charm", 1, EquipmentSlot.Relic, EquipmentEffectKind.MovementSpeed, 0.5f, new[] { "hero" })
                },
                new[] { new ResourceItemDefinition("crystal", "Crystal", 1) });
            var inventory = new InventoryState(
                new[]
                {
                    new ItemInstanceState("i1", "blade"),
                    new ItemInstanceState("i2", "coat"),
                    new ItemInstanceState("i3", "charm")
                },
                Array.Empty<ResourceStackState>(),
                new[] { new HeroEquipmentState("hero") })
                .Equip("hero", "i1", EquipmentSlot.Weapon)
                .Equip("hero", "i2", EquipmentSlot.Armor)
                .Equip("hero", "i3", EquipmentSlot.Relic);

            var result = new EquipmentEffectResolver(catalog).Resolve(inventory, "hero");

            Assert.That(result.PrimaryDamageBonus, Is.EqualTo(4));
            Assert.That(result.MaximumHealthBonus, Is.EqualTo(20));
            Assert.That(result.MovementSpeedBonus, Is.EqualTo(0.5f));
        }

        [Test]
        public void Resolver_RejectsDefinitionIncompatibleWithSlotOrActor()
        {
            var catalog = new ItemCatalog(
                new[] { new EquipmentItemDefinition("blade", "Blade", 1, EquipmentSlot.Weapon, EquipmentEffectKind.PrimaryDamage, 4, new[] { "hero" }) },
                Array.Empty<ResourceItemDefinition>());
            var inventory = new InventoryState(
                new[] { new ItemInstanceState("i1", "blade") },
                Array.Empty<ResourceStackState>(),
                new[] { new HeroEquipmentState("hero") })
                .Equip("hero", "i1", EquipmentSlot.Weapon);

            Assert.That(new EquipmentEffectResolver(catalog).Resolve(inventory, "hero").PrimaryDamageBonus, Is.EqualTo(4));
            Assert.Throws<ArgumentException>(() => new EquipmentEffectResolver(catalog).Resolve(inventory, "other"));
        }

        [Test]
        public void Catalog_RejectsDefinitionWithIncompatibleEffectAndSlot()
        {
            Assert.Throws<ArgumentException>(() => new ItemCatalog(
                new[] { new EquipmentItemDefinition("blade", "Blade", 1, EquipmentSlot.Armor, EquipmentEffectKind.PrimaryDamage, 4, new[] { "hero" }) },
                Array.Empty<ResourceItemDefinition>()));
        }

        [Test]
        public void Definition_RejectsFractionalIntegerEffectValue()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new EquipmentItemDefinition(
                "blade", "Blade", 1, EquipmentSlot.Weapon, EquipmentEffectKind.PrimaryDamage, 4.5f, new[] { "hero" }));
        }

        [Test]
        public void Resolver_RejectsUnknownUnEquippedDefinition()
        {
            var catalog = new ItemCatalog(
                new[] { new EquipmentItemDefinition("blade", "Blade", 1, EquipmentSlot.Weapon, EquipmentEffectKind.PrimaryDamage, 4, new[] { "hero" }) },
                Array.Empty<ResourceItemDefinition>());
            var inventory = new InventoryState(
                new[] { new ItemInstanceState("i1", "removed.definition") },
                Array.Empty<ResourceStackState>(),
                new[] { new HeroEquipmentState("hero") });

            Assert.Throws<InvalidOperationException>(() => new EquipmentEffectResolver(catalog).ValidateInventory(inventory));
        }
    }
}
