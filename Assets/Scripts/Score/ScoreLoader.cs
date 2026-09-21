using Cysharp.Threading.Tasks;

namespace DefaultNamespace
{
    public class ScoreLoader
    {
        private const string ScoreConfigPath = "score_config.json";
        private readonly RemoteJsonLoader remoteJsonLoader;

        public ScoreLoader(RemoteJsonLoader remoteJsonLoader)
        {
            this.remoteJsonLoader = remoteJsonLoader;
        }

        public UniTask<ScoreConfigJson> LoadScoreConfigAsync()
        {
            return remoteJsonLoader.LoadAsync<ScoreConfigJson>(ScoreConfigPath);
        }
    }
}
