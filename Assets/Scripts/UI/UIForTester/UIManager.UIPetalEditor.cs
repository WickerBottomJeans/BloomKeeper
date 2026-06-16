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
            if (petalEditorInstance != null)
            {
                petalEditorInstance.gameObject.SetActive(true);
                PositionPetalEditor(screenPos);
                ShowBackdrop(HidePetalEditor, petalEditorInstance.transform);
                return;
            }

            petalEditorInstance = Instantiate(petalEditorPrefab, canvas.transform);
            petalEditorInstance.OnConfirmed += (petalType, skillType) =>
            {
                OnPetalEditConfirmed?.Invoke(petalType, skillType);
                HidePetalEditor();
            };
            petalEditorInstance.OnDismissed += HidePetalEditor;
            PositionPetalEditor(screenPos);
            ShowBackdrop(HidePetalEditor, petalEditorInstance.transform);
        }

        public void HidePetalEditor()
        {
            if (petalEditorInstance == null) return;
            petalEditorInstance.gameObject.SetActive(false);
            HideBackdrop();
        }

        private void PositionPetalEditor(Vector2 screenPos)
        {
            UIPositionHelper.ConvertWorldToCanvasAndClampPopupPosition(
                petalEditorInstance.GetComponent<RectTransform>(),
                canvas,
                screenPos
            );
        }
    }
}