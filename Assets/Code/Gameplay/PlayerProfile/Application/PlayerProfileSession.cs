using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DungeonTeam.Gameplay.PlayerProfile.Domain;
using DungeonTeam.Gameplay.Inventory.Domain;

namespace DungeonTeam.Gameplay.PlayerProfile.Application
{
    public readonly struct ProfileResourceGrant
    {
        public ProfileResourceGrant(string definitionId, int amount)
        {
            DefinitionId = !string.IsNullOrWhiteSpace(definitionId)
                ? definitionId
                : throw new ArgumentException("Resource definition ID cannot be empty.", nameof(definitionId));
            Amount = amount > 0
                ? amount
                : throw new ArgumentOutOfRangeException(nameof(amount));
        }

        public string DefinitionId { get; }
        public int Amount { get; }
    }

    public sealed class ProfileTerminalResultRequest
    {
        private readonly ReadOnlyCollection<ProfileResourceGrant> _resourceGrants;

        public ProfileTerminalResultRequest(
            string runId,
            long goldAmount,
            IReadOnlyList<ProfileResourceGrant> resourceGrants)
        {
            RunId = RequireRunId(runId);
            GoldAmount = goldAmount >= 0
                ? goldAmount
                : throw new ArgumentOutOfRangeException(nameof(goldAmount));
            _resourceGrants = CopyGrants(resourceGrants);
        }

        public string RunId { get; }
        public long GoldAmount { get; }
        public IReadOnlyList<ProfileResourceGrant> ResourceGrants => _resourceGrants;

        internal PendingTerminalResultState ToPendingState()
        {
            var grants = new ResourceStackState[_resourceGrants.Count];
            for (var index = 0; index < grants.Length; index++)
            {
                grants[index] = new ResourceStackState(
                    _resourceGrants[index].DefinitionId,
                    _resourceGrants[index].Amount);
            }

            return new PendingTerminalResultState(RunId, GoldAmount, grants);
        }

        private static ReadOnlyCollection<ProfileResourceGrant> CopyGrants(
            IReadOnlyList<ProfileResourceGrant> grants)
        {
            if (grants == null)
            {
                throw new ArgumentNullException(nameof(grants));
            }

            var copy = new ProfileResourceGrant[grants.Count];
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < copy.Length; index++)
            {
                copy[index] = grants[index];
                if (!ids.Add(copy[index].DefinitionId))
                {
                    throw new ArgumentException(
                        "Terminal resource grant IDs must be unique.",
                        nameof(grants));
                }
            }

            return Array.AsReadOnly(copy);
        }

        private static string RequireRunId(string runId) => !string.IsNullOrWhiteSpace(runId)
            ? runId
            : throw new ArgumentException("Run ID cannot be empty.", nameof(runId));
    }

    public sealed class ProfileSettlementReceipt
    {
        private readonly ReadOnlyCollection<ProfileResourceGrant> _resourceGrants;

        internal ProfileSettlementReceipt(PendingTerminalResultState pending)
        {
            if (pending == null)
            {
                throw new ArgumentNullException(nameof(pending));
            }

            RunId = pending.RunId;
            GoldAmount = pending.GoldAmount;
            var grants = new ProfileResourceGrant[pending.ResourceGrants.Count];
            for (var index = 0; index < grants.Length; index++)
            {
                var grant = pending.ResourceGrants[index];
                grants[index] = new ProfileResourceGrant(grant.DefinitionId, grant.Quantity);
            }

            _resourceGrants = Array.AsReadOnly(grants);
        }

        public string RunId { get; }
        public long GoldAmount { get; }
        public IReadOnlyList<ProfileResourceGrant> ResourceGrants => _resourceGrants;
    }

    public enum ProfileSettlementStatus
    {
        Applied = 0,
        AlreadyApplied = 1
    }

    public sealed class ProfileSettlementResult
    {
        private ProfileSettlementResult(ProfileSettlementStatus status, ProfileSettlementReceipt receipt)
        {
            Status = status;
            Receipt = receipt;
        }

        public ProfileSettlementStatus Status { get; }
        public ProfileSettlementReceipt Receipt { get; }
        public bool IsApplied => Status == ProfileSettlementStatus.Applied;

        internal static ProfileSettlementResult Applied(ProfileSettlementReceipt receipt) =>
            new(ProfileSettlementStatus.Applied, receipt ?? throw new ArgumentNullException(nameof(receipt)));

        internal static ProfileSettlementResult AlreadyApplied() =>
            new(ProfileSettlementStatus.AlreadyApplied, null);
    }

    public interface IPlayerProfileRepository
    {
        bool TryLoad(out PlayerProfileState state);
        void Save(PlayerProfileState state);
    }

    public sealed class PlayerProfileSeed
    {
        public PlayerProfileSeed(
            IReadOnlyList<HeroProfileState> heroes,
            string leaderActorId,
            IReadOnlyList<string> companionActorIds,
            InventoryState inventory = null)
        {
            State = new PlayerProfileState(
                0,
                null,
                heroes,
                leaderActorId,
                companionActorIds,
                inventory ?? InventoryState.Empty);
        }

        public PlayerProfileState State { get; }
    }

    public sealed class PlayerProfileSession
    {
        private readonly IPlayerProfileRepository _repository;

        public PlayerProfileSession(IPlayerProfileRepository repository, PlayerProfileSeed seed)
        {
            if (repository == null)
            {
                throw new ArgumentNullException(nameof(repository));
            }

            if (seed == null)
            {
                throw new ArgumentNullException(nameof(seed));
            }

            _repository = repository;
            if (!repository.TryLoad(out var state))
            {
                state = seed.State;
                repository.Save(state);
            }

            State = state ?? throw new InvalidOperationException("Loaded player profile is missing.");
            RecoverPendingTerminalResult();
        }

        public PlayerProfileState State { get; private set; }

        public void Commit(PlayerProfileState candidate)
        {
            if (candidate == null)
            {
                throw new ArgumentNullException(nameof(candidate));
            }

            if (ReferenceEquals(candidate, State))
            {
                return;
            }

            _repository.Save(candidate);
            State = candidate;
        }

        public ProfileSettlementResult BankTerminalResult(ProfileTerminalResultRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var pending = State.PendingTerminalResult;
            if (pending != null && !string.Equals(pending.RunId, request.RunId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Terminal result '{pending.RunId}' is pending and cannot be overwritten by '{request.RunId}'.");
            }

            if (string.Equals(State.LastAppliedRunId, request.RunId, StringComparison.Ordinal))
            {
                return ProfileSettlementResult.AlreadyApplied();
            }

            if (pending == null)
            {
                pending = request.ToPendingState();
                Commit(State.WithTerminalState(pending, State.LastAppliedRunId));
            }

            var candidate = State.ApplyPendingTerminalResult(pending);
            Commit(candidate);
            return ProfileSettlementResult.Applied(new ProfileSettlementReceipt(pending));
        }

        public void RecoverPendingTerminalResult()
        {
            var pending = State.PendingTerminalResult;
            if (pending == null)
            {
                return;
            }

            var candidate = string.Equals(State.LastAppliedRunId, pending.RunId, StringComparison.Ordinal)
                ? State.WithTerminalState(null, State.LastAppliedRunId)
                : State.ApplyPendingTerminalResult(pending);
            Commit(candidate);
        }
    }
}
