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
        private readonly GuildRankCatalog _rankCatalog;
        private readonly Action<Exception> _reportPersistenceFailure;

        public GuildProfileEditHandler(
            PlayerProfileSession session,
            DungeonRunTeamSetup teamSetup,
            GuildProfileTextSnapshot text,
            Func<PlayerProfileState, GuildProfileSnapshot> buildSnapshot,
            Action<Exception> reportPersistenceFailure)
            : this(session, teamSetup, text, buildSnapshot, null, null, reportPersistenceFailure)
        {
        }

        public GuildProfileEditHandler(
            PlayerProfileSession session,
            DungeonRunTeamSetup teamSetup,
            GuildProfileTextSnapshot text,
            Func<PlayerProfileState, GuildProfileSnapshot> buildSnapshot,
            ItemCatalog itemCatalog,
            Action<Exception> reportPersistenceFailure)
            : this(session, teamSetup, text, buildSnapshot, itemCatalog, null, reportPersistenceFailure)
        {
        }

        public GuildProfileEditHandler(
            PlayerProfileSession session,
            DungeonRunTeamSetup teamSetup,
            GuildProfileTextSnapshot text,
            Func<PlayerProfileState, GuildProfileSnapshot> buildSnapshot,
            ItemCatalog itemCatalog,
            GuildRankCatalog rankCatalog,
            Action<Exception> reportPersistenceFailure)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _teamSetup = teamSetup ?? throw new ArgumentNullException(nameof(teamSetup));
            _text = text ?? throw new ArgumentNullException(nameof(text));
            _buildSnapshot = buildSnapshot ?? throw new ArgumentNullException(nameof(buildSnapshot));
            _itemCatalog = itemCatalog;
            _rankCatalog = rankCatalog;
            _reportPersistenceFailure = reportPersistenceFailure ??
                throw new ArgumentNullException(nameof(reportPersistenceFailure));
        }

        public GuildProfileEditResult Handle(GuildProfileEditRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (request.Kind == GuildProfileEditKind.PromoteRank)
            {
                return PromoteRank();
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
                    GuildProfileEditKind.SellUniqueItem => SellUniqueItem(request),
                    GuildProfileEditKind.SellResource => SellResource(request),
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
            catch (OverflowException)
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

        private GuildProfileEditResult PromoteRank()
        {
            if (_rankCatalog == null)
            {
                return GuildProfileEditResult.Reject(_text.RejectedInvalidActor);
            }

            RankPromotionResult result;
            try
            {
                result = _session.PromoteRank(_rankCatalog);
            }
            catch (Exception exception)
            {
                _reportPersistenceFailure(exception);
                return GuildProfileEditResult.Reject(_text.RejectedPersistence);
            }

            if (!result.Accepted)
            {
                return GuildProfileEditResult.Reject(ResolveRankRejection(result.Rejection.Value));
            }

            return GuildProfileEditResult.Accept(_buildSnapshot(_session.State));
        }

        private GuildTextSnapshot ResolveRankRejection(RankPromotionRejection rejection)
        {
            return rejection switch
            {
                RankPromotionRejection.InsufficientGold =>
                    _text.RejectedInsufficientGold ?? _text.RejectedPersistence,
                RankPromotionRejection.AlreadyTerminal =>
                    _text.TerminalRank ?? _text.RejectedPersistence,
                RankPromotionRejection.InvalidCurrentRank => _text.RejectedInvalidActor,
                _ => throw new ArgumentOutOfRangeException(nameof(rejection), rejection, null)
            };
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

        private PlayerProfileState SellUniqueItem(GuildProfileEditRequest request)
        {
            if (_itemCatalog == null)
                throw new InvalidOperationException("Item catalog is not configured.");
            if (!_session.State.Inventory.TryGetInstance(request.ItemInstanceId, out var item))
                throw new ArgumentException("Item instance is not owned.");
            if (!_itemCatalog.TryGetEquipment(item.DefinitionId, out var definition))
                throw new ArgumentException("Item definition is not configured.");

            return _session.State.SellUniqueItem(request.ItemInstanceId, definition.SaleValue);
        }

        private PlayerProfileState SellResource(GuildProfileEditRequest request)
        {
            if (_itemCatalog == null)
                throw new InvalidOperationException("Item catalog is not configured.");
            if (!_session.State.Inventory.TryGetResource(request.DefinitionId, out var resource))
                throw new ArgumentException("Resource stack is not owned.");
            if (!_itemCatalog.TryGetResource(request.DefinitionId, out var definition))
                throw new ArgumentException("Resource definition is not configured.");

            var saleValue = checked((long)definition.SaleValue * resource.Quantity);
            return _session.State.SellResource(request.DefinitionId, saleValue);
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
