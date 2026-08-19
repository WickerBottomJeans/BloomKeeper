using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DefaultNamespace.UI;

namespace DefaultNamespace
{
    public class HomeShopFlow
    {
        private const string MainShopId = "main";
        private readonly PlayFabShopService shopService = new PlayFabShopService();
        private LoadShopResponse cachedMainShopResponse;
        private CancellationTokenSource cachedMainShopExpiryCancellation;

        public async UniTask<bool> TryEnterShopAsync()
        {
            while (true)
            {
                try
                {
                    if (cachedMainShopResponse == null) await LoadAndCacheMainShopAsync();
                    UIManager.Instance.DisplayHomeShop(cachedMainShopResponse);
                    return true;
                }
                catch (ShopLoadException exception) when (exception.IsRetryable)
                {
                    if (!await RunRetryDialog()) return false;
                }
                catch (ShopLoadException exception)
                {
                    await RunInformationDialog("Shop unavailable", exception.Message);
                    return false;
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

        private async UniTask<bool> RunRetryDialog()
        {
            bool shouldRetry = false;
            DialogOptionButton[] options = { DialogOptionButton.Cancel, DialogOptionButton.Retry };
            await DialogManager.Instance.RunDialogWorkflow("Shop unavailable", "The shop could not be loaded. Please try again.", async session =>
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
