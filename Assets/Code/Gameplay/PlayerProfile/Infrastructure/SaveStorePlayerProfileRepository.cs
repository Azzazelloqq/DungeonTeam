using System;
using System.Collections.Generic;
using DungeonTeam.Gameplay.Inventory.Domain;
using DungeonTeam.Gameplay.PlayerProfile.Application;
using DungeonTeam.Gameplay.PlayerProfile.Domain;
using LocalSaveSystem;

namespace DungeonTeam.Gameplay.PlayerProfile.Infrastructure
{
    [SaveModel]
    [SaveVersion(2)]
    public sealed class PlayerProfileSaveV1
    {
        [SaveFieldId("gold")] public long Gold;
        [SaveFieldId("rank_id")] public string RankId;
        [SaveFieldId("heroes")] public PlayerProfileHeroSaveV1[] Heroes;
        [SaveFieldId("leader_actor_id")] public string LeaderActorId;
        [SaveFieldId("companion_actor_ids")] public string[] CompanionActorIds;
        [SaveFieldId("inventory_unique_items")] public PlayerProfileItemInstanceSaveV2[] UniqueItems;
        [SaveFieldId("inventory_resources")] public PlayerProfileResourceStackSaveV2[] Resources;
        [SaveFieldId("inventory_equipment_by_hero")] public PlayerProfileHeroEquipmentSaveV2[] EquipmentByHero;
    }

    [SaveModel]
    [SaveVersion(1)]
    public sealed class PlayerProfileHeroSaveV1
    {
        [SaveFieldId("actor_id")] public string ActorId;
        [SaveFieldId("level")] public int Level;
        [SaveFieldId("loadout_id")] public string LoadoutId;
    }

    [SaveModel]
    [SaveVersion(1)]
    public sealed class PlayerProfileItemInstanceSaveV2
    {
        [SaveFieldId("instance_id")] public string InstanceId;
        [SaveFieldId("definition_id")] public string DefinitionId;
    }

    [SaveModel]
    [SaveVersion(1)]
    public sealed class PlayerProfileResourceStackSaveV2
    {
        [SaveFieldId("definition_id")] public string DefinitionId;
        [SaveFieldId("quantity")] public int Quantity;
    }

    [SaveModel]
    [SaveVersion(1)]
    public sealed class PlayerProfileHeroEquipmentSaveV2
    {
        [SaveFieldId("actor_id")] public string ActorId;
        [SaveFieldId("weapon_instance_id")] public string WeaponInstanceId;
        [SaveFieldId("armor_instance_id")] public string ArmorInstanceId;
        [SaveFieldId("relic_instance_id")] public string RelicInstanceId;
    }

    public sealed class PlayerProfilePersistenceException : InvalidOperationException
    {
        public PlayerProfilePersistenceException(string message, Exception innerException = null) : base(message, innerException) { }
    }

    public sealed class PlayerProfileV1ToV2Migrator : SaveMigrator<PlayerProfileSaveV1>
    {
        public bool WasApplied { get; private set; }
        public void ClearApplied() => WasApplied = false;
        public override int FromVersion => 1;
        public override int ToVersion => 2;

        public override PlayerProfileSaveV1 Migrate(PlayerProfileSaveV1 value)
        {
            if (value == null) throw new InvalidOperationException("Cannot migrate a missing player profile.");
            WasApplied = true;
            value.UniqueItems ??= new[]
            {
                new PlayerProfileItemInstanceSaveV2 { InstanceId = "starter.training-blade", DefinitionId = "equipment.training-blade" },
                new PlayerProfileItemInstanceSaveV2 { InstanceId = "starter.warden-coat", DefinitionId = "equipment.warden-coat" },
                new PlayerProfileItemInstanceSaveV2 { InstanceId = "starter.pathfinder-charm", DefinitionId = "equipment.pathfinder-charm" }
            };
            value.Resources ??= Array.Empty<PlayerProfileResourceStackSaveV2>();
            if (value.EquipmentByHero == null)
            {
                var heroes = value.Heroes ?? Array.Empty<PlayerProfileHeroSaveV1>();
                value.EquipmentByHero = new PlayerProfileHeroEquipmentSaveV2[heroes.Length];
                for (var index = 0; index < heroes.Length; index++)
                    value.EquipmentByHero[index] = new PlayerProfileHeroEquipmentSaveV2 { ActorId = heroes[index]?.ActorId };
            }
            return value;
        }
    }

