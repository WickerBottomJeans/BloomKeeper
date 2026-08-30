using System;
using System.Collections.Generic;
using System.Threading;
using Boosters;
using Cysharp.Threading.Tasks;
using DefaultNamespace.UI;
using DefaultNamespace.Utility;
using UnityEngine;

namespace DefaultNamespace
{
    public class BoosterFlow
    {
        private enum State
        {
            Inactive,
            Idle,
            Targeting,
            Authorizing,
            AwaitingAuthorizationRetry,
            Applying
        }

        private readonly BoosterUseService boosterUseService;
        private readonly GameBoard gameBoard;
        private CancellationTokenSource lifetimeCancellation;
        private State state;

        public BoosterFlow(BoosterUseService boosterUseService, GameBoard gameBoard)
        {
            this.boosterUseService = boosterUseService ?? throw new ArgumentNullException(nameof(boosterUseService));
            this.gameBoard = gameBoard ?? throw new ArgumentNullException(nameof(gameBoard));
        }

        /// <summary>
        /// [Duong] Activates the booster flow, subscribes to booster UI requests, and enters its idle state.
        /// </summary>
        public void Start()
        {
            if (state != State.Inactive) throw new InvalidOperationException($"Cannot start the booster flow while it is {state}.");

            lifetimeCancellation = new CancellationTokenSource();
            UIManager.Instance.BoosterUseRequested += HandleBoosterUseRequested;
            UIManager.Instance.BoosterCancelRequested += HandleBoosterCancelRequested;
            state = State.Idle;
        }

        /// <summary>
        /// [Duong] Stops the booster flow, cancels active targeting, and returns it to its inactive state.
        /// </summary>
        public void Stop()
        {
            if (state == State.Inactive) return;
            if (state == State.Authorizing || state == State.AwaitingAuthorizationRetry || state == State.Applying) throw new InvalidOperationException("Cannot stop the booster flow while an authorization transaction is unresolved.");

            UIManager.Instance.BoosterUseRequested -= HandleBoosterUseRequested;
            UIManager.Instance.BoosterCancelRequested -= HandleBoosterCancelRequested;
            lifetimeCancellation.Cancel();
            if (state == State.Targeting)
                gameBoard.CancelBoosterTargeting();
            lifetimeCancellation.Dispose();
            lifetimeCancellation = null;
            state = State.Inactive;
        }

        
        /// <summary>
        /// [Duong] Player requested to use a booster
        /// </summary>
        private void HandleBoosterUseRequested(BoosterType boosterType)
        {
            if (state != State.Idle) throw new InvalidOperationException($"Cannot begin a booster use while the booster flow is {state}.");
            ApplicationOperationRunner.Instance.Run(() => RunBoosterUse(boosterType, lifetimeCancellation.Token));
        }

        /// <summary>
        /// [Duong] Tells the board to cancel targeting, which completes RunBoosterUse's pending wait
        /// </summary>
        private void HandleBoosterCancelRequested()
        {
            if (state != State.Targeting) throw new InvalidOperationException("A booster use can only be canceled while targeting.");
            gameBoard.CancelBoosterTargeting();
        }

        /// <summary>
        /// [Duong] Runs the booster-use flow from local validation through target selection, server authorization, and application.
        /// </summary>
        private async UniTask RunBoosterUse(BoosterType boosterType, CancellationToken cancellationToken)
        {
            // Check whether the booster can be used
            boosterUseService.ValidateUseRequest(boosterType);

            // Start target selection.
            state = State.Targeting;
            UIManager.Instance.EnterBoosterTargeting(boosterType);

            BoosterTargetSelectionResult targetSelection;
            try
            {
                targetSelection = await gameBoard.TrySelectBoosterTargets(boosterType);
                cancellationToken.ThrowIfCancellationRequested();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch
            {
                UIManager.Instance.ExitBoosterTargeting();
                throw;
            }

            // Exit when the board cannot begin targeting.
            if (targetSelection.IsUnavailable)
            {
                UIManager.Instance.ExitBoosterTargeting();
                state = State.Idle;
                return;
            }

            // Exit when the player cancels targeting.
            if (targetSelection.IsCanceled)
            {
                UIManager.Instance.ExitBoosterTargeting();
                state = State.Idle;
                return;
            }

            // Pause while authorization is pending.
            state = State.Authorizing;
            UIManager.Instance.EnterBoosterAuthorizationPending();
            GameTimeService.RequestPause(this);
            
            
            try
            {
                // Ask the server to authorize the booster use.
                BoosterAuthorizationResult result = await AuthorizeWithRetry(boosterType, targetSelection.Targets);

                // Apply the authorization result.
                state = State.Applying;
                UIManager.Instance.RefreshLevelBoosters(boosterUseService.GetViewData());
                UIManager.Instance.ExitBoosterTargeting();
                if (result.Consumed)
                    gameBoard.ExecuteApprovedBooster(result.BoosterType, result.Targets);
                else
                    gameBoard.RejectPendingBoosterUse();
                state = State.Idle;
            }
            finally
            {
                GameTimeService.ReleasePause(this);
            }
        }

        /// <summary>
        /// [Duong] Requests server authorization and keeps retrying when the transaction outcome is unknown.
        /// Cancel is unsafe because the server may already have consumed the booster.
        /// </summary>
        private async UniTask<BoosterAuthorizationResult> AuthorizeWithRetry(BoosterType boosterType, IReadOnlyList<Vector2Int> targets)
        {
            try
            {
                return await ApplicationPresentationService.Instance.RunWithLoading(() => boosterUseService.BeginBoosterAuthorization(boosterType, targets).AsTask());
            }
            catch (PlayFabRequestException exception) when (exception.IsRetryable)
            {
                Debug.LogWarning(exception);
                state = State.AwaitingAuthorizationRetry;
                return await RunAuthorizationRetryDialog();
            }
        }

        /// <summary>
        /// [Duong] Shows a retry-only dialog until the pending booster authorization succeeds
        /// </summary>
        private async UniTask<BoosterAuthorizationResult> RunAuthorizationRetryDialog()
        {
            DialogOptionButton[] options = { DialogOptionButton.Retry };
            while (true)
            {
                DialogButtonType buttonType = await DialogManager.Instance.RunDialog("Connection interrupted", "The booster has not been applied yet. Retry to safely confirm the same use.", options);
                if (buttonType != DialogButtonType.Retry) throw new ArgumentOutOfRangeException(nameof(buttonType), buttonType, "Unsupported booster authorization retry button.");

                state = State.Authorizing;
                try
                {
                    return await ApplicationPresentationService.Instance.RunWithLoading(() => boosterUseService.RetryPendingBoosterAuthorization().AsTask());
                }
                catch (PlayFabRequestException exception) when (exception.IsRetryable)
                {
                    Debug.LogWarning(exception);
                    state = State.AwaitingAuthorizationRetry;
                }
            }
        }
    }
}
