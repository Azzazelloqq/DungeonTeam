using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DungeonTeam.Gameplay.AmbientNpc.Application;

namespace DungeonTeam.Gameplay.GuildHall.Application
{
    public enum GuildInteractionKind
    {
        Npc = 0,
        NoticeBoard = 1,
        Reception = 2,
        Exit = 3
    }

    public sealed class GuildHallMovementSettings
    {
        public GuildHallMovementSettings(float speed, float acceleration, float interactionScanInterval)
        {
            if (speed <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(speed));
            }

            if (acceleration <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(acceleration));
            }

            if (interactionScanInterval <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(interactionScanInterval));
            }

            Speed = speed;
            Acceleration = acceleration;
            InteractionScanInterval = interactionScanInterval;
        }

        public float Speed { get; }
        public float Acceleration { get; }
        public float InteractionScanInterval { get; }
    }

    public sealed class GuildInteractionLabels
    {
        public GuildInteractionLabels(
            GuildTextSnapshot npc,
            GuildTextSnapshot noticeBoard,
            GuildTextSnapshot reception,
            GuildTextSnapshot exit)
        {
            Npc = npc ?? throw new ArgumentNullException(nameof(npc));
            NoticeBoard = noticeBoard ?? throw new ArgumentNullException(nameof(noticeBoard));
            Reception = reception ?? throw new ArgumentNullException(nameof(reception));
            Exit = exit ?? throw new ArgumentNullException(nameof(exit));
        }

        public GuildTextSnapshot Npc { get; }
        public GuildTextSnapshot NoticeBoard { get; }
        public GuildTextSnapshot Reception { get; }
        public GuildTextSnapshot Exit { get; }

        public GuildTextSnapshot Get(GuildInteractionKind kind)
        {
            return kind switch
            {
                GuildInteractionKind.Npc => Npc,
                GuildInteractionKind.NoticeBoard => NoticeBoard,
                GuildInteractionKind.Reception => Reception,
                GuildInteractionKind.Exit => Exit,
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
            };
        }
    }

    public sealed class GuildHallCatalog
    {
        private readonly IReadOnlyDictionary<string, AmbientNpcSnapshot> _npcsById;

        public GuildHallCatalog(
            IReadOnlyList<AmbientNpcSnapshot> npcs,
            GuildHallMovementSettings movement,
            GuildInteractionLabels interactionLabels,
            NoticeBoardTextSnapshot noticeBoardText,
            GuildRunSummaryTextSnapshot runSummaryText,
            GuildProfileTextSnapshot profileText)
        {
            Npcs = Snapshot.Copy(npcs, nameof(npcs));
            var npcsById = new Dictionary<string, AmbientNpcSnapshot>(StringComparer.Ordinal);
            for (var index = 0; index < Npcs.Count; index++)
            {
                var npc = Npcs[index];
                if (!npcsById.TryAdd(npc.NpcId, npc))
                {
                    throw new ArgumentException($"NPC ID '{npc.NpcId}' is duplicated.", nameof(npcs));
                }
            }

            _npcsById = new ReadOnlyDictionary<string, AmbientNpcSnapshot>(npcsById);
            Movement = movement ?? throw new ArgumentNullException(nameof(movement));
            InteractionLabels = interactionLabels ??
                throw new ArgumentNullException(nameof(interactionLabels));
            NoticeBoardText = noticeBoardText ??
                throw new ArgumentNullException(nameof(noticeBoardText));
            RunSummaryText = runSummaryText ??
                throw new ArgumentNullException(nameof(runSummaryText));
            ProfileText = profileText ?? throw new ArgumentNullException(nameof(profileText));
        }

        public IReadOnlyList<AmbientNpcSnapshot> Npcs { get; }
        public GuildHallMovementSettings Movement { get; }
        public GuildInteractionLabels InteractionLabels { get; }
        public NoticeBoardTextSnapshot NoticeBoardText { get; }
        public GuildRunSummaryTextSnapshot RunSummaryText { get; }
        public GuildProfileTextSnapshot ProfileText { get; }

        public AmbientNpcSnapshot RequireNpc(string npcId)
        {
            if (!_npcsById.TryGetValue(GuildId.Require(npcId, nameof(npcId)), out var npc))
            {
                throw new KeyNotFoundException($"Unknown Guild Hall NPC ID '{npcId}'.");
            }

            return npc;
        }
    }

    public sealed class ContractCatalog
    {
        private readonly IReadOnlyDictionary<string, NoticeBoardOfferSnapshot> _offersById;

        public ContractCatalog(IReadOnlyList<NoticeBoardOfferSnapshot> offers)
        {
            Offers = Snapshot.Copy(offers, nameof(offers));
            var byId = new Dictionary<string, NoticeBoardOfferSnapshot>(StringComparer.Ordinal);
            for (var index = 0; index < Offers.Count; index++)
            {
                var offer = Offers[index];
                if (!byId.TryAdd(offer.ContractId, offer))
                {
                    throw new ArgumentException(
                        $"Contract ID '{offer.ContractId}' is duplicated.",
                        nameof(offers));
                }
            }

            _offersById = new ReadOnlyDictionary<string, NoticeBoardOfferSnapshot>(byId);
        }

        public IReadOnlyList<NoticeBoardOfferSnapshot> Offers { get; }

        public NoticeBoardOfferSnapshot Require(string contractId)
        {
            if (!_offersById.TryGetValue(GuildId.Require(contractId, nameof(contractId)), out var offer))
            {
                throw new KeyNotFoundException($"Unknown contract ID '{contractId}'.");
            }

            return offer;
        }
    }

    public static class GuildContentValidator
    {
        public static void Validate(
            GuildHallCatalog guildHall,
            DialogueCatalog dialogues,
            AmbientNpcProfileCatalog ambientProfiles,
            ContractCatalog contracts,
            IReadOnlyCollection<string> contractLocationIds)
        {
            if (guildHall == null)
            {
                throw new ArgumentNullException(nameof(guildHall));
            }

            if (dialogues == null)
            {
                throw new ArgumentNullException(nameof(dialogues));
            }

            if (ambientProfiles == null)
            {
                throw new ArgumentNullException(nameof(ambientProfiles));
            }

            if (contracts == null)
            {
                throw new ArgumentNullException(nameof(contracts));
            }

            if (contractLocationIds == null)
            {
                throw new ArgumentNullException(nameof(contractLocationIds));
            }

            var locations = new HashSet<string>(contractLocationIds, StringComparer.Ordinal);
            for (var index = 0; index < guildHall.Npcs.Count; index++)
            {
                var npc = guildHall.Npcs[index];
                if (!dialogues.Contains(npc.DialoguePoolId))
                {
                    throw new InvalidOperationException(
                        $"NPC '{npc.NpcId}' references unknown dialogue pool '{npc.DialoguePoolId}'.");
                }

                if (!ambientProfiles.Contains(npc.AmbientProfileId))
                {
                    throw new InvalidOperationException(
                        $"NPC '{npc.NpcId}' references unknown ambient profile '{npc.AmbientProfileId}'.");
                }
            }

            for (var index = 0; index < contracts.Offers.Count; index++)
            {
                var offer = contracts.Offers[index];
                if (!locations.Contains(offer.LocationId))
                {
                    throw new InvalidOperationException(
                        $"Contract '{offer.ContractId}' references unsupported location '{offer.LocationId}'.");
                }
            }
        }
    }

    public static class GuildHallStartContextBuilder
    {
        public static GuildHallStartContext Build(
            GuildHallCatalog guildHall,
            ContractCatalog contracts,
            GuildSessionState sessionState)
        {
            if (guildHall == null)
            {
                throw new ArgumentNullException(nameof(guildHall));
            }

            if (contracts == null)
            {
                throw new ArgumentNullException(nameof(contracts));
            }

            if (sessionState == null)
            {
                throw new ArgumentNullException(nameof(sessionState));
            }

            return new GuildHallStartContext(
                guildHall.Npcs,
                contracts.Offers,
                sessionState.SelectedContractId,
                sessionState.LastRunSummary);
        }
    }
}
