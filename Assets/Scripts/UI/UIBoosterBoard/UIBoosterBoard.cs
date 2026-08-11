using System;
using System.Collections.Generic;
using DG.Tweening;
using DefaultNamespace;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIBoosterBoard : MonoBehaviour
{
    [SerializeField] private UIBoosterButton buttonTemplate;
    [SerializeField] private RectTransform buttonRoot;
    [SerializeField] private RectTransform boosterUseRoot;
    [SerializeField] private RectTransform selectedBoosterSlot;
    [SerializeField] private CanvasGroup userGuideGroup;
    [SerializeField] private TMP_Text userGuideText;
    [SerializeField] private CanvasGroup cancelButtonGroup;
    [SerializeField] private Button cancelButton;
    [SerializeField] private float targetingEnterDuration = 0.3f;
    [SerializeField] private float targetingExitDuration = 0.2f;
    [SerializeField] private Ease targetingEnterEase = Ease.OutCubic;
    [SerializeField] private Ease targetingExitEase = Ease.InCubic;

    private readonly List<UIBoosterButton> activeButtons = new List<UIBoosterButton>();
    private Sequence targetingTransition;
    private UIBoosterButton selectedButton;
    private UIBoosterButton selectedButtonCopy;

    #region Unity Lifecycle

    private void Awake()
    {
        buttonTemplate.gameObject.SetActive(false);
        boosterUseRoot.gameObject.SetActive(false);
        cancelButton.onClick.AddListener(HandleBoosterCancelRequested);
    }

    private void OnDestroy()
    {
        targetingTransition?.Kill();
        cancelButton.onClick.RemoveListener(HandleBoosterCancelRequested);
        foreach (UIBoosterButton boosterButton in activeButtons)
            boosterButton.BoosterUseRequested -= HandleBoosterUseRequested;
    }

    #endregion

    #region Public API

    public event Action<BoosterType> BoosterUseRequested;
    public event Action BoosterCancelRequested;

    public void Display(IReadOnlyList<BoosterViewData> availableBoosters)
    {
        ClearButtons();

        for (int i = 0; i < availableBoosters.Count; i++)
            SpawnButton(availableBoosters[i]);
    }

    public void EnterBoosterTargeting(BoosterType boosterType)
    {
        if (selectedButtonCopy != null) throw new InvalidOperationException("Booster targeting presentation is already active.");

        // Get the selected booster and guidance.
        selectedButton = FindButton(boosterType);
        string guidanceText = selectedButton.GetGuidanceText();

        // Show the targeting layout transparent.
        boosterUseRoot.gameObject.SetActive(true);
        userGuideText.text = guidanceText;
        userGuideGroup.alpha = 0f;
        cancelButtonGroup.alpha = 0f;
        cancelButtonGroup.interactable = true;
        cancelButtonGroup.blocksRaycasts = true;

        Canvas.ForceUpdateCanvases();

        // Spawn the copy over the selected booster.
        selectedButtonCopy = Instantiate(buttonTemplate, selectedBoosterSlot);
        selectedButtonCopy.Configure(selectedButton.ViewData);
        selectedButtonCopy.Show();
        selectedButtonCopy.SetInputEnabled(false);
        selectedButtonCopy.RectTransform.position = selectedButton.RectTransform.position;

        // Hide the selected original.
        selectedButton.SetVisualAlpha(0f);

        // Disable all booster buttons.
        foreach (UIBoosterButton boosterButton in activeButtons)
            boosterButton.SetInputEnabled(false);

        targetingTransition = DOTween.Sequence();

        // Fade out the other boosters.
        foreach (UIBoosterButton boosterButton in activeButtons)
            if (boosterButton != selectedButton)
                targetingTransition.Join(boosterButton.FadeVisual(0f, targetingEnterDuration));

        // Slide the copy into its slot.
        targetingTransition.Join(selectedButtonCopy.RectTransform.DOAnchorPos(Vector2.zero, targetingEnterDuration).SetEase(targetingEnterEase));

        // Fade in guidance and Cancel.
        targetingTransition.Join(userGuideGroup.DOFade(1f, targetingEnterDuration));
        targetingTransition.Join(cancelButtonGroup.DOFade(1f, targetingEnterDuration));
        targetingTransition.SetLink(gameObject, LinkBehaviour.KillOnDestroy);
    }

    public void EnterBoosterAuthorizationPending()
    {
        if (selectedButtonCopy == null) throw new InvalidOperationException("Booster targeting presentation is not active.");

        cancelButtonGroup.interactable = false;
        cancelButtonGroup.blocksRaycasts = false;
    }

    public void Refresh(IReadOnlyList<BoosterViewData> boosterViewData)
    {
        if (boosterViewData == null) throw new ArgumentNullException(nameof(boosterViewData));
        if (boosterViewData.Count != activeButtons.Count) throw new ArgumentException("Booster refresh data does not match the displayed booster count.", nameof(boosterViewData));

        foreach (BoosterViewData viewData in boosterViewData)
            FindButton(viewData.BoosterType).Configure(viewData);

        if (selectedButtonCopy != null)
        {
            selectedButtonCopy.Configure(selectedButton.ViewData);
            selectedButtonCopy.SetInputEnabled(false);
            foreach (UIBoosterButton boosterButton in activeButtons)
                boosterButton.SetInputEnabled(false);
            return;
        }

        foreach (UIBoosterButton boosterButton in activeButtons)
            boosterButton.SetInputEnabled(boosterButton.ViewData.Amount > 0);
    }

    public void ExitBoosterTargeting()
    {
        if (selectedButtonCopy == null) throw new InvalidOperationException("Booster targeting presentation is not active.");

        // Stop the entrance animation.
        targetingTransition?.Kill();

        // Disable Cancel.
        cancelButtonGroup.interactable = false;
        cancelButtonGroup.blocksRaycasts = false;

        targetingTransition = DOTween.Sequence();

        // Fade the other boosters back in.
        foreach (UIBoosterButton boosterButton in activeButtons)
            if (boosterButton != selectedButton)
                targetingTransition.Join(boosterButton.FadeVisual(1f, targetingExitDuration));

        // Slide the copy back.
        targetingTransition.Join(selectedButtonCopy.RectTransform.DOMove(selectedButton.RectTransform.position, targetingExitDuration).SetEase(targetingExitEase));

        // Fade out guidance and Cancel.
        targetingTransition.Join(userGuideGroup.DOFade(0f, targetingExitDuration));
        targetingTransition.Join(cancelButtonGroup.DOFade(0f, targetingExitDuration));
        targetingTransition.OnComplete(CompleteBoosterTargetingExit);
        targetingTransition.SetLink(gameObject, LinkBehaviour.KillOnDestroy);
    }

    public float GetHeightInWorldUnits()
    {
        Vector3[] corners = new Vector3[4];
        GetComponent<RectTransform>().GetWorldCorners(corners);
        return corners[1].y - corners[0].y;
    }

    public void Show() => gameObject.SetActive(true);
    public void Hide()
    {
        ResetBoosterTargetingPresentation();
        gameObject.SetActive(false);
    }

    #endregion

    #region Private Methods

    private void SpawnButton(BoosterViewData viewData)
    {
        UIBoosterButton boosterButton = Instantiate(buttonTemplate, buttonRoot);
        boosterButton.Configure(viewData);
        boosterButton.BoosterUseRequested += HandleBoosterUseRequested;
        activeButtons.Add(boosterButton);
        boosterButton.Show();
        boosterButton.SetInputEnabled(viewData.Amount > 0);
    }

    private void ClearButtons()
    {
        ResetBoosterTargetingPresentation();

        foreach (UIBoosterButton boosterButton in activeButtons)
        {
            boosterButton.BoosterUseRequested -= HandleBoosterUseRequested;
            boosterButton.Hide();
            Destroy(boosterButton.gameObject);
        }

        activeButtons.Clear();
    }

    private UIBoosterButton FindButton(BoosterType boosterType)
    {
        foreach (UIBoosterButton boosterButton in activeButtons)
            if (boosterButton.BoosterType == boosterType)
                return boosterButton;

        throw new InvalidOperationException($"UIBoosterBoard has no displayed button for booster type: {boosterType}.");
    }

    private void CompleteBoosterTargetingExit()
    {
        // Restore the original buttons.
        selectedButton.SetVisualAlpha(1f);
        foreach (UIBoosterButton boosterButton in activeButtons)
            boosterButton.SetInputEnabled(boosterButton.ViewData.Amount > 0);

        // Remove the copy and hide targeting UI.
        Destroy(selectedButtonCopy.gameObject);
        selectedButtonCopy = null;
        selectedButton = null;
        targetingTransition = null;
        boosterUseRoot.gameObject.SetActive(false);
    }

    private void ResetBoosterTargetingPresentation()
    {
        // Reset targeting visuals immediately.
        targetingTransition?.Kill();
        targetingTransition = null;

        if (selectedButton != null)
            selectedButton.SetVisualAlpha(1f);
        foreach (UIBoosterButton boosterButton in activeButtons)
        {
            boosterButton.SetVisualAlpha(1f);
            boosterButton.SetInputEnabled(boosterButton.ViewData.Amount > 0);
        }

        if (selectedButtonCopy != null)
            Destroy(selectedButtonCopy.gameObject);
        selectedButtonCopy = null;
        selectedButton = null;

        userGuideGroup.alpha = 0f;
        cancelButtonGroup.alpha = 0f;
        cancelButtonGroup.interactable = false;
        cancelButtonGroup.blocksRaycasts = false;
        boosterUseRoot.gameObject.SetActive(false);
    }

    private void HandleBoosterUseRequested(BoosterType boosterType)
    {
        BoosterUseRequested?.Invoke(boosterType);
    }

    private void HandleBoosterCancelRequested()
    {
        BoosterCancelRequested?.Invoke();
    }

    #endregion
}
