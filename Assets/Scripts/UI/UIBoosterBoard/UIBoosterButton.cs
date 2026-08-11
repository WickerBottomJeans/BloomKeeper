using System;
using DG.Tweening;
using DefaultNamespace;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public sealed class UIBoosterButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text amountText;
    [SerializeField] private BoosterButtonConfig config;
    [SerializeField] private CanvasGroup canvasGroup;

    public event Action<BoosterType> BoosterUseRequested;

    private BoosterViewData viewData;

    public BoosterType BoosterType => viewData.BoosterType;
    public BoosterViewData ViewData => viewData;
    public RectTransform RectTransform => (RectTransform)transform;

    private void Awake()
    {
        button.onClick.AddListener(HandleClicked);
    }

    public void Configure(BoosterViewData data)
    {
        viewData = data ?? throw new ArgumentNullException(nameof(data));
        icon.sprite = config.GetIcon(data.BoosterType);
        amountText.text = data.Amount.ToString();
    }

    public string GetGuidanceText() => config.GetGuidanceText(viewData.BoosterType);
    public void SetVisualAlpha(float alpha) => canvasGroup.alpha = alpha;
    public Tween FadeVisual(float alpha, float duration) => canvasGroup.DOFade(alpha, duration);

    public void SetInputEnabled(bool enabled)
    {
        canvasGroup.interactable = enabled;
        canvasGroup.blocksRaycasts = enabled;
    }

    public void Show() => gameObject.SetActive(true);
    public void Hide() => gameObject.SetActive(false);

    private void HandleClicked()
    {
        BoosterUseRequested?.Invoke(viewData.BoosterType);
    }

    private void OnDestroy()
    {
        button.onClick.RemoveListener(HandleClicked);
    }
}