    public sealed class SaveStorePlayerProfileRepository : IPlayerProfileRepository
    {
        private static readonly SaveKey<PlayerProfileSaveV1> ProfileKey = new("player.profile");
        private readonly Func<ISaveStore> _freshStoreFactory;
        private readonly Func<IReadOnlyList<HeroProfileState>, InventoryState> _legacyInventoryFactory;
        private readonly Func<bool> _wasMigrated;
        private readonly Action _clearMigrationFlag;
        private ISaveStore _store;

        public SaveStorePlayerProfileRepository(ISaveStore store) : this(store, null, null, null, null) { }

        public SaveStorePlayerProfileRepository(
            ISaveStore store,
            Func<ISaveStore> freshStoreFactory,
            Func<IReadOnlyList<HeroProfileState>, InventoryState> legacyInventoryFactory,
            Func<bool> wasMigrated = null,
            Action clearMigrationFlag = null)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _freshStoreFactory = freshStoreFactory;
            _legacyInventoryFactory = legacyInventoryFactory;
            _wasMigrated = wasMigrated;
            _clearMigrationFlag = clearMigrationFlag;
            RegisterKey(_store);
        }

        public bool TryLoad(out PlayerProfileState state)
        {
            if (!_store.TryGet(ProfileKey, out var dto) || dto == null ||
                (dto.Heroes == null && dto.LeaderActorId == null && dto.CompanionActorIds == null))
            {
                state = null;
                return false;
            }

            var wasLegacy = dto.UniqueItems == null || dto.Resources == null || dto.EquipmentByHero == null || _wasMigrated?.Invoke() == true;
            state = ToState(dto, _legacyInventoryFactory);
            if (wasLegacy && _freshStoreFactory != null)
            {
                _clearMigrationFlag?.Invoke();
                Save(state);
            }
            return true;
        }

        public void Save(PlayerProfileState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            try
            {
                _store.Set(ProfileKey, ToDto(state));
                _store.ForceSave();
                if (_freshStoreFactory != null) VerifyPersisted(state);
            }
            catch (PlayerProfilePersistenceException) { throw; }
            catch (Exception exception)
            {
                ResetAfterFailure(exception);
                throw;
            }
        }

        public void Dispose()
        {
            _store?.Dispose();
            _store = null;
        }

        private void VerifyPersisted(PlayerProfileState expected)
        {
            ISaveStore reader = null;
            try
            {
                reader = _freshStoreFactory();
                RegisterKey(reader);
                if (!reader.TryGet(ProfileKey, out var dto) || dto == null)
                    throw new InvalidOperationException("Fresh SaveStore reader did not observe player profile.");
                var observed = ToState(dto, _legacyInventoryFactory);
                if (!Equivalent(expected, observed))
                    throw new InvalidOperationException("Fresh SaveStore reader observed a different player profile.");
            }
            catch (Exception exception) { ResetAfterFailure(exception); }
            finally { reader?.Dispose(); }
        }

        private void ResetAfterFailure(Exception cause)
        {
            try
            {
                _store?.Dispose();
                _store = _freshStoreFactory?.Invoke();
                if (_store != null) RegisterKey(_store);
            }
            catch (Exception resetException)
            {
                throw new PlayerProfilePersistenceException(
                    "Player profile persistence failed and its store could not be recreated.",
                    new AggregateException(cause, resetException));
            }

            throw new PlayerProfilePersistenceException(
                "Player profile persistence verification failed; the previous session state was retained.", cause);
        }

        private static void RegisterKey(ISaveStore store) => store.RegisterKey(ProfileKey);

