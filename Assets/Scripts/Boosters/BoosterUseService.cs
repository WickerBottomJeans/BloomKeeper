using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DefaultNamespace;
using UnityEngine;

namespace Boosters
{
    /// <summary>
    /// [Duong] Validates booster uses and handles their server authorization
    /// </summary>
    public class BoosterUseService
    {
        private readonly IReadOnlyList<BoosterType> allowedBoosters;
        private readonly IBoosterConsumptionGateway consumptionGateway;
        private PendingBoosterAuthorization pendingBoosterAuthorization;

        public BoosterUseService(IReadOnlyList<BoosterType> allowedBoosters, IBoosterConsumptionGateway consumptionGateway)
        {
            if (allowedBoosters == null) throw new ArgumentNullException(nameof(allowedBoosters));
            this.consumptionGateway = consumptionGateway ?? throw new ArgumentNullException(nameof(consumptionGateway));

            var configuredBoosters = new List<BoosterType>(allowedBoosters.Count);
            foreach (BoosterType boosterType in allowedBoosters)
            {
                if (!Enum.IsDefined(typeof(BoosterType), boosterType)) throw new ArgumentOutOfRangeException(nameof(allowedBoosters), boosterType, "Level config contains an undefined booster type.");
                if (configuredBoosters.Contains(boosterType)) throw new ArgumentException($"Level config contains booster type {boosterType} more than once.", nameof(allowedBoosters));
                configuredBoosters.Add(boosterType);
            }

            this.allowedBoosters = configuredBoosters.AsReadOnly();
        }

        public IReadOnlyList<BoosterViewData> GetViewData()
        {
            BoosterInventoryData inventory = PlayerAccountContext.Instance.GetCurrentBoosterInventory();
            return allowedBoosters.Select(boosterType => new BoosterViewData(boosterType, inventory.GetQuantity(boosterType))).ToList().AsReadOnly();
        }

        /// <summary>
        /// [Duong] Just checks if this booster is allowed, and player actually have any to use
        /// </summary>
        public void ValidateUseRequest(BoosterType boosterType)
        {
            if (pendingBoosterAuthorization != null) throw new InvalidOperationException("A booster authorization transaction is unresolved.");
            if (!allowedBoosters.Contains(boosterType)) throw new ArgumentOutOfRangeException(nameof(boosterType), boosterType, "Booster is not allowed for this level.");
            int availableQuantity = PlayerAccountContext.Instance.GetCurrentBoosterInventory().GetQuantity(boosterType);
            if (availableQuantity <= 0) throw new InvalidOperationException($"Player has no {boosterType} boosters available.");
        }

        /// <summary>
        /// [Duong] Creates a pending booster authorization and starts the server request
        /// </summary>
        public UniTask<BoosterAuthorizationResult> BeginBoosterAuthorization(BoosterType boosterType, IReadOnlyList<Vector2Int> targets)
        {
            ValidateUseRequest(boosterType);
            if (targets == null) throw new ArgumentNullException(nameof(targets));

            pendingBoosterAuthorization = new PendingBoosterAuthorization(Guid.NewGuid().ToString("N"), boosterType, targets);
            return AuthorizePendingUse();
        }

        /// <summary>
        /// [Duong] Retries authorization for the pending booster use
        /// </summary>
        public UniTask<BoosterAuthorizationResult> RetryPendingBoosterAuthorization()
        {
            if (pendingBoosterAuthorization == null) throw new InvalidOperationException("There is no pending booster authorization transaction to retry.");
            return AuthorizePendingUse();
        }

        /// <summary>
        /// [Duong] Sends the pending authorization request and returns its result
        /// </summary>
        private async UniTask<BoosterAuthorizationResult> AuthorizePendingUse()
        {
            PendingBoosterAuthorization authorization = pendingBoosterAuthorization ?? throw new InvalidOperationException("Booster authorization requires a pending transaction.");
            PlayerAccount account = PlayerAccountContext.Instance.CurrentAccount;
            (ConsumeBoosterOutcome outcome, ConsumeBoosterRejectionReason? _, BoosterInventoryData inventory) = await consumptionGateway.ConsumeBooster(account.AuthSession, authorization.BoosterConsumptionIdempotencyKey, authorization.BoosterType);
            account.ReplaceBoosterInventory(inventory);
            var result = new BoosterAuthorizationResult(authorization.BoosterType, authorization.Targets, outcome == ConsumeBoosterOutcome.Consumed);
            pendingBoosterAuthorization = null;
            return result;
        }
        
        /// <summary>
        /// [Duong] Represents a booster use awaiting server authorization
        /// </summary>
        private class PendingBoosterAuthorization
        {
            public string BoosterConsumptionIdempotencyKey { get; }
            public BoosterType BoosterType { get; }
            public IReadOnlyList<Vector2Int> Targets { get; }

            public PendingBoosterAuthorization(string boosterConsumptionIdempotencyKey, BoosterType boosterType, IReadOnlyList<Vector2Int> targets)
            {
                BoosterConsumptionIdempotencyKey = boosterConsumptionIdempotencyKey;
                BoosterType = boosterType;
                Targets = new List<Vector2Int>(targets).AsReadOnly();
            }
        }
    }
}
