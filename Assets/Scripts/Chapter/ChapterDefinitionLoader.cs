using Cysharp.Threading.Tasks;

namespace DefaultNamespace
{
    public class ChapterDefinitionLoader
    {
        private readonly RemoteJsonLoader remoteJsonLoader;

        public ChapterDefinitionLoader(RemoteJsonLoader remoteJsonLoader)
        {
            this.remoteJsonLoader = remoteJsonLoader;
        }

        public UniTask<ChapterDefinition> LoadAsync(string configPath)
        {
            return remoteJsonLoader.LoadAsync<ChapterDefinition>(configPath);
        }
    }
}