        private static PlayerProfileSaveV1 ToDto(PlayerProfileState state)
        {
            var heroes = new PlayerProfileHeroSaveV1[state.Heroes.Count];
            for (var index = 0; index < heroes.Length; index++)
                heroes[index] = new PlayerProfileHeroSaveV1
                {
                    ActorId = state.Heroes[index].ActorId,
                    Level = state.Heroes[index].Level,
                    LoadoutId = state.Heroes[index].LoadoutId
                };
            var companions = new string[state.CompanionActorIds.Count];
            for (var index = 0; index < companions.Length; index++) companions[index] = state.CompanionActorIds[index];
            var items = new PlayerProfileItemInstanceSaveV2[state.Inventory.UniqueItems.Count];
            for (var index = 0; index < items.Length; index++)
                items[index] = new PlayerProfileItemInstanceSaveV2
                {
                    InstanceId = state.Inventory.UniqueItems[index].InstanceId,
                    DefinitionId = state.Inventory.UniqueItems[index].DefinitionId
                };
            var resources = new PlayerProfileResourceStackSaveV2[state.Inventory.Resources.Count];
            for (var index = 0; index < resources.Length; index++)
                resources[index] = new PlayerProfileResourceStackSaveV2
                {
                    DefinitionId = state.Inventory.Resources[index].DefinitionId,
                    Quantity = state.Inventory.Resources[index].Quantity
                };
            var equipment = new PlayerProfileHeroEquipmentSaveV2[state.Inventory.EquipmentByHero.Count];
            for (var index = 0; index < equipment.Length; index++)
            {
                var source = state.Inventory.EquipmentByHero[index];
                equipment[index] = new PlayerProfileHeroEquipmentSaveV2
                {
                    ActorId = source.ActorId,
                    WeaponInstanceId = source.WeaponInstanceId,
                    ArmorInstanceId = source.ArmorInstanceId,
                    RelicInstanceId = source.RelicInstanceId
                };
            }
            return new PlayerProfileSaveV1
            {
                Gold = state.Gold, RankId = state.RankId, Heroes = heroes,
                LeaderActorId = state.LeaderActorId, CompanionActorIds = companions,
                UniqueItems = items, Resources = resources, EquipmentByHero = equipment
            };
        }

        private static PlayerProfileState ToState(
            PlayerProfileSaveV1 dto,
            Func<IReadOnlyList<HeroProfileState>, InventoryState> legacyInventoryFactory)
        {
            if (dto.Heroes == null || dto.CompanionActorIds == null)
                throw new InvalidOperationException("Player profile contains missing roster collections.");
            var heroes = new HeroProfileState[dto.Heroes.Length];
            for (var index = 0; index < heroes.Length; index++)
            {
                var hero = dto.Heroes[index] ?? throw new InvalidOperationException($"Player profile hero {index} is missing.");
                heroes[index] = new HeroProfileState(hero.ActorId, hero.Level, hero.LoadoutId);
            }
            var inventory = dto.UniqueItems != null && dto.Resources != null && dto.EquipmentByHero != null
                ? ToInventory(dto)
                : legacyInventoryFactory?.Invoke(heroes) ?? InventoryState.Empty;
            return new PlayerProfileState(dto.Gold, dto.RankId, heroes, dto.LeaderActorId, dto.CompanionActorIds, inventory);
        }

        private static InventoryState ToInventory(PlayerProfileSaveV1 dto)
        {
            var items = new ItemInstanceState[dto.UniqueItems.Length];
            for (var index = 0; index < items.Length; index++)
            {
                var item = dto.UniqueItems[index] ?? throw new InvalidOperationException($"Player profile item {index} is missing.");
                items[index] = new ItemInstanceState(item.InstanceId, item.DefinitionId);
            }
            var resources = new ResourceStackState[dto.Resources.Length];
            for (var index = 0; index < resources.Length; index++)
            {
                var resource = dto.Resources[index] ?? throw new InvalidOperationException($"Player profile resource {index} is missing.");
                resources[index] = new ResourceStackState(resource.DefinitionId, resource.Quantity);
            }
            var equipment = new HeroEquipmentState[dto.EquipmentByHero.Length];
            for (var index = 0; index < equipment.Length; index++)
            {
                var source = dto.EquipmentByHero[index] ?? throw new InvalidOperationException($"Player profile equipment mapping {index} is missing.");
                equipment[index] = new HeroEquipmentState(source.ActorId, source.WeaponInstanceId, source.ArmorInstanceId, source.RelicInstanceId);
            }
            return new InventoryState(items, resources, equipment);
        }

