using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace DefaultNamespace.UI
{
    public class ScrollMapController : MonoBehaviour
    {
        [SerializeField] private RectTransform content;
        [SerializeField] private float backgroundAspectRatio;
        [SerializeField] private GameObject levelButtonPrefab;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform viewport;
        private float buttonHalfHeight;

        [Header("Pooling Settings")]
        [SerializeField] private int defaultPoolCapacity = 10;
        [SerializeField] private int maxPoolSize = 50;

        private IObjectPool<LevelButton> pool;
        private List<LevelMeta> allMetas;
        private List<Vector2> calculatedPositions = new List<Vector2>();
        private Dictionary<int, LevelButton> visibleButtons = new Dictionary<int, LevelButton>();
        private float viewportHeight;

        private void Awake()
        {
            pool = new ObjectPool<LevelButton>(
                CreateButton,
                OnTakeFromPool,
                OnReturnedToPool,
                OnDestroyPoolObject,
                true,
                defaultPoolCapacity,
                maxPoolSize
            );
        }

        private void Start()
        {
            SetContentSize();
            LoadMetas();
            CalculatePositions();
            viewportHeight = viewport.rect.height;
            
            LevelButton temp = pool.Get();
            buttonHalfHeight = temp.GetComponent<RectTransform>().rect.height / 2f;
            pool.Release(temp);
            
            scrollRect.onValueChanged.AddListener(OnScroll);
            OnScroll(scrollRect.normalizedPosition);
        }

        private void SetContentSize()
        {
            float contentHeight = Screen.width * backgroundAspectRatio;
            content.sizeDelta = new Vector2(0, contentHeight);
        }

        private void LoadMetas()
        {
            allMetas = LevelLoader.LoadMetas();
        }

        private void CalculatePositions()
        {
            calculatedPositions.Clear();
            foreach (LevelMeta meta in allMetas)
            {
                float x = (meta.normalizedX * content.rect.width);
                float y = (meta.normalizedY * content.rect.height);
        
                calculatedPositions.Add(new Vector2(x, y));
            }
        }

        private void OnScroll(Vector2 scrollPos)
        {
            float scrolledY = scrollPos.y * (content.rect.height - viewportHeight);
            HashSet<int> shouldBeVisible = new HashSet<int>();

            for (int i = 0; i < allMetas.Count; i++)
            {
                float buttonY = calculatedPositions[i].y;
                bool isVisible = (buttonY + buttonHalfHeight) >= scrolledY &&
                                 (buttonY - buttonHalfHeight) <= scrolledY + viewportHeight;
                         
                if (isVisible) shouldBeVisible.Add(i);
            }

            foreach (int i in visibleButtons.Keys.ToList())
            {
                if (!shouldBeVisible.Contains(i))
                {
                    pool.Release(visibleButtons[i]);
                    visibleButtons.Remove(i);
                }
            }

            foreach (int i in shouldBeVisible)
            {
                if (!visibleButtons.ContainsKey(i))
                {
                    LevelButton button = pool.Get();
                    button.GetComponent<RectTransform>().localPosition = calculatedPositions[i];
                    button.Init(allMetas[i]);
                    visibleButtons[i] = button;
                }
            }
        }
        private LevelButton CreateButton()
        {
            GameObject buttonGo = Instantiate(levelButtonPrefab);
            buttonGo.SetActive(false);
            buttonGo.transform.SetParent(content, false);
            return buttonGo.GetComponent<LevelButton>();
        }

        private void OnTakeFromPool(LevelButton button)
        {
            button.gameObject.SetActive(true);
        }

        private void OnReturnedToPool(LevelButton button)
        {
            button.gameObject.SetActive(false);
        }

        private void OnDestroyPoolObject(LevelButton button)
        {
            Destroy(button.gameObject);
        }
    }
}