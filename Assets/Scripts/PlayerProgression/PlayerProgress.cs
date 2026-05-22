using System.Collections.Generic;
using DefaultNamespace.Utility;

namespace DefaultNamespace
{
    public class PlayerProgress : Singleton<PlayerProgress>
    {
        private IProgressRepository repository;
        private ProgressData data;

        protected override void Awake()
        {
            base.Awake();
            repository = new LocalProgressRepository();
            data = repository.Load();
        }

        public int GetStars(int levelId) => data.starsPerLevel.TryGetValue(levelId, out int s) ? s : 0;

        public void SetStars(int levelId, int stars)
        {
            data.starsPerLevel[levelId] = stars;
            repository.Save(data);
        }

        private void OnApplicationPause(bool pause) { if (pause) repository.Save(data); }
        private void OnApplicationQuit() => repository.Save(data);
    }
}