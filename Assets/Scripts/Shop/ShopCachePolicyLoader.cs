using Cysharp.Threading.Tasks;

namespace DefaultNamespace
{
    public class ShopCachePolicyLoader
    {
        private const string MainShopCachePolicyPath = "shops/main/cache_policy.json";
        private readonly RemoteJsonLoader remoteJsonLoader;

        public ShopCachePolicyLoader(RemoteJsonLoader remoteJsonLoader)
        {
            this.remoteJsonLoader = remoteJsonLoader;
        }

        public UniTask<ShopCachePolicyConfig> LoadMainShopCachePolicyAsync()
        {
            return remoteJsonLoader.LoadAsync<ShopCachePolicyConfig>(MainShopCachePolicyPath);
        }
    }
}
