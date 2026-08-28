using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DefaultNamespace.UI;
using UnityEngine;

namespace DefaultNamespace
{
    /// <summary>
    /// [Duong] Owns loading, buying, retry decisions, and account refresh for the home shop.
    /// </summary>
    public class HomeShopFlow
    {
        private const string MainShopId = "main";
        private readonly PlayFabShopService shopService = new PlayFabShopService();
        private readonly PlayerLivesPresentationService playerLivesPresentationService;
        private LoadShopResponse cachedMainShopResponse;
        private CancellationTokenSource cachedMainShopExpiryCancellation;

        /// <summary>
        /// Creates the shop flow with lives presentation support.
        /// </summary>
        public HomeShopFlow(PlayerLivesPresentationService playerLivesPresentationService)
        {
            this.playerLivesPresentationService = playerLivesPresentationService ?? throw new ArgumentNullException(nameof(playerLivesPresentationService));
        }

        /// <summary>
        /// Starts listening for shop purchase requests from UI.
        /// </summary>
        public void Enter()
        {
            UIManager.Instance.HomeShopOfferBuyRequested += HandleShopOfferBuyRequested;
        }

        /// <summary>
        /// Stops listening for shop purchase requests from UI.
        /// </summary>
        public void Exit()
        {
            UIManager.Instance.HomeShopOfferBuyRequested -= HandleShopOfferBuyRequested;
        }

        /// <summary>
        /// [Duong] Loads and displays the main shop.
        /// </summary>
        /// <returns>Whether the shop was displayed.</returns>
        public async UniTask<bool> TryEnterShopAsync()
        {
            while (true)
            {
                try
                {
                    // [Duong] load shop data
                    if (cachedMainShopResponse == null) await LoadAndCacheMainShopAsync();
                    // [Duong] display shop
                    UIManager.Instance.DisplayHomeShop(cachedMainShopResponse);
                    return true;
                }
                catch (PlayFabRequestException exception) when (exception.IsRetryable)
                {
                    Debug.LogWarning(exception);
                    if (!await RunRetryDialog(ShopDialogText.ShopUnavailableTitle, ShopDialogText.ShopLoadRetryMessage)) return false;
                }
                catch (PlayFabRequestException exception)
                {
                    Debug.LogWarning(exception);
                    await RunInformationDialog(ShopDialogText.ShopUnavailableTitle, ShopDialogText.ShopLoadFailureMessage);
                    return false;
                }
            }
        }

        /// <summary>
        /// [Duong] Starts buying the offer
        /// </summary>
        private void HandleShopOfferBuyRequested(string offerId)
        {
            ApplicationOperationRunner.Instance.Run(() => TryBuyShopOfferAsync(offerId));
        }

        /// <summary>
        /// [Duong] Buys an offer and refreshes the affected local player state.
        /// </summary>
        private async UniTask TryBuyShopOfferAsync(string offerId)
        {
            // [Duong] Reuse the same purchase key for every retry.
            string buyShopOfferIdempotencyKey = Guid.NewGuid().ToString("N");
            while (true)
            {
                try
                {
                    //[Duong] Ask server to buy an offer
                    PlayerAccount account = PlayerAccountContext.Instance.CurrentAccount;
                    BuyShopOfferResponse response = await ApplicationPresentationService.Instance.RunWithLoading(() => shopService.BuyShopOffer(account.AuthSession, MainShopId, offerId, buyShopOfferIdempotencyKey));

                    // Apply the returned player state.
                    if (response.playerInventorySnapshot != null)
                    {
                        var playerInventoryData = new PlayerInventoryData(response.playerInventorySnapshot.quantitiesByCatalogId);
                        account.ReplacePlayerInventory(playerInventoryData);
                        UIManager.Instance.DisplayHomeCurrency(playerInventoryData.DiamondQuantity);
                    }
                    if (response.lives != null) playerLivesPresentationService.ReplaceServerLivesSnapshot(response.lives);

                    // Show the purchase result.
                    switch (response.outcome)
                    {
                        case BuyShopOfferOutcome.Purchased:
                            await RunInformationDialog(ShopDialogText.PurchaseSuccessTitle, ShopDialogText.PurchaseSuccessMessage);
                            return;
                        case BuyShopOfferOutcome.Rejected:
                            await RunInformationDialog(ShopDialogText.PurchaseFailureTitle, ShopDialogText.GetPurchaseRejectionMessage(response.rejectionReason.Value));
                            return;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(response.outcome), response.outcome, "Unsupported shop purchase outcome.");
                    }
                }
                catch (PlayFabRequestException exception) when (exception.IsRetryable)
                {
                    Debug.LogWarning(exception);
                    if (!await RunRetryDialog(ShopDialogText.PurchaseFailureTitle, ShopDialogText.PurchaseRetryMessage)) return;
                }
                catch (PlayFabRequestException exception)
                {
                    Debug.LogWarning(exception);
                    await RunInformationDialog(ShopDialogText.PurchaseFailureTitle, ShopDialogText.PurchaseFailureMessage);
                    return;
                }
            }
        }

