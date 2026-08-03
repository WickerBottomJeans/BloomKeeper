using Cysharp.Threading.Tasks;

namespace DefaultNamespace
{
    public interface IChapterContentProvider
    {
        UniTask<ChapterContent> LoadChapterAsync(int chapterId);
    }
}
