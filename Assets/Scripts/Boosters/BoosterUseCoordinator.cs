using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DefaultNamespace;
using UnityEngine;

namespace Boosters
{
    public sealed class BoosterUseCoordinator
    {
        private readonly IReadOnlyList<BoosterType> allowedBoosters;
        private readonly PlayFabBoosterInventoryService inventoryService;
        private BoosterType? activeBoosterType;
        private IReadOnlyList<Vector2Int> pendingTargets;
        
        /// <summary>
        /// Idempotency key for the pending server request to consume this booster.
        /// </summary>
        private string pendingOperationId;
        private BoosterUsePhase phase = BoosterUsePhase.Idle;

        public event Action<BoosterType> BoosterTargetingApproved;
        public event Action BoosterCancelApproved;

        public BoosterUseCoordinator(IReadOnlyList<BoosterType> allowedBoosters, PlayFabBoosterInventoryService inventoryService)
        {
            if (allowedBoosters == null) throw new ArgumentNullException(nameof(allowedBoosters));
            this.inventoryService = inventoryService ?? throw new ArgumentNullException(nameof(inventoryService));

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

        public void RequestUse(BoosterType boosterType)
        {
            if (phase != BoosterUsePhase.Idle) throw new InvalidOperationException("A booster use is already active.");
            if (!allowedBoosters.Contains(boosterType)) throw new ArgumentOutOfRangeException(nameof(boosterType), boosterType, "Booster is not allowed for this level.");
            int availableQuantity = PlayerAccountContext.Instance.GetCurrentBoosterInventory().GetQuantity(boosterType);
            if (availableQuantity <= 0) throw new InvalidOperationException($"Player has no {boosterType} boosters available.");

            activeBoosterType = boosterType;
            phase = BoosterUsePhase.Targeting;
            BoosterTargetingApproved?.Invoke(boosterType);
        }

        public void RequestCancel()
        {
            if (phase != BoosterUsePhase.Targeting) throw new InvalidOperationException("A booster use can only be canceled while targeting.");
            BoosterCancelApproved?.Invoke();
        }

        public void CompleteTargetingCancellation()
        {
            if (phase != BoosterUsePhase.Targeting) throw new InvalidOperationException("A booster targeting cancellation can only complete while targeting.");
            ClearActiveUse();
        }

        public UniTask<BoosterAuthorizationResult> AuthorizeBoosterUse(BoosterType boosterType, IReadOnlyList<Vector2Int> targets)
        {
            if (phase != BoosterUsePhase.Targeting) throw new InvalidOperationException("Booster authorization can only begin after targeting starts.");
            if (!activeBoosterType.HasValue || activeBoosterType.Value != boosterType) throw new InvalidOperationException("Selected booster does not match the active booster use.");
            if (targets == null) throw new ArgumentNullException(nameof(targets));

            pendingTargets = new List<Vector2Int>(targets).AsReadOnly();
            pendingOperationId = Guid.NewGuid().ToString("N");
            phase = BoosterUsePhase.Authorizing;
            return AuthorizePendingUse();
        }

        public UniTask<BoosterAuthorizationResult> RetryPendingAuthorization()
        {
            if (phase != BoosterUsePhase.AwaitingAuthorizationRetry) throw new InvalidOperationException("Booster authorization can only be retried after an interrupted request.");

            phase = BoosterUsePhase.Authorizing;
            return AuthorizePendingUse();
        }

        public void AbandonPendingUse()
        {
            if (phase != BoosterUsePhase.AwaitingAuthorizationRetry) throw new InvalidOperationException("A booster use can only be abandoned while awaiting an authorization retry.");
            ClearActiveUse();
        }

        private async UniTask<BoosterAuthorizationResult> AuthorizePendingUse()
        {
            try
            {
                BoosterType boosterType = activeBoosterType ?? throw new InvalidOperationException("Booster authorization requires an active booster type.");
                IReadOnlyList<Vector2Int> targets = pendingTargets ?? throw new InvalidOperationException("Booster authorization requires selected targets.");
                PlayerAccount account = PlayerAccountContext.Instance.CurrentAccount;
                (ConsumeBoosterOutcome outcome, ConsumeBoosterRejectionReason? _, BoosterInventoryData inventory) = await inventoryService.ConsumeBooster(account.AuthSession, pendingOperationId, boosterType);
                account.ReplaceBoosterInventory(inventory);
                var result = new BoosterAuthorizationResult(boosterType, targets, outcome == ConsumeBoosterOutcome.Consumed);
                ClearActiveUse();
                return result;
            }
            catch
            {
                phase = BoosterUsePhase.AwaitingAuthorizationRetry;
                throw;
            }
        }

        private void ClearActiveUse()
        {
            activeBoosterType = null;
            pendingTargets = null;
            pendingOperationId = null;
            phase = BoosterUsePhase.Idle;
        }
    }
}
