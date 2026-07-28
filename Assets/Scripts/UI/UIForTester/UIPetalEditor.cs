using System;
using System.Collections.Generic;
using DefaultNamespace;
using DefaultNamespace.Utility;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIForTester
{
    public class UIPetalEditor : MonoBehaviour
    {
        [SerializeField] private ScrollRect tableScrollRect;
        [SerializeField] private RectTransform tableContent;
        [SerializeField] private Button tileTemplate;
        [SerializeField] private TMP_Text headerTemplate;
        [SerializeField] private Button dismissButton;
        [SerializeField] private List<SpecialSkillType> selectableSkills;

        public event Action<PetalType, SpecialSkillType> OnConfirmed;
        public event Action OnDismissed;

        private void Awake()
        {
            ConfigureTable();
            BuildTable();

            dismissButton.onClick.AddListener(OnDismissClicked);
        }

        private void ConfigureTable()
        {
            tableScrollRect.horizontal = true;
            tableScrollRect.vertical = true;
            tableScrollRect.movementType = ScrollRect.MovementType.Clamped;

            GridLayoutGroup grid = tableContent.GetComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = selectableSkills.Count + 1;

            tileTemplate.gameObject.SetActive(false);
            headerTemplate.gameObject.SetActive(false);
        }

        private void BuildTable()
        {
            CreateHeader(string.Empty, "Corner");

            foreach (SpecialSkillType skillType in selectableSkills)
                CreateHeader(GetSkillLabel(skillType), $"Header_{skillType}");

            foreach (PetalType petalType in Enum.GetValues(typeof(PetalType)))
            {
                if (petalType == PetalType.None)
                    continue;

                CreateHeader(petalType.ToString(), $"Header_{petalType}");

                foreach (SpecialSkillType skillType in selectableSkills)
                    CreateSelectionTile(petalType, skillType);
            }
        }

        private void CreateHeader(string text, string objectName)
        {
            TMP_Text header = Instantiate(headerTemplate, tableContent);
            header.gameObject.name = objectName;
            header.text = text;
            header.gameObject.SetActive(true);
        }

        private void CreateSelectionTile(PetalType petalType, SpecialSkillType skillType)
        {
            Button button = Instantiate(tileTemplate, tableContent);
            button.gameObject.name = $"{petalType}_{skillType}";
            button.gameObject.SetActive(true);

            string spriteKey = SpriteKeyHelper.GetPetalSpriteKey(petalType, skillType);
            Sprite sprite = SpriteLoader.Instance.GetSprite(spriteKey);
            Image image = button.image;

            image.sprite = sprite;
            image.preserveAspect = true;
            button.interactable = sprite != null;

            if (sprite == null)
            {
                image.color = new Color(1f, 1f, 1f, 0.2f);
                return;
            }

            button.onClick.AddListener(() => OnConfirmed?.Invoke(petalType, skillType));
        }

        private static string GetSkillLabel(SpecialSkillType skillType)
        {
            return skillType switch
            {
                SpecialSkillType.None => "Normal",
                SpecialSkillType.StripedHorizontal => "Horizontal Stripe",
                SpecialSkillType.StripedVertical => "Vertical Stripe",
                _ => skillType.ToString()
            };
        }

        private void OnDismissClicked() => OnDismissed?.Invoke();

        private void OnDestroy()
        {
            dismissButton.onClick.RemoveListener(OnDismissClicked);
        }
    }
}
