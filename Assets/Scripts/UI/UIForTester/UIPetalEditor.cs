using System;
using System.Collections.Generic;
using DefaultNamespace;
using TMPro;
using UI.Components;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIForTester
{
    public class UIPetalEditor : MonoBehaviour
    {
        [SerializeField] private TMP_Dropdown petalTypeDropdown;
        [SerializeField] private TMP_Dropdown skillTypeDropdown;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button dismissButton;
        public event Action<PetalType, SpecialSkillType> OnConfirmed;
        public event Action OnDismissed;

        private void Awake()
        {
            PopulateDropdown(petalTypeDropdown, Enum.GetNames(typeof(PetalType)));
            PopulateDropdown(skillTypeDropdown, Enum.GetNames(typeof(SpecialSkillType)));

            confirmButton.onClick.AddListener(OnConfirmClicked);
            dismissButton.onClick.AddListener(OnDismissClicked);
        }

        private void OnConfirmClicked()
        {
            PetalType petalType = (PetalType)petalTypeDropdown.value;
            SpecialSkillType skillType = (SpecialSkillType)skillTypeDropdown.value;
            OnConfirmed?.Invoke(petalType, skillType);
        }

        private void OnDismissClicked() => OnDismissed?.Invoke();

        private void PopulateDropdown(TMP_Dropdown dropdown, string[] options)
        {
            dropdown.ClearOptions();
            dropdown.AddOptions(new List<string>(options));
        }

        private void OnDestroy()
        {
            confirmButton.onClick.RemoveListener(OnConfirmClicked);
            dismissButton.onClick.RemoveListener(OnDismissClicked);
        }
    }
}