using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DefaultNamespace.UI
{
    public partial class UIManager
    {
        [SerializeField] private UIJawCurtain jawCurtainPrefab;
        [SerializeField] private UIJawCurtainTipConfig jawCurtainTipConfig;
        private UIJawCurtain jawCurtainInstance;
        private UIJawCurtainTipProvider jawCurtainTipProvider;

        public UniTask CloseJawCurtain(UIJawCurtainTipCategory tipCategory)
        {
            UIJawCurtainTip tip = GetJawCurtainTipProvider().GetTip(tipCategory);
            return CloseJawCurtain(tip.Text, tip.Sprite);
        }

        public void SnapJawCurtainClosed(UIJawCurtainTipCategory tipCategory)
        {
            UIJawCurtainTip tip = GetJawCurtainTipProvider().GetTip(tipCategory);
            SnapJawCurtainClosed(tip.Text, tip.Sprite);
        }

        public UniTask CloseJawCurtain(string tipText, Sprite tipSprite = null)
        {
            UIJawCurtain jawCurtain = GetJawCurtain();
            jawCurtain.transform.SetAsLastSibling();
            return jawCurtain.Close(tipText, tipSprite);
        }

        public void SnapJawCurtainClosed(string tipText, Sprite tipSprite = null)
        {
            UIJawCurtain jawCurtain = GetJawCurtain();
            jawCurtain.transform.SetAsLastSibling();
            jawCurtain.SnapClosed(tipText, tipSprite);
        }

        public UniTask OpenJawCurtain()
        {
            UIJawCurtain jawCurtain = GetJawCurtain();
            jawCurtain.transform.SetAsLastSibling();
            return jawCurtain.Open();
        }

        private UIJawCurtainTipProvider GetJawCurtainTipProvider()
        {
            if (jawCurtainTipProvider == null)
                jawCurtainTipProvider = new UIJawCurtainTipProvider(jawCurtainTipConfig);
            return jawCurtainTipProvider;
        }

        private UIJawCurtain GetJawCurtain()
        {
            if (jawCurtainInstance != null) return jawCurtainInstance;

            jawCurtainInstance = Instantiate(jawCurtainPrefab, overlayRoot);
            jawCurtainInstance.transform.SetAsLastSibling();
            jawCurtainInstance.gameObject.SetActive(false);
            jawCurtainInstance.SnapOpen();
            jawCurtainInstance.gameObject.SetActive(true);
            return jawCurtainInstance;
        }
    }
}
