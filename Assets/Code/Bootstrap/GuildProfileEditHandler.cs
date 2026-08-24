using System;
using DungeonTeam.Gameplay.DungeonRun.Application;
using DungeonTeam.Gameplay.GuildHall.Application;
using DungeonTeam.Gameplay.PlayerProfile.Application;
using DungeonTeam.Gameplay.PlayerProfile.Domain;
using DungeonTeam.Gameplay.Inventory.Application;
using DungeonTeam.Gameplay.Inventory.Domain;

namespace Code.ApplicationRoot
{
    internal sealed class GuildProfileEditHandler
    {
        private readonly PlayerProfileSession _session;
        private readonly DungeonRunTeamSetup _teamSetup;
        private readonly GuildProfileTextSnapshot _text;
        private readonly Func<PlayerProfileState, GuildProfileSnapshot> _buildSnapshot;
        private readonly ItemCatalog _itemCatalog;
        private readonly Action<Exception> _reportPersistenceFailure;

        public GuildProfileEditHandler(
            PlayerProfileSession session,
            DungeonRunTeamSetup teamSetup,
            GuildProfileTextSnapshot text,
            Func<PlayerProfileState, GuildProfileSnapshot> buildSnapshot,
            Action<Exception> reportPersistenceFailure)
            : this(session, teamSetup, text, buildSnapshot, null, reportPersistenceFailure)
        {
        }

        public GuildProfileEditHandler(
            PlayerProfileSession session,
            DungeonRunTeamSetup teamSetup,
            GuildProfileTextSnapshot text,
            Func<PlayerProfileState, GuildProfileSnapshot> buildSnapshot,
            ItemCatalog itemCatalog,
            Action<Exception> reportPersistenceFailure)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _teamSetup = teamSetup ?? throw new ArgumentNullException(nameof(teamSetup));
            _text = text ?? throw new ArgumentNullException(nameof(text));
            _buildSnapshot = buildSnapshot ?? throw new ArgumentNullException(nameof(buildSnapshot));
            _itemCatalog = itemCatalog;
            _reportPersistenceFailure = reportPersistenceFailure ??
                throw new ArgumentNullException(nameof(reportPersistenceFailure));
        }

        public GuildProfileEditResult Handle(GuildProfileEditRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            PlayerProfileState candidate;
            try
            {
                candidate = request.Kind switch
                {
                    GuildProfileEditKind.SetLeader => _session.State.ChangeLeader(request.ActorId),
                    GuildProfileEditKind.AddCompanion => _session.State.AddCompanion(request.ActorId),
                    GuildProfileEditKind.RemoveCompanion => _session.State.RemoveCompanion(request.ActorId),
                    GuildProfileEditKind.SetLoadout => _session.State.ChangeLoadout(
                        request.ActorId,
                        request.LoadoutId),
                    GuildProfileEditKind.EquipItem => EquipItem(request),
                    GuildProfileEditKind.UnequipItem => UnequipItem(request),
                    _ => throw new ArgumentOutOfRangeException(nameof(request))
                };
            }
            catch (ArgumentException)
            {
                return GuildProfileEditResult.Reject(
                    request.Kind == GuildProfileEditKind.SetLoadout
                        ? _text.RejectedInvalidLoadout
                        : _text.RejectedInvalidActor);
            }
            catch (InvalidOperationException)
            {
                return GuildProfileEditResult.Reject(_text.RejectedInvalidActor);
            }

            if (ReferenceEquals(candidate, _session.State))
            {
                return GuildProfileEditResult.Accept(_buildSnapshot(_session.State));
            }

            var selection = PlayerProfileComposition.MapToTeamSelection(candidate, _itemCatalog);
            if (!_teamSetup.TryValidate(selection, out var failure))
            {
                return GuildProfileEditResult.Reject(ResolveRejection(failure));
            }

            var snapshot = _buildSnapshot(candidate);
            try
            {
                _session.Commit(candidate);
            }
            catch (Exception exception)
            {
                _reportPersistenceFailure(exception);
                return GuildProfileEditResult.Reject(_text.RejectedPersistence);
            }

            return GuildProfileEditResult.Accept(snapshot);
        }

        private PlayerProfileState EquipItem(GuildProfileEditRequest request)
        {
            if (_itemCatalog == null)
                throw new InvalidOperationException("Item catalog is not configured.");
            if (!_session.State.Inventory.TryGetInstance(request.ItemInstanceId, out var item))
                throw new ArgumentException("Item instance is not owned.");
            var definition = _itemCatalog.RequireEquipment(item.DefinitionId);
            if (!definition.IsEligibleFor(request.ActorId))
                throw new ArgumentException("Actor cannot equip this item.");
            return _session.State.ReplaceInventory(
                _session.State.Inventory.Equip(request.ActorId, request.ItemInstanceId, definition.Slot));
        }

        private PlayerProfileState UnequipItem(GuildProfileEditRequest request)
        {
            return _session.State.ReplaceInventory(
                _session.State.Inventory.Unequip(
                    request.ActorId,
                    ToInventorySlot(request.EquipmentSlot.Value)));
        }

        private static EquipmentSlot ToInventorySlot(GuildProfileEquipmentSlot slot) => slot switch
        {
            GuildProfileEquipmentSlot.Weapon => EquipmentSlot.Weapon,
            GuildProfileEquipmentSlot.Armor => EquipmentSlot.Armor,
            GuildProfileEquipmentSlot.Relic => EquipmentSlot.Relic,
            _ => throw new ArgumentOutOfRangeException(nameof(slot), slot, null)
        };

        private GuildTextSnapshot ResolveRejection(DungeonRunTeamValidationFailure failure)
        {
            return failure switch
            {
                DungeonRunTeamValidationFailure.TeamSizeOutOfRange => _text.RejectedTeamSize,
                DungeonRunTeamValidationFailure.LoadoutUnavailable => _text.RejectedInvalidLoadout,
                DungeonRunTeamValidationFailure.SelectionMissing or
                    DungeonRunTeamValidationFailure.ActorUnavailable or
                    DungeonRunTeamValidationFailure.LevelUnavailable => _text.RejectedInvalidActor,
                _ => throw new ArgumentOutOfRangeException(nameof(failure), failure, null)
            };
        }
    }
}
