using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Kuantech.Core.UI
{
    
    public class CarouselSnap : UIElement, IEndDragHandler
    {
        [Header("UI References")]
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform Content;
        [SerializeField] private Button LeftButton;
        [SerializeField] private Button RightButton;


        [SerializeField] private float SnapSpeed = 12f;
        [Tooltip("On rebuild, stretch every page/card to the viewport width so you only author the element's " +
                 "content, not its page size. Requires a Horizontal Layout Group + Content Size Fitter on Content.")]
        [SerializeField] private bool FitPagesToViewport = true;

        /// <summary>The transform pages/cards live under. Fill this, then call <see cref="RebuildCarousel"/>.</summary>
        public RectTransform ContentRoot => Content;

        private int childCount;
        private float[] pagePositions;
        private int currentIndex = 0;
        private bool isSnapping = false;
        private float targetNormalizedPos;

        public override void Initialize()
        {
            if (Initialized) return;
            base.Initialize();
            if (LeftButton != null) LeftButton.onClick.AddListener(() => SnapToIndex(currentIndex - 1));
            if (RightButton != null) RightButton.onClick.AddListener(() => SnapToIndex(currentIndex + 1));
        }

        public void RebuildCarousel(int startIndex = 0)
        {
            // 1. KANIT: Unity UI Layout sistemine boyutları hemen yenilemesini emret.
            // Bu adımı atlarsak Content'in rect genişliği henüz güncellenmediği için hesaplar şaşar.
            Canvas.ForceUpdateCanvases();

            childCount = Content.childCount;
            if (childCount == 0) return;

            // Auto-fit each page to the viewport width so the author only builds the element's content, not its
            // page size. Equal viewport-width pages are also what makes the normalized stops (i/(n-1)) line up.
            if (FitPagesToViewport && scrollRect != null && scrollRect.viewport != null)
            {
                float viewportWidth = scrollRect.viewport.rect.width;
                for (int i = 0; i < childCount; i++)
                {
                    if (!(Content.GetChild(i) is RectTransform child)) continue;
                    LayoutElement layoutElement = child.GetComponent<LayoutElement>();
                    if (layoutElement == null) layoutElement = child.gameObject.AddComponent<LayoutElement>();
                    layoutElement.preferredWidth = viewportWidth;
                }
                Canvas.ForceUpdateCanvases(); // apply the new widths before we read positions / snap
            }

            pagePositions = new float[childCount];

            if (childCount == 1)
            {
                pagePositions[0] = 0f; // Tek kart varsa durak 0'dır
            }
            else
            {
                for (int i = 0; i < childCount; i++)
                {
                    pagePositions[i] = (float)i / (childCount - 1);
                }
            }

            if (LeftButton)
            {
                LeftButton.onClick.RemoveAllListeners();
                LeftButton.onClick.AddListener(SelectPrevious);
            }
            if (RightButton)
            {
                RightButton.onClick.RemoveAllListeners();
                RightButton.onClick.AddListener(SelectNext);
            }

            SnapToIndexInstant(Mathf.Clamp(startIndex, 0, childCount - 1));
        }

        private void SelectPrevious()
        {
            SnapToIndex(currentIndex -1);
        }

        private void SelectNext()
        {
            SnapToIndex(currentIndex+1);
        }

        public void SnapToIndex(int index)
        {
            if (childCount == 0) return;
            currentIndex = Mathf.Clamp(index, 0, childCount - 1);
            targetNormalizedPos = pagePositions[currentIndex];
            isSnapping = true;
            UpdateButtonState();
        }

        private void SnapToIndexInstant(int index)
        {
            if (childCount == 0) return;
            currentIndex = index;
            targetNormalizedPos = pagePositions[currentIndex];
            scrollRect.horizontalNormalizedPosition = targetNormalizedPos;
            isSnapping = false;
            UpdateButtonState();
        }

        private void UpdateButtonState()
        {
            if (LeftButton) LeftButton.interactable = (currentIndex > 0);
            if (RightButton) RightButton.interactable = (currentIndex < childCount - 1);
        }

        private void Update()
        {
            if(isSnapping)
            {
                // unscaled so the snap still animates if this menu opens over a paused game (timeScale 0).
                scrollRect.horizontalNormalizedPosition = Mathf.Lerp(
                    scrollRect.horizontalNormalizedPosition, targetNormalizedPos, Time.unscaledDeltaTime * SnapSpeed
                );

                if (Mathf.Abs(scrollRect.horizontalNormalizedPosition - targetNormalizedPos) < 0.001f)
                {
                    scrollRect.horizontalNormalizedPosition = targetNormalizedPos;
                    isSnapping = false;
                }
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            float currentPos = scrollRect.horizontalNormalizedPosition;
            int nearestIndex = 0;
            float minDistance = float.MaxValue;

            // En yakın kartı bul
            for (int i = 0; i < childCount; i++)
            {
                float distance = Mathf.Abs(currentPos - pagePositions[i]);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestIndex = i;
                }
            }

            SnapToIndex(nearestIndex);
        }

        public int GetCurrentElementIndex() => currentIndex;
    }
}