using Cysharp.Threading.Tasks;

namespace DefaultNamespace
{
    public class LevelDataLoader
    {
        private readonly RemoteJsonLoader remoteJsonLoader;

        public LevelDataLoader(RemoteJsonLoader remoteJsonLoader)
        {
            this.remoteJsonLoader = remoteJsonLoader;
        }

        public UniTask<LevelData> LoadAsync(int levelId)
        {
            return remoteJsonLoader.LoadAsync<LevelData>($"levels/level_{levelId}.json");
        }
    }
}
