using System;
using System.Collections.Generic;
using DungeonTeam.Gameplay.Contracts.Application;
using DungeonTeam.Gameplay.Contracts.Domain;
using DungeonTeam.Gameplay.PlayerProfile.Application;

namespace Code.ApplicationRoot
{
    internal enum ContractRewardClaimStatus
    {
        Rejected = 0,
        Applied = 1,
        AlreadyApplied = 2
    }

    internal sealed class ContractRewardClaimResult
    {
        private ContractRewardClaimResult(ContractRewardClaimStatus status)
        {
            Status = status;
        }

        public ContractRewardClaimStatus Status { get; }
        public bool Accepted => Status != ContractRewardClaimStatus.Rejected;

        public static ContractRewardClaimResult Rejected() =>
            new(ContractRewardClaimStatus.Rejected);

        public static ContractRewardClaimResult Applied() =>
            new(ContractRewardClaimStatus.Applied);

        public static ContractRewardClaimResult AlreadyApplied() =>
            new(ContractRewardClaimStatus.AlreadyApplied);
    }

    internal sealed class ContractRewardClaimCoordinator
    {
        private readonly ContractSession _contracts;
        private readonly ContractCatalog _catalog;
        private readonly PlayerProfileSession _profile;

        public ContractRewardClaimCoordinator(
            ContractSession contracts,
            ContractCatalog catalog,
            PlayerProfileSession profile)
        {
            _contracts = contracts ?? throw new ArgumentNullException(nameof(contracts));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _profile = profile ?? throw new ArgumentNullException(nameof(profile));
        }

        public ContractRewardClaimResult Claim(
            string contractId,
            ContractRewardClaimPoint point)
        {
            if (string.IsNullOrWhiteSpace(contractId) || point == null || !_catalog.Contains(contractId))
            {
                return ContractRewardClaimResult.Rejected();
            }

            var definition = _catalog.Require(contractId);
            if (definition.Reward == null ||
                !_contracts.State.IsCompleted(contractId) ||
                _contracts.State.IsRewardClaimed(contractId) ||
                !definition.Reward.ClaimPoint.Matches(point))
            {
                return ContractRewardClaimResult.Rejected();
            }

            var grants = new ProfileResourceGrant[definition.Reward.Resources.Count];
            for (var index = 0; index < grants.Length; index++)
            {
                var grant = definition.Reward.Resources[index];
                grants[index] = new ProfileResourceGrant(grant.DefinitionId, grant.Amount);
            }

            var profileResult = _profile.ClaimReward(new ProfileRewardClaimRequest(
                $"contract.reward:{definition.ContractId}",
                definition.Reward.GoldAmount,
                grants));

            // Contract state is deliberately marked only after Profile has durably
            // applied (or already observed) the stable claim ID. If this save fails,
            // retrying is safe because Profile returns AlreadyApplied.
            _contracts.MarkRewardClaimed(definition.ContractId, _catalog);
            return profileResult.Status == ProfileRewardClaimStatus.AlreadyApplied
                ? ContractRewardClaimResult.AlreadyApplied()
                : ContractRewardClaimResult.Applied();
        }

        public IReadOnlyList<string> GetClaimableAt(ContractRewardClaimPoint point)
        {
            if (point == null)
            {
                throw new ArgumentNullException(nameof(point));
            }

            return _contracts.State.GetClaimableAt(point, _catalog);
        }
    }
}
