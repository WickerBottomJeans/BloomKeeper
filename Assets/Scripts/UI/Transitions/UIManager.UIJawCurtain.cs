using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DefaultNamespace.UI
{
    public partial class UIManager
    {
        [SerializeField] private UIJawCurtain jawCurtain;
        [SerializeField] private UIJawCurtainTipConfig jawCurtainTipConfig;
        private UIJawCurtainTipProvider jawCurtainTipProvider;
        private bool isPlayingJawCurtainTransition;

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
            jawCurtain.transform.SetAsLastSibling();
            return jawCurtain.Close(tipText, tipSprite);
        }

        public void SnapJawCurtainClosed(string tipText, Sprite tipSprite = null)
        {
            jawCurtain.transform.SetAsLastSibling();
            jawCurtain.SnapClosed(tipText, tipSprite);
        }

        public UniTask OpenJawCurtain()
        {
            jawCurtain.transform.SetAsLastSibling();
            return jawCurtain.Open();
        }

        public async UniTask PlayJawCurtainTransition(UIJawCurtainTipCategory tipCategory, Func<UniTask> whileClosed)
        {
            if (isPlayingJawCurtainTransition) return;

            isPlayingJawCurtainTransition = true;
            bool curtainClosed = false;

            try
            {
                await CloseJawCurtain(tipCategory);
                curtainClosed = true;
                await whileClosed();
                await OpenJawCurtain();
                curtainClosed = false;
            }
            finally
            {
                try
                {
                    if (curtainClosed)
                        await OpenJawCurtain();
                }
                finally
                {
                    isPlayingJawCurtainTransition = false;
                }
            }
        }

        private UIJawCurtainTipProvider GetJawCurtainTipProvider()
        {
            if (jawCurtainTipProvider == null)
                jawCurtainTipProvider = new UIJawCurtainTipProvider(jawCurtainTipConfig);
            return jawCurtainTipProvider;
        }
    }
}