        /// <summary>
        /// [Duong] Load the main shop data and cache it with an expiry timer.
        /// </summary>
        private async UniTask LoadAndCacheMainShopAsync()
        {
            PlayerAccount account = PlayerAccountContext.Instance.CurrentAccount;
            LoadShopResponse shopResponse = await ApplicationPresentationService.Instance.RunWithLoading(() => shopService.LoadShop(account.AuthSession, MainShopId));
            cachedMainShopResponse = shopResponse;
            StartCachedMainShopExpiryTimer(shopResponse, ConfigManager.Instance.MainShopCachePolicy.cacheLifetimeSeconds).Forget();
        }

        /// <summary>
        /// [Duong] Expire the cached main shop data after its cache lifetime.
        /// </summary>
        private async UniTask StartCachedMainShopExpiryTimer(LoadShopResponse shopResponse, int cacheLifetimeSeconds)
        {
            cachedMainShopExpiryCancellation?.Cancel();
            cachedMainShopExpiryCancellation?.Dispose();
            cachedMainShopExpiryCancellation = new CancellationTokenSource();
            CancellationToken cancellationToken = cachedMainShopExpiryCancellation.Token;
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(cacheLifetimeSeconds), true, cancellationToken: cancellationToken);
                if (ReferenceEquals(cachedMainShopResponse, shopResponse)) cachedMainShopResponse = null;
            }
            catch (OperationCanceledException)
            {
            }
        }

        /// <summary>
        /// Asks the player whether to retry a failed shop request.
        /// </summary>
        /// <returns>Whether the player chose Retry.</returns>
        private async UniTask<bool> RunRetryDialog(string title, string message)
        {
            bool shouldRetry = false;
            DialogOptionButton[] options = { DialogOptionButton.Cancel, DialogOptionButton.Retry };
            await DialogManager.Instance.RunDialogWorkflow(title, message, async session =>
            {
                int buttonId = await session.WaitForButtonClick();
                switch ((DialogButtonType)buttonId)
                {
                    case DialogButtonType.Cancel:
                        return;
                    case DialogButtonType.Retry:
                        shouldRetry = true;
                        return;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(buttonId), buttonId, "Unsupported shop retry dialog button.");
                }
            }, options);
            return shouldRetry;
        }

        /// <summary>
        /// Shows a shop message and waits for OK.
        /// </summary>
        private async UniTask RunInformationDialog(string title, string message)
        {
            DialogOptionButton[] options = { DialogOptionButton.Ok };
            await DialogManager.Instance.RunDialogWorkflow(title, message, async session =>
            {
                int buttonId = await session.WaitForButtonClick();
                if ((DialogButtonType)buttonId != DialogButtonType.Ok) throw new ArgumentOutOfRangeException(nameof(buttonId), buttonId, "Unsupported shop information dialog button.");
            }, options);
        }
    }
}
