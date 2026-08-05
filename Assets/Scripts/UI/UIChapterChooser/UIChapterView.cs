using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace DefaultNamespace.UI
{
    public sealed class UIChapterView : MonoBehaviour
    {
        [SerializeField] private Image avatarImage;
        [SerializeField] private Sprite placeholderSprite;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private Button visitButton;
        [SerializeField] private TMP_Text visitButtonLabel;
        [SerializeField] private RectTransform currentChapterIndicator;

        private AsyncOperationHandle<Sprite> chooserImageHandle;
        private int chapterId;
        private int initVersion;

        public event Action<int> VisitRequested;

        private void Awake()
        {
            visitButton.onClick.AddListener(HandleVisitClicked);
        }

        public async UniTask Init(ChapterChooserItemState state, CancellationToken cancellationToken)
        {
            ResetForPool();
            int requestedVersion = initVersion;
            ChapterIndexEntry chapter = state.Chapter;
            chapterId = chapter.chapterId;
            titleText.text = chapter.displayName;
            descriptionText.text = chapter.description;
            visitButton.gameObject.SetActive(!state.IsCurrent);
            visitButton.interactable = state.IsUnlocked;
            visitButtonLabel.text = state.IsUnlocked ? "Visit" : $"Reach Level {chapter.unlockLevelId}";
            currentChapterIndicator.gameObject.SetActive(state.IsCurrent);

            AsyncOperationHandle<Sprite> loadHandle = Addressables.LoadAssetAsync<Sprite>(chapter.chooserImageAddress);
            try
            {
                Sprite chooserImage = await loadHandle.ToUniTask(cancellationToken: cancellationToken);
                if (requestedVersion != initVersion) return;
                chooserImageHandle = loadHandle;
                loadHandle = default;
                avatarImage.sprite = chooserImage;
            }
            finally
            {
                if (loadHandle.IsValid()) Addressables.Release(loadHandle);
            }
        }

        public void ResetForPool()
        {
            initVersion++;
            if (chooserImageHandle.IsValid()) Addressables.Release(chooserImageHandle);
            chooserImageHandle = default;
            chapterId = 0;
            avatarImage.sprite = placeholderSprite;
            titleText.text = string.Empty;
            descriptionText.text = string.Empty;
            visitButton.gameObject.SetActive(false);
            visitButton.interactable = false;
            visitButtonLabel.text = string.Empty;
            currentChapterIndicator.gameObject.SetActive(false);
        }

        private void HandleVisitClicked()
        {
            VisitRequested?.Invoke(chapterId);
        }

        private void OnDestroy()
        {
            visitButton.onClick.RemoveListener(HandleVisitClicked);
            ResetForPool();
        }
    }
}
