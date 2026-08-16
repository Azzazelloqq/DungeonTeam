using System;
using DungeonTeam.Gameplay.PlayerProfile.Application;
using DungeonTeam.Gameplay.PlayerProfile.Domain;
using LocalSaveSystem;

namespace DungeonTeam.Gameplay.PlayerProfile.Infrastructure
{
    [SaveModel]
    [SaveVersion(1)]
    public sealed class PlayerProfileSaveV1
    {
        [SaveFieldId("gold")]
        public long Gold;

        [SaveFieldId("rank_id")]
        public string RankId;

        [SaveFieldId("heroes")]
        public PlayerProfileHeroSaveV1[] Heroes;

        [SaveFieldId("leader_actor_id")]
        public string LeaderActorId;

        [SaveFieldId("companion_actor_ids")]
        public string[] CompanionActorIds;
    }

    [SaveModel]
    [SaveVersion(1)]
    public sealed class PlayerProfileHeroSaveV1
    {
        [SaveFieldId("actor_id")]
        public string ActorId;

        [SaveFieldId("level")]
        public int Level;

        [SaveFieldId("loadout_id")]
        public string LoadoutId;
    }

    public sealed class SaveStorePlayerProfileRepository : IPlayerProfileRepository
    {
        private readonly SaveKey<PlayerProfileSaveV1> _key = new("player.profile");
        private readonly ISaveStore _store;

        public SaveStorePlayerProfileRepository(ISaveStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _store.RegisterKey(_key);
        }

        public bool TryLoad(out PlayerProfileState state)
        {
            if (!_store.TryGet(_key, out var dto) || dto == null ||
                (dto.Heroes == null && dto.LeaderActorId == null && dto.CompanionActorIds == null))
            {
                state = null;
                return false;
            }

            state = ToState(dto);
            return true;
        }

        public void Save(PlayerProfileState state)
        {
            _store.Set(_key, ToDto(state ?? throw new ArgumentNullException(nameof(state))));
            _store.ForceSave();
        }

        private static PlayerProfileSaveV1 ToDto(PlayerProfileState state)
        {
            var heroes = new PlayerProfileHeroSaveV1[state.Heroes.Count];
            for (var index = 0; index < heroes.Length; index++)
            {
                heroes[index] = new PlayerProfileHeroSaveV1
                {
                    ActorId = state.Heroes[index].ActorId,
                    Level = state.Heroes[index].Level,
                    LoadoutId = state.Heroes[index].LoadoutId
                };
            }

            var companions = new string[state.CompanionActorIds.Count];
            for (var index = 0; index < companions.Length; index++)
            {
                companions[index] = state.CompanionActorIds[index];
            }

            return new PlayerProfileSaveV1
            {
                Gold = state.Gold,
                RankId = state.RankId,
                Heroes = heroes,
                LeaderActorId = state.LeaderActorId,
                CompanionActorIds = companions
            };
        }

        private static PlayerProfileState ToState(PlayerProfileSaveV1 dto)
        {
            if (dto.Heroes == null || dto.CompanionActorIds == null)
            {
                throw new InvalidOperationException(
                    "Player profile V1 contains missing collections.");
            }

            var heroes = new HeroProfileState[dto.Heroes.Length];
            for (var index = 0; index < heroes.Length; index++)
            {
                var hero = dto.Heroes[index] ?? throw new InvalidOperationException(
                    $"Player profile hero {index} is missing.");
                heroes[index] = new HeroProfileState(
                    hero.ActorId,
                    hero.Level,
                    hero.LoadoutId);
            }

            return new PlayerProfileState(
                dto.Gold,
                dto.RankId,
                heroes,
                dto.LeaderActorId,
                dto.CompanionActorIds);
        }
    }
}
