using System;
using DungeonTeam.Gameplay.DungeonRun.Application;
using DungeonTeam.Gameplay.GuildHall.Application;
using DungeonTeam.Gameplay.PlayerProfile.Application;
using DungeonTeam.Gameplay.PlayerProfile.Domain;

namespace Code.ApplicationRoot
{
    internal sealed class GuildProfileEditHandler
    {
        private readonly PlayerProfileSession _session;
        private readonly DungeonRunTeamSetup _teamSetup;
        private readonly GuildProfileTextSnapshot _text;
        private readonly Func<PlayerProfileState, GuildProfileSnapshot> _buildSnapshot;
        private readonly Action<Exception> _reportPersistenceFailure;

        public GuildProfileEditHandler(
            PlayerProfileSession session,
            DungeonRunTeamSetup teamSetup,
            GuildProfileTextSnapshot text,
            Func<PlayerProfileState, GuildProfileSnapshot> buildSnapshot,
            Action<Exception> reportPersistenceFailure)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _teamSetup = teamSetup ?? throw new ArgumentNullException(nameof(teamSetup));
            _text = text ?? throw new ArgumentNullException(nameof(text));
            _buildSnapshot = buildSnapshot ?? throw new ArgumentNullException(nameof(buildSnapshot));
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

            if (ReferenceEquals(candidate, _session.State))
            {
                return GuildProfileEditResult.Accept(_buildSnapshot(_session.State));
            }

            var selection = PlayerProfileComposition.MapToTeamSelection(candidate);
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
