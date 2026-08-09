using System;
using DG.Tweening;
using DefaultNamespace;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public sealed class UIBoosterButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image icon;
    [SerializeField] private BoosterButtonConfig config;

    public event Action<BoosterType> BoosterUseRequested;

    private BoosterType boosterType;
    private CanvasGroup canvasGroup;

    public BoosterType BoosterType => boosterType;
    public RectTransform RectTransform => (RectTransform)transform;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        button.onClick.AddListener(HandleClicked);
    }

    public void Configure(BoosterType type)
    {
        boosterType = type;
        icon.sprite = config.GetIcon(type);
    }

    public string GetGuidanceText() => config.GetGuidanceText(boosterType);
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
        BoosterUseRequested?.Invoke(boosterType);
    }

    private void OnDestroy()
    {
        button.onClick.RemoveListener(HandleClicked);
    }
}
