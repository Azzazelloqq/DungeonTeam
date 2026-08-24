using System;
using DungeonTeam.Gameplay.Quests.Application;
using DungeonTeam.Gameplay.Quests.Domain;
using LocalSaveSystem;
using Unity.Scripting.LifecycleManagement;

namespace DungeonTeam.Gameplay.Quests.Infrastructure
{
    [SaveModel]
    [SaveVersion(2)]
    public sealed class QuestSaveV1
    {
        [SaveFieldId("active")] public QuestProgressSaveV1[] Active;
        [SaveFieldId("completed")] public string[] Completed;
        [SaveFieldId("claimed_reward_quest_ids")] public string[] ClaimedRewardQuestIds;
    }
    [SaveModel]
    [SaveVersion(1)]
    public sealed class QuestProgressSaveV1 { [SaveFieldId("quest_id")] public string QuestId; [SaveFieldId("progress")] public int Progress; }
    public sealed class QuestV1ToV2Migrator : SaveMigrator<QuestSaveV1>
    {
        public override int FromVersion => 1;
        public override int ToVersion => 2;
        public override QuestSaveV1 Migrate(QuestSaveV1 value)
        {
            if (value == null) throw new InvalidOperationException("Cannot migrate a missing quest save.");
            value.ClaimedRewardQuestIds ??= Array.Empty<string>();
            return value;
        }
    }
    public sealed class SaveStoreQuestRepository : IQuestRepository, IDisposable
    {
        [NoAutoStaticsCleanup] private static readonly SaveKey<QuestSaveV1> Key = new("guild.quests");
        private readonly Func<ISaveStore> _freshStoreFactory;
        private ISaveStore _store;
        public SaveStoreQuestRepository(ISaveStore store, Func<ISaveStore> freshStoreFactory = null)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _freshStoreFactory = freshStoreFactory;
            _store.RegisterKey(Key);
        }
        public bool TryLoad(out QuestState state)
        {
            if (!_store.TryGet(Key, out var dto) || dto == null || (dto.Active == null && dto.Completed == null)) { state = null; return false; }
            var active = dto.Active ?? Array.Empty<QuestProgressSaveV1>();
            var values = new QuestProgress[active.Length];
            for (var index = 0; index < values.Length; index++) values[index] = active[index] == null ? throw new InvalidOperationException("Persisted quest progress is missing.") : new QuestProgress(active[index].QuestId, active[index].Progress);
            state = new QuestState(values, dto.Completed ?? Array.Empty<string>(), dto.ClaimedRewardQuestIds ?? Array.Empty<string>());
            return true;
        }
        public void Save(QuestState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            try
            {
                _store.Set(Key, ToDto(state));
                _store.ForceSave();
                VerifyPersisted(state);
            }
            catch (QuestPersistenceException) { throw; }
            catch (Exception exception)
            {
                throw new QuestPersistenceException(
                    "Quest persistence failed; the previous session state was retained.", exception);
            }
        }
        public void Dispose() { _store?.Dispose(); _store = null; }

        private void VerifyPersisted(QuestState expected)
        {
            if (_freshStoreFactory == null) return;
            using var reader = _freshStoreFactory();
            reader.RegisterKey(Key);
            if (!reader.TryGet(Key, out var dto) || dto == null)
                throw new QuestPersistenceException("Quest persistence verification did not observe the saved state.");
            if (!Equivalent(expected, ToState(dto)))
                throw new QuestPersistenceException("Quest persistence verification observed a different state.");
        }
        private static QuestSaveV1 ToDto(QuestState state)
        {
            var active = new QuestProgressSaveV1[state.Active.Count];
            for (var index = 0; index < active.Length; index++) active[index] = new QuestProgressSaveV1 { QuestId = state.Active[index].QuestId, Progress = state.Active[index].CurrentProgress };
            var completed = new string[state.CompletedIds.Count];
            for (var index = 0; index < completed.Length; index++) completed[index] = state.CompletedIds[index];
            var claimed = new string[state.ClaimedRewardQuestIds.Count];
            for (var index = 0; index < claimed.Length; index++) claimed[index] = state.ClaimedRewardQuestIds[index];
            return new QuestSaveV1 { Active = active, Completed = completed, ClaimedRewardQuestIds = claimed };
        }

        private static QuestState ToState(QuestSaveV1 dto)
        {
            if (dto == null) throw new InvalidOperationException("Persisted quest state is missing.");
            var active = dto.Active ?? Array.Empty<QuestProgressSaveV1>();
            var values = new QuestProgress[active.Length];
            for (var index = 0; index < values.Length; index++)
            {
                var progress = active[index] ?? throw new InvalidOperationException(
                    $"Persisted quest progress {index} is missing.");
                values[index] = new QuestProgress(progress.QuestId, progress.Progress);
            }

            return new QuestState(values, dto.Completed ?? Array.Empty<string>(), dto.ClaimedRewardQuestIds ?? Array.Empty<string>());
        }

        private static bool Equivalent(QuestState expected, QuestState observed)
        {
            if (expected.Active.Count != observed.Active.Count ||
                expected.CompletedIds.Count != observed.CompletedIds.Count ||
                expected.ClaimedRewardQuestIds.Count != observed.ClaimedRewardQuestIds.Count) return false;
            for (var index = 0; index < expected.Active.Count; index++)
            {
                var left = expected.Active[index];
                var right = observed.Active[index];
                if (!string.Equals(left.QuestId, right.QuestId, StringComparison.Ordinal) ||
                    left.CurrentProgress != right.CurrentProgress) return false;
            }
            for (var index = 0; index < expected.CompletedIds.Count; index++)
                if (!string.Equals(expected.CompletedIds[index], observed.CompletedIds[index], StringComparison.Ordinal)) return false;
            for (var index = 0; index < expected.ClaimedRewardQuestIds.Count; index++)
                if (!string.Equals(expected.ClaimedRewardQuestIds[index], observed.ClaimedRewardQuestIds[index], StringComparison.Ordinal)) return false;
            return true;
        }
    }

    public sealed class QuestPersistenceException : InvalidOperationException
    {
        public QuestPersistenceException(string message, Exception innerException = null) : base(message, innerException) { }
    }

    public sealed class QuestPersistence : IDisposable
    {
        private readonly SaveStoreOptions _options;
        private readonly SaveRegistry _registry;
        private readonly SaveMigratorRegistry _migrators;
        private SaveStore _store;

        public QuestPersistence(SaveStoreOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _registry = SaveRegistry.CreateDefault(new SaveSerializationOptions { UseTaggedFormat = _options.UseTaggedFormat });
            _migrators = new SaveMigratorRegistry();
            _migrators.Register(new QuestV1ToV2Migrator());
            _store = CreateStore();
            Repository = new SaveStoreQuestRepository(_store, CreateStore);
        }

        public SaveStoreQuestRepository Repository { get; }

        public void Dispose()
        {
            Repository.Dispose();
            _store = null;
        }

        private SaveStore CreateStore() => new(_options, _registry, _migrators);
    }
}
