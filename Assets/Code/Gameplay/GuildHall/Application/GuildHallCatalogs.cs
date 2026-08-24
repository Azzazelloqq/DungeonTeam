using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DungeonTeam.Gameplay.AmbientNpc.Application;
using DungeonTeam.Gameplay.Contracts.Domain;

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

            contracts.ValidateSupportedLocations(contractLocationIds);
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

            var offers = new NoticeBoardOfferSnapshot[contracts.Definitions.Count];
            for (var index = 0; index < offers.Length; index++)
            {
                var definition = contracts.Definitions[index];
                var disabledReason = definition.AuthoredDisabledReason == null
                    ? null
                    : new GuildTextSnapshot(
                        definition.AuthoredDisabledReason.TextId,
                        definition.AuthoredDisabledReason.DisplayText);
                offers[index] = new NoticeBoardOfferSnapshot(
                    definition.ContractId,
                    new GuildTextSnapshot(definition.Title.TextId, definition.Title.DisplayText),
                    new GuildTextSnapshot(definition.Summary.TextId, definition.Summary.DisplayText),
                    definition.LocationId,
                    definition.IsAuthoredAvailable,
                    disabledReason,
                    definition.MinimumRankId);
            }

            return new GuildHallStartContext(
                guildHall.Npcs,
                offers,
                sessionState.SelectedContractId,
                sessionState.LastRunSummary);
        }
    }
}
