using System;
using UI.Components;
using UI.UIForTester;
using UnityEngine;
using Utility;

namespace DefaultNamespace.UI
{
    public partial class UIManager
    {
        [SerializeField] private UIPetalEditor petalEditorPrefab;
        private UIPetalEditor petalEditorInstance;

        public event Action<PetalType, SpecialSkillType> OnPetalEditConfirmed;

        public void ShowPetalEditorPopup(Vector2 screenPos)
        {
            bool isNewPetalEditorInstance = petalEditorInstance == null;
            GetPanel(ref petalEditorInstance, petalEditorPrefab, uiRoot);
            if (isNewPetalEditorInstance)
            {
                petalEditorInstance.OnConfirmed += (petalType, skillType) =>
                {
                    OnPetalEditConfirmed?.Invoke(petalType, skillType);
                    HidePetalEditor();
                };
                petalEditorInstance.OnDismissed += HidePetalEditor;
            }

            PositionPetalEditor(screenPos);
            petalEditorInstance.Show();
        }

        public void HidePetalEditor()
        {
            if (petalEditorInstance == null) return;
            petalEditorInstance.Hide();
        }

        private void PositionPetalEditor(Vector2 screenPos)
        {
            UIPositionHelper.ConvertWorldToCanvasAndClampPopupPosition(
                petalEditorInstance.GetComponent<RectTransform>(),
                canvas,
                screenPos, 3f
            );
        }
    }
}
