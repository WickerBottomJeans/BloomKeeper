using Cysharp.Threading.Tasks;

namespace DefaultNamespace
{
    public class ChapterIndexLoader
    {
        private const string ChapterIndexPath = "chapters.json";
        private readonly RemoteJsonLoader remoteJsonLoader;

        public ChapterIndexLoader(RemoteJsonLoader remoteJsonLoader)
        {
            this.remoteJsonLoader = remoteJsonLoader;
        }

        public UniTask<ChapterIndex> LoadAsync()
        {
            return remoteJsonLoader.LoadAsync<ChapterIndex>(ChapterIndexPath);
        }
    }
}
