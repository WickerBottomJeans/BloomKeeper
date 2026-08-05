using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace DefaultNamespace.UI
{
    public sealed class UIChapterChooser : MonoBehaviour
    {
        [SerializeField] private HorizontalSnapPool snapPool;
        [SerializeField] private RectTransform scrollRectTransform;
        [SerializeField] private UIChapterView chapterViewTemplate;
        [SerializeField] private Button closeButton;
        [SerializeField] private CanvasGroup visibilityGroup;
        [SerializeField] private UIPopupEntranceAnimator entranceAnimator;
        [SerializeField, Min(0f)] private float edgeGap;
        [SerializeField] private int defaultPoolCapacity = 3;
        [SerializeField] private int maxPoolSize = 5;
        [SerializeField] private int preloadItemCount = 1;

        private IReadOnlyList<ChapterChooserItemState> chapterStates;
        private CancellationTokenSource lifecycleCancellation;
        private List<UniTask> preparationTasks;
        private Vector3 preparedViewScale;
        private float centerStep;
        private int? preparedCurrentChapterIndex;
        private bool isPreparing;
        private bool isPrepared;
        private bool isHiddenForPreparation;

        public event Action<int> ChapterVisitRequested;
        public event Action CloseRequested;

        private void Awake()
        {
            chapterViewTemplate.gameObject.SetActive(false);
            closeButton.onClick.AddListener(HandleCloseClicked);
        }

        public async UniTask PrepareAsync(IReadOnlyList<ChapterChooserItemState> chapterStates)
        {
            if (!isHiddenForPreparation) throw new InvalidOperationException("Chapter chooser must be hidden for preparation before preparation begins.");
            if (isPreparing) throw new InvalidOperationException("Chapter chooser preparation is already running.");
            if (chapterStates == null) throw new ArgumentNullException(nameof(chapterStates));

            isPreparing = true;

            try
            {
                ClearPoolAndPreparedState();
                this.chapterStates = chapterStates;

                int? currentChapterIndex = null;
                for (int index = 0; index < chapterStates.Count; index++)
                {
                    if (!chapterStates[index].IsCurrent) continue;
                    if (currentChapterIndex.HasValue) throw new InvalidOperationException("Chapter chooser requires exactly one current chapter.");
                    currentChapterIndex = index;
                }

                if (!currentChapterIndex.HasValue) throw new InvalidOperationException("Chapter chooser requires exactly one current chapter.");
                preparedCurrentChapterIndex = currentChapterIndex;

                lifecycleCancellation = new CancellationTokenSource();
                CancellationToken preparationCancellationToken = lifecycleCancellation.Token;
                preparationTasks = new List<UniTask>();
                PrepareLayout();
                snapPool.Configure(chapterViewTemplate, chapterStates.Count, centerStep, PrepareView, ShowView, HideView, currentChapterIndex.Value, defaultPoolCapacity, maxPoolSize, preloadItemCount);
                List<UniTask> initialViewTasks = preparationTasks;
                preparationTasks = null;
                await UniTask.WhenAll(initialViewTasks);
                preparationCancellationToken.ThrowIfCancellationRequested();
                isPrepared = true;
            }
            catch
            {
                ClearPoolAndPreparedState();
                throw;
            }
            finally
            {
                preparationTasks = null;
                isPreparing = false;
            }
        }

        public void Show()
        {
            if (!isPrepared) throw new InvalidOperationException("Chapter chooser must finish preparation before it can be shown.");
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)scrollRectTransform.parent);
            scrollRectTransform.ForceUpdateRectTransforms();
            snapPool.RefreshViewport();
            snapPool.JumpToIndex(preparedCurrentChapterIndex.Value);
            isHiddenForPreparation = false;
            SetVisibility(true);
            entranceAnimator.enabled = true;
        }

        public void Hide()
        {
            isHiddenForPreparation = false;
            entranceAnimator.enabled = false;
            SetVisibility(false);
            ClearPoolAndPreparedState();
            gameObject.SetActive(false);
        }

        public void HideForPreparation()
        {
            ClearPoolAndPreparedState();
            isHiddenForPreparation = true;
            entranceAnimator.enabled = false;
            SetVisibility(false);
            gameObject.SetActive(true);
        }

        private void PrepareView(UIChapterView view)
        {
            view.transform.localScale = preparedViewScale;
            view.VisitRequested += HandleChapterVisitRequested;
        }

        private void PrepareLayout()
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)scrollRectTransform.parent);
            scrollRectTransform.ForceUpdateRectTransforms();
            RectTransform templateTransform = (RectTransform)chapterViewTemplate.transform;
            Vector3 templateScale = templateTransform.localScale;
            float templateWidth = templateTransform.rect.width * Mathf.Abs(templateScale.x);
            float templateHeight = templateTransform.rect.height * Mathf.Abs(templateScale.y);
            if (scrollRectTransform.rect.width <= 0f || scrollRectTransform.rect.height <= 0f) throw new InvalidOperationException("Chapter chooser scroll rect requires positive dimensions.");
            if (templateWidth <= 0f || templateHeight <= 0f) throw new InvalidOperationException("Chapter chooser view template requires positive dimensions.");

            float fitScale = Mathf.Min(1f, scrollRectTransform.rect.width / templateWidth, scrollRectTransform.rect.height / templateHeight);
            preparedViewScale = templateScale * fitScale;
            float scaledViewWidth = templateWidth * fitScale;
            centerStep = scaledViewWidth + edgeGap;
        }

        private void ShowView(UIChapterView view, int index)
        {
            UniTask initialization = view.Init(chapterStates[index], lifecycleCancellation.Token);
            if (preparationTasks != null)
            {
                preparationTasks.Add(initialization);
                return;
            }

            initialization.Forget();
        }

        private static void HideView(UIChapterView view)
        {
            view.ResetForPool();
        }

        private void ClearPoolAndPreparedState()
        {
            isPrepared = false;
            preparationTasks = null;
            lifecycleCancellation?.Cancel();
            snapPool.ClearPoolAndConfig();
            lifecycleCancellation?.Dispose();
            lifecycleCancellation = null;
            chapterStates = null;
            preparedCurrentChapterIndex = null;
        }

        private void HandleChapterVisitRequested(int chapterId)
        {
            ChapterVisitRequested?.Invoke(chapterId);
        }

        private void HandleCloseClicked()
        {
            CloseRequested?.Invoke();
        }

        private void SetVisibility(bool isVisible)
        {
            visibilityGroup.alpha = isVisible ? 1f : 0f;
            visibilityGroup.interactable = isVisible;
            visibilityGroup.blocksRaycasts = isVisible;
        }

        private void OnDestroy()
        {
            closeButton.onClick.RemoveListener(HandleCloseClicked);
            ClearPoolAndPreparedState();
        }
    }
}