        private static bool Equivalent(PlayerProfileState expected, PlayerProfileState observed)
        {
            if (expected.Gold != observed.Gold || !string.Equals(expected.RankId, observed.RankId, StringComparison.Ordinal) ||
                !string.Equals(expected.LeaderActorId, observed.LeaderActorId, StringComparison.Ordinal) ||
                !EqualStrings(expected.CompanionActorIds, observed.CompanionActorIds) ||
                expected.Heroes.Count != observed.Heroes.Count ||
                expected.Inventory.UniqueItems.Count != observed.Inventory.UniqueItems.Count ||
                expected.Inventory.Resources.Count != observed.Inventory.Resources.Count ||
                expected.Inventory.EquipmentByHero.Count != observed.Inventory.EquipmentByHero.Count) return false;
            for (var index = 0; index < expected.Heroes.Count; index++)
            {
                var left = expected.Heroes[index]; var right = observed.Heroes[index];
                if (!string.Equals(left.ActorId, right.ActorId, StringComparison.Ordinal) || left.Level != right.Level ||
                    !string.Equals(left.LoadoutId, right.LoadoutId, StringComparison.Ordinal)) return false;
            }
            for (var index = 0; index < expected.Inventory.UniqueItems.Count; index++)
            {
                var left = expected.Inventory.UniqueItems[index]; var right = observed.Inventory.UniqueItems[index];
                if (!string.Equals(left.InstanceId, right.InstanceId, StringComparison.Ordinal) ||
                    !string.Equals(left.DefinitionId, right.DefinitionId, StringComparison.Ordinal)) return false;
            }
            for (var index = 0; index < expected.Inventory.Resources.Count; index++)
            {
                var left = expected.Inventory.Resources[index]; var right = observed.Inventory.Resources[index];
                if (!string.Equals(left.DefinitionId, right.DefinitionId, StringComparison.Ordinal) || left.Quantity != right.Quantity) return false;
            }
            for (var index = 0; index < expected.Inventory.EquipmentByHero.Count; index++)
            {
                var left = expected.Inventory.EquipmentByHero[index]; var right = observed.Inventory.EquipmentByHero[index];
                if (!string.Equals(left.ActorId, right.ActorId, StringComparison.Ordinal) ||
                    !string.Equals(left.WeaponInstanceId, right.WeaponInstanceId, StringComparison.Ordinal) ||
                    !string.Equals(left.ArmorInstanceId, right.ArmorInstanceId, StringComparison.Ordinal) ||
                    !string.Equals(left.RelicInstanceId, right.RelicInstanceId, StringComparison.Ordinal)) return false;
            }
            return true;
        }

        private static bool EqualStrings(IReadOnlyList<string> left, IReadOnlyList<string> right)
        {
            if (left.Count != right.Count) return false;
            for (var index = 0; index < left.Count; index++)
                if (!string.Equals(left[index], right[index], StringComparison.Ordinal)) return false;
            return true;
        }
    }

    public sealed class PlayerProfilePersistence : IDisposable
    {
        private readonly SaveStoreOptions _options;
        private readonly SaveRegistry _registry;
        private readonly SaveMigratorRegistry _migrators;
        private SaveStore _store;

        public PlayerProfilePersistence(
            SaveStoreOptions options,
            Func<IReadOnlyList<HeroProfileState>, InventoryState> legacyInventoryFactory = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _registry = SaveRegistry.CreateDefault(new SaveSerializationOptions { UseTaggedFormat = _options.UseTaggedFormat });
            _migrators = new SaveMigratorRegistry();
            var migrator = new PlayerProfileV1ToV2Migrator();
            _migrators.Register(migrator);
            _store = CreateStore();
            Repository = new SaveStorePlayerProfileRepository(
                _store,
                CreateStore,
                legacyInventoryFactory,
                () => migrator.WasApplied,
                migrator.ClearApplied);
        }

        public SaveStorePlayerProfileRepository Repository { get; }

        public void Dispose()
        {
            Repository.Dispose();
            _store = null;
        }

        private SaveStore CreateStore() => new(_options, _registry, _migrators);
    }
}
