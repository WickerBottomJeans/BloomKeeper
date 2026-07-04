using System.Threading.Tasks;
using DefaultNamespace.UI;

namespace DefaultNamespace
{
    public class BootFlow
    {
        public async Task Enter()
        {
            await SpriteLoader.Instance.LoadAll();

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            UIManager.Instance.ShowTesterToggle();
#endif
        }
    }
}
