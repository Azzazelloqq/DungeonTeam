using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DungeonTeam.Gameplay.GuildHall.Application
{
    public enum GuildProfileEditKind
    {
        SetLeader = 0,
        AddCompanion = 1,
        RemoveCompanion = 2,
        SetLoadout = 3,
        EquipItem = 4,
        UnequipItem = 5,
        SellUniqueItem = 6,
        SellResource = 7
    }

    public enum GuildProfileEquipmentSlot
    {
        Weapon = 0,
        Armor = 1,
        Relic = 2
    }

    public sealed class GuildProfileEditRequest
    {
        public GuildProfileEditRequest(
            GuildProfileEditKind kind,
            string actorId,
            string loadoutId = null,
            string itemInstanceId = null,
            GuildProfileEquipmentSlot? equipmentSlot = null,
            string definitionId = null)
        {
            if (!Enum.IsDefined(typeof(GuildProfileEditKind), kind))
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }

            var isSale = kind == GuildProfileEditKind.SellUniqueItem ||
                kind == GuildProfileEditKind.SellResource;
            if (!isSale && string.IsNullOrWhiteSpace(actorId))
            {
                throw new ArgumentException("Actor ID cannot be empty.", nameof(actorId));
            }

            if (equipmentSlot.HasValue && !Enum.IsDefined(typeof(GuildProfileEquipmentSlot), equipmentSlot.Value))
            {
                throw new ArgumentOutOfRangeException(nameof(equipmentSlot));
            }

            if (kind == GuildProfileEditKind.SetLoadout != !string.IsNullOrWhiteSpace(loadoutId))
            {
                throw new ArgumentException(
                    "Loadout ID is required only for a loadout change.",
                    nameof(loadoutId));
            }

            if (kind == GuildProfileEditKind.EquipItem)
            {
                if (string.IsNullOrWhiteSpace(itemInstanceId) || loadoutId != null || equipmentSlot.HasValue)
                    throw new ArgumentException("Equip requires an item instance and no other edit value.");
            }
            else if (kind == GuildProfileEditKind.UnequipItem)
            {
                if (!equipmentSlot.HasValue || loadoutId != null || itemInstanceId != null)
                    throw new ArgumentException("Unequip requires an equipment slot and no other edit value.");
            }
            else if (kind == GuildProfileEditKind.SellUniqueItem)
            {
                if (string.IsNullOrWhiteSpace(itemInstanceId) || actorId != null ||
                    loadoutId != null || equipmentSlot.HasValue || definitionId != null)
                    throw new ArgumentException("Selling a unique item requires only its instance ID.");
            }
            else if (kind == GuildProfileEditKind.SellResource)
            {
                if (string.IsNullOrWhiteSpace(definitionId) || actorId != null ||
                    loadoutId != null || itemInstanceId != null || equipmentSlot.HasValue)
                    throw new ArgumentException("Selling a resource requires only its definition ID.");
            }
            else if (itemInstanceId != null || equipmentSlot.HasValue || definitionId != null)
            {
                throw new ArgumentException("Item edit values are valid only for equipment edits.");
            }

            Kind = kind;
            ActorId = actorId;
            LoadoutId = loadoutId;
            ItemInstanceId = itemInstanceId;
            EquipmentSlot = equipmentSlot;
            DefinitionId = definitionId;
        }

        public GuildProfileEditKind Kind { get; }
        public string ActorId { get; }
        public string LoadoutId { get; }
        public string ItemInstanceId { get; }
        public GuildProfileEquipmentSlot? EquipmentSlot { get; }
        public string DefinitionId { get; }
    }

    public sealed class GuildProfileEditResult
    {
        private GuildProfileEditResult(
            bool accepted,
            GuildProfileSnapshot profile,
            GuildTextSnapshot rejection)
        {
            Accepted = accepted;
            Profile = profile;
            Rejection = rejection;
        }

        public bool Accepted { get; }
        public GuildProfileSnapshot Profile { get; }
        public GuildTextSnapshot Rejection { get; }

        public static GuildProfileEditResult Accept(GuildProfileSnapshot profile) =>
            new(true, profile ?? throw new ArgumentNullException(nameof(profile)), null);

        public static GuildProfileEditResult Reject(GuildTextSnapshot rejection) =>
            new(false, null, rejection ?? throw new ArgumentNullException(nameof(rejection)));
    }

    public enum GuildHeroRole
    {
        Leader = 0,
        Companion = 1,
        Available = 2
    }

    public sealed class GuildProfileTextSnapshot
    {
        public GuildProfileTextSnapshot(
            GuildTextSnapshot header,
            GuildTextSnapshot goldLabel,
            GuildTextSnapshot rankLabel,
            GuildTextSnapshot unassignedRank,
            GuildTextSnapshot leaderLabel,
            GuildTextSnapshot leaderExplanation,
            GuildTextSnapshot teamLabel,
            GuildTextSnapshot rosterLabel,
            GuildTextSnapshot availableHeroLabel,
            GuildTextSnapshot levelLabel,
            GuildTextSnapshot healthLabel,
            GuildTextSnapshot speedLabel,
            GuildTextSnapshot primarySkillLabel,
            GuildTextSnapshot activeSkillLabel,
            GuildTextSnapshot close,
            GuildTextSnapshot makeLeader,
            GuildTextSnapshot addCompanion,
            GuildTextSnapshot removeCompanion,
            GuildTextSnapshot loadoutLabel,
            GuildTextSnapshot rejectedTeamSize,
            GuildTextSnapshot rejectedInvalidActor,
            GuildTextSnapshot rejectedInvalidLoadout,
            GuildTextSnapshot rejectedPersistence)
        {
            Header = header ?? throw new ArgumentNullException(nameof(header));
            GoldLabel = goldLabel ?? throw new ArgumentNullException(nameof(goldLabel));
            RankLabel = rankLabel ?? throw new ArgumentNullException(nameof(rankLabel));
            UnassignedRank = unassignedRank ?? throw new ArgumentNullException(nameof(unassignedRank));
            LeaderLabel = leaderLabel ?? throw new ArgumentNullException(nameof(leaderLabel));
            LeaderExplanation = leaderExplanation ??
                throw new ArgumentNullException(nameof(leaderExplanation));
            TeamLabel = teamLabel ?? throw new ArgumentNullException(nameof(teamLabel));
            RosterLabel = rosterLabel ?? throw new ArgumentNullException(nameof(rosterLabel));
            AvailableHeroLabel = availableHeroLabel ??
                throw new ArgumentNullException(nameof(availableHeroLabel));
            LevelLabel = levelLabel ?? throw new ArgumentNullException(nameof(levelLabel));
            HealthLabel = healthLabel ?? throw new ArgumentNullException(nameof(healthLabel));
            SpeedLabel = speedLabel ?? throw new ArgumentNullException(nameof(speedLabel));
            PrimarySkillLabel = primarySkillLabel ??
                throw new ArgumentNullException(nameof(primarySkillLabel));
            ActiveSkillLabel = activeSkillLabel ??
                throw new ArgumentNullException(nameof(activeSkillLabel));
            Close = close ?? throw new ArgumentNullException(nameof(close));
            MakeLeader = makeLeader ?? throw new ArgumentNullException(nameof(makeLeader));
            AddCompanion = addCompanion ?? throw new ArgumentNullException(nameof(addCompanion));
            RemoveCompanion = removeCompanion ?? throw new ArgumentNullException(nameof(removeCompanion));
            LoadoutLabel = loadoutLabel ?? throw new ArgumentNullException(nameof(loadoutLabel));
            RejectedTeamSize = rejectedTeamSize ?? throw new ArgumentNullException(nameof(rejectedTeamSize));
            RejectedInvalidActor = rejectedInvalidActor ?? throw new ArgumentNullException(nameof(rejectedInvalidActor));
            RejectedInvalidLoadout = rejectedInvalidLoadout ?? throw new ArgumentNullException(nameof(rejectedInvalidLoadout));
            RejectedPersistence = rejectedPersistence ?? throw new ArgumentNullException(nameof(rejectedPersistence));
        }

        public GuildTextSnapshot Header { get; }
        public GuildTextSnapshot GoldLabel { get; }
        public GuildTextSnapshot RankLabel { get; }
        public GuildTextSnapshot UnassignedRank { get; }
        public GuildTextSnapshot LeaderLabel { get; }
        public GuildTextSnapshot LeaderExplanation { get; }
        public GuildTextSnapshot TeamLabel { get; }
        public GuildTextSnapshot RosterLabel { get; }
        public GuildTextSnapshot AvailableHeroLabel { get; }
        public GuildTextSnapshot LevelLabel { get; }
        public GuildTextSnapshot HealthLabel { get; }
        public GuildTextSnapshot SpeedLabel { get; }
        public GuildTextSnapshot PrimarySkillLabel { get; }
        public GuildTextSnapshot ActiveSkillLabel { get; }
        public GuildTextSnapshot Close { get; }
        public GuildTextSnapshot MakeLeader { get; }
        public GuildTextSnapshot AddCompanion { get; }
        public GuildTextSnapshot RemoveCompanion { get; }
        public GuildTextSnapshot LoadoutLabel { get; }
        public GuildTextSnapshot RejectedTeamSize { get; }
        public GuildTextSnapshot RejectedInvalidActor { get; }
        public GuildTextSnapshot RejectedInvalidLoadout { get; }
        public GuildTextSnapshot RejectedPersistence { get; }
    }

    public sealed class GuildHeroLoadoutSnapshot
    {
        public GuildHeroLoadoutSnapshot(string loadoutId, string displayText)
        {
            LoadoutId = Require(loadoutId, nameof(loadoutId));
            DisplayText = Require(displayText, nameof(displayText));
        }

        public string LoadoutId { get; }
        public string DisplayText { get; }

        private static string Require(string value, string parameterName) =>
            !string.IsNullOrWhiteSpace(value)
                ? value
                : throw new ArgumentException("Value cannot be empty.", parameterName);
    }

    public sealed class GuildHeroSkillSnapshot
    {
        public GuildHeroSkillSnapshot(string slotId, string slotDisplayText, string displayName, int level)
        {
            SlotId = Require(slotId, nameof(slotId));
            SlotDisplayText = Require(slotDisplayText, nameof(slotDisplayText));
            DisplayName = Require(displayName, nameof(displayName));
            Level = level > 0 ? level : throw new ArgumentOutOfRangeException(nameof(level));
        }

        public string SlotId { get; }
        public string SlotDisplayText { get; }
        public string DisplayName { get; }
        public int Level { get; }

        private static string Require(string value, string parameterName) =>
            !string.IsNullOrWhiteSpace(value)
                ? value
                : throw new ArgumentException("Value cannot be empty.", parameterName);
    }

    public sealed class GuildEquipmentSlotSnapshot
    {
        public GuildEquipmentSlotSnapshot(
            GuildProfileEquipmentSlot slot,
            string displayText,
            string instanceId,
            string itemDisplayName)
        {
            if (!Enum.IsDefined(typeof(GuildProfileEquipmentSlot), slot))
                throw new ArgumentOutOfRangeException(nameof(slot));
            Slot = slot;
            DisplayText = Require(displayText, nameof(displayText));
            InstanceId = instanceId;
            ItemDisplayName = itemDisplayName;
        }

        public GuildProfileEquipmentSlot Slot { get; }
        public string DisplayText { get; }
        public string InstanceId { get; }
        public string ItemDisplayName { get; }

        private static string Require(string value, string name) =>
            !string.IsNullOrWhiteSpace(value) ? value : throw new ArgumentException("Value cannot be empty.", name);
    }

    public sealed class GuildInventoryItemSnapshot
    {
        public GuildInventoryItemSnapshot(
            string instanceId,
            string definitionId,
            string displayText,
            GuildProfileEquipmentSlot slot,
            bool isEquipped,
            long saleValue = 0,
            bool canEquip = true)
        {
            InstanceId = Require(instanceId, nameof(instanceId));
            DefinitionId = Require(definitionId, nameof(definitionId));
            DisplayText = Require(displayText, nameof(displayText));
            if (!Enum.IsDefined(typeof(GuildProfileEquipmentSlot), slot))
                throw new ArgumentOutOfRangeException(nameof(slot));
            Slot = slot;
            IsEquipped = isEquipped;
            SaleValue = saleValue >= 0 ? saleValue : throw new ArgumentOutOfRangeException(nameof(saleValue));
            CanEquip = canEquip;
        }

        public string InstanceId { get; }
        public string DefinitionId { get; }
        public string DisplayText { get; }
        public GuildProfileEquipmentSlot Slot { get; }
        public bool IsEquipped { get; }
        public long SaleValue { get; }
        public bool CanEquip { get; }

        private static string Require(string value, string name) =>
            !string.IsNullOrWhiteSpace(value) ? value : throw new ArgumentException("Value cannot be empty.", name);
    }

    public sealed class GuildHeroSnapshot
    {
        private readonly ReadOnlyCollection<GuildHeroSkillSnapshot> _skills;
        private readonly ReadOnlyCollection<GuildHeroLoadoutSnapshot> _allowedLoadouts;
        private readonly ReadOnlyCollection<GuildEquipmentSlotSnapshot> _equipment;
        private readonly ReadOnlyCollection<GuildInventoryItemSnapshot> _inventoryItems;

        public GuildHeroSnapshot(
            string actorId,
            string displayName,
            GuildHeroRole role,
            int level,
            int maximumHealth,
            float movementSpeed,
            IReadOnlyList<GuildHeroSkillSnapshot> skills,
            string loadoutId,
            IReadOnlyList<GuildHeroLoadoutSnapshot> allowedLoadouts,
            IReadOnlyList<GuildEquipmentSlotSnapshot> equipment = null,
            IReadOnlyList<GuildInventoryItemSnapshot> inventoryItems = null)
        {
            ActorId = Require(actorId, nameof(actorId));
            DisplayName = Require(displayName, nameof(displayName));
            if (!Enum.IsDefined(typeof(GuildHeroRole), role))
            {
                throw new ArgumentOutOfRangeException(nameof(role));
            }

            Role = role;
            Level = level > 0 ? level : throw new ArgumentOutOfRangeException(nameof(level));
            MaximumHealth = maximumHealth > 0
                ? maximumHealth
                : throw new ArgumentOutOfRangeException(nameof(maximumHealth));
            MovementSpeed = movementSpeed > 0f
                ? movementSpeed
                : throw new ArgumentOutOfRangeException(nameof(movementSpeed));

            if (skills == null)
            {
                throw new ArgumentNullException(nameof(skills));
            }

            var copy = new GuildHeroSkillSnapshot[skills.Count];
            for (var index = 0; index < copy.Length; index++)
            {
                copy[index] = skills[index] ?? throw new ArgumentException(
                    $"Skill at index {index} is missing.",
                    nameof(skills));
            }

            _skills = Array.AsReadOnly(copy);
            LoadoutId = Require(loadoutId, nameof(loadoutId));
            _allowedLoadouts = CopyLoadouts(allowedLoadouts, nameof(allowedLoadouts));
            _equipment = CopyOptional(equipment);
            _inventoryItems = CopyOptional(inventoryItems);
        }

        public string ActorId { get; }
        public string DisplayName { get; }
        public GuildHeroRole Role { get; }
        public int Level { get; }
        public int MaximumHealth { get; }
        public float MovementSpeed { get; }
        public IReadOnlyList<GuildHeroSkillSnapshot> Skills => _skills;
        public string LoadoutId { get; }
        public IReadOnlyList<GuildHeroLoadoutSnapshot> AllowedLoadouts => _allowedLoadouts;
        public IReadOnlyList<GuildEquipmentSlotSnapshot> Equipment => _equipment;
        public IReadOnlyList<GuildInventoryItemSnapshot> InventoryItems => _inventoryItems;

        private static ReadOnlyCollection<GuildHeroLoadoutSnapshot> CopyLoadouts(
            IReadOnlyList<GuildHeroLoadoutSnapshot> source,
            string parameterName)
        {
            if (source == null || source.Count == 0)
            {
                throw new ArgumentException("At least one loadout is required.", parameterName);
            }

            var copy = new GuildHeroLoadoutSnapshot[source.Count];
            for (var index = 0; index < copy.Length; index++)
            {
                copy[index] = source[index] ?? throw new ArgumentException(
                    $"Loadout at index {index} is missing.", parameterName);
            }

            return Array.AsReadOnly(copy);
        }

        private static string Require(string value, string parameterName) =>
            !string.IsNullOrWhiteSpace(value)
                ? value
                : throw new ArgumentException("Value cannot be empty.", parameterName);

        private static ReadOnlyCollection<T> CopyOptional<T>(IReadOnlyList<T> source) where T : class
        {
            if (source == null) return Array.AsReadOnly(Array.Empty<T>());
            var copy = new T[source.Count];
            for (var index = 0; index < copy.Length; index++)
                copy[index] = source[index] ?? throw new ArgumentException("Collection contains a missing item.");
            return Array.AsReadOnly(copy);
        }
    }

    public sealed class GuildProfileSnapshot
    {
        private readonly ReadOnlyCollection<GuildHeroSnapshot> _companions;
        private readonly ReadOnlyCollection<GuildHeroSnapshot> _roster;
        private readonly ReadOnlyCollection<GuildResourceSnapshot> _resources;

        public GuildProfileSnapshot(
            long gold,
            string rankDisplayText,
            GuildHeroSnapshot leader,
            IReadOnlyList<GuildHeroSnapshot> companions,
            IReadOnlyList<GuildHeroSnapshot> roster,
            GuildProfileTextSnapshot text,
            IReadOnlyList<GuildResourceSnapshot> resources = null)
        {
            Gold = gold >= 0 ? gold : throw new ArgumentOutOfRangeException(nameof(gold));
            RankDisplayText = !string.IsNullOrWhiteSpace(rankDisplayText)
                ? rankDisplayText
                : throw new ArgumentException("Rank display cannot be empty.", nameof(rankDisplayText));
            Leader = leader ?? throw new ArgumentNullException(nameof(leader));
            _companions = Copy(companions, nameof(companions));
            _roster = Copy(roster, nameof(roster));
            Text = text ?? throw new ArgumentNullException(nameof(text));
            _resources = Copy(resources);
        }

        public long Gold { get; }
        public string RankDisplayText { get; }
        public GuildHeroSnapshot Leader { get; }
        public IReadOnlyList<GuildHeroSnapshot> Companions => _companions;
        public IReadOnlyList<GuildHeroSnapshot> Roster => _roster;
        public GuildProfileTextSnapshot Text { get; }
        public IReadOnlyList<GuildResourceSnapshot> Resources => _resources;

        private static ReadOnlyCollection<GuildHeroSnapshot> Copy(
            IReadOnlyList<GuildHeroSnapshot> source,
            string parameterName)
        {
            if (source == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            var copy = new GuildHeroSnapshot[source.Count];
            for (var index = 0; index < copy.Length; index++)
            {
                copy[index] = source[index] ?? throw new ArgumentException(
                    $"Hero at index {index} is missing.",
                    parameterName);
            }

            return Array.AsReadOnly(copy);
        }

        private static ReadOnlyCollection<GuildResourceSnapshot> Copy(IReadOnlyList<GuildResourceSnapshot> source)
        {
            if (source == null) return Array.AsReadOnly(Array.Empty<GuildResourceSnapshot>());
            var copy = new GuildResourceSnapshot[source.Count];
            for (var index = 0; index < copy.Length; index++)
                copy[index] = source[index] ?? throw new ArgumentException("Resource is missing.");
            return Array.AsReadOnly(copy);
        }
    }

    public sealed class GuildResourceSnapshot
    {
        public GuildResourceSnapshot(string definitionId, string displayText, int quantity)
            : this(definitionId, displayText, quantity, 0)
        {
        }

        public GuildResourceSnapshot(string definitionId, string displayText, int quantity, long saleValue)
        {
            DefinitionId = !string.IsNullOrWhiteSpace(definitionId) ? definitionId : throw new ArgumentException("Definition ID cannot be empty.", nameof(definitionId));
            DisplayText = !string.IsNullOrWhiteSpace(displayText) ? displayText : throw new ArgumentException("Display text cannot be empty.", nameof(displayText));
            Quantity = quantity > 0 ? quantity : throw new ArgumentOutOfRangeException(nameof(quantity));
            SaleValue = saleValue >= 0 ? saleValue : throw new ArgumentOutOfRangeException(nameof(saleValue));
        }

        public string DefinitionId { get; }
        public string DisplayText { get; }
        public int Quantity { get; }
        public long SaleValue { get; }
    }
}
