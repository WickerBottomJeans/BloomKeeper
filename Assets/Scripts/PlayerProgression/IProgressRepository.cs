namespace DefaultNamespace
{
    //TODO: server save system
    /// <summary>
    /// I was planning to implement a server-authoritative save system too
    /// </summary>
    public interface IProgressRepository
    {
        ProgressData Load();
        void Save(ProgressData progress);
    }
}