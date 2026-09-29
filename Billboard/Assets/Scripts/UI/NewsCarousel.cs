using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace BillboardTool.UI
{
    /// <summary>
    /// Interactive News Carousel / Slideshow widget inspired by Fortnite's "THE BIG BANG" news banner.
    /// Supports remote live CMS updates over HTTP, image downloads, dynamic pagination dots,
    /// mouse wheel scrolling, auto-sliding, smooth crossfades, and click callbacks (with action URLs).
    /// </summary>
    public class NewsCarousel : MonoBehaviour, IScrollHandler
    {
        [System.Serializable]
        public class NewsSlide
        {
            public string title;
            public string subtitle;
            public string badge;
            public Color bgGradientStart;
            public Color bgGradientEnd;
            public Sprite customImage;
            public string imageUrl;
            public string actionUrl;

            public NewsSlide(string title, string subtitle, string badge, Color start, Color end)
            {
                this.title = title;
                this.subtitle = subtitle;
                this.badge = badge;
                this.bgGradientStart = start;
                this.bgGradientEnd = end;
            }
        }

        [System.Serializable]
        private class RemoteSlideData
        {
            public string id;
            public string title;
            public string subtitle;
            public string badge;
            public string bgGradientStart;
            public string bgGradientEnd;
            public string imageUrl;
            public string actionUrl;
        }

        [System.Serializable]
        private class RemoteCarouselResponse
        {
            public int version;
            public string updatedAt;
            public List<RemoteSlideData> slides;
        }

        public enum CardSizePreset
        {
            Custom,
            Compact_440x248,
            Standard_560x315,
            Large_640x360,
            Cinema_720x405
        }

        public enum CardAnchorPreset
        {
            BottomLeft,
            BottomRight,
            BottomCenter,
            TopLeft,
            TopRight,
            Center
        }

        [Header("Layout & Sizing Controls")]
        [Tooltip("Quick presets for standard 16:9 carousel dimensions.")]
        [SerializeField] private CardSizePreset sizePreset = CardSizePreset.Standard_560x315;
        [Tooltip("Custom size in pixels for the carousel card.")]
        [SerializeField] private Vector2 cardSize = new Vector2(560f, 315f);
        [Tooltip("If true, changing width in custom mode automatically locks the card to a 16:9 aspect ratio.")]
        [SerializeField] private bool lock16x9AspectRatio = true;
        [Tooltip("Screen anchor location on the Canvas.")]
        [SerializeField] private CardAnchorPreset anchorPosition = CardAnchorPreset.BottomLeft;
        [Tooltip("Margin/Padding distance from screen edges.")]
        [SerializeField] private Vector2 screenMargin = new Vector2(60f, 60f);

        [Header("Visual Styling & Controls")]
        [Range(0f, 1f)]
        [Tooltip("Strength/opacity of the bottom dark text vignette overlay.")]
        [SerializeField] private float vignetteStrength = 1f;
        [Tooltip("Toggle visibility of left/right arrow buttons.")]
        [SerializeField] private bool showNavigationArrows = true;
        [Tooltip("Diameter of pagination dots in pixels.")]
        [SerializeField] private float dotSize = 10f;
        [Tooltip("Spacing between pagination dots.")]
        [SerializeField] private float dotSpacing = 8f;

        [Header("Typography Scaling")]
        [Tooltip("If true, fonts automatically scale proportionally when you resize the carousel card.")]
        [SerializeField] private bool autoScaleFonts = true;
        [SerializeField] private int titleFontSize = 30;
        [SerializeField] private int subtitleFontSize = 12;
        [SerializeField] private int badgeFontSize = 11;

        [Header("Animation & Transitions")]
        [Tooltip("Duration in seconds for the smooth crossfade slide transition.")]
        [SerializeField] private float crossfadeDuration = 0.22f;

        [Header("Mouse Wheel Scroll")]
        [Tooltip("Enable navigating slides with mouse wheel scroll.")]
        [SerializeField] private bool enableMouseScroll = true;
        [Tooltip("Cooldown in seconds between mouse wheel scroll slide triggers.")]
        [SerializeField] private float scrollCooldown = 0.22f;
        [Tooltip("Invert mouse scroll direction.")]
        [SerializeField] private bool invertMouseScroll = false;

        [Header("Remote CMS Configuration")]
        [Tooltip("If true, automatically fetches slides from the web server when initialized.")]
        [SerializeField] private bool fetchRemoteOnStart = true;
        [Tooltip("Full URL to the JSON carousel endpoint (e.g. http://localhost:3000/api/carousel).")]
        [SerializeField] private string remoteApiUrl = "http://localhost:3000/api/carousel";
        [Tooltip("Interval in seconds to re-fetch slides in the background. Set to 0 to fetch only once on startup.")]
        [SerializeField] private float autoRefreshInterval = 0f;

        [Header("Slides Configuration")]
        [SerializeField] private List<NewsSlide> slides = new List<NewsSlide>();
        [SerializeField] private float autoAdvanceInterval = 4.5f;
        [SerializeField] private bool autoAdvance = true;

        [Header("UI References")]
        [SerializeField] private Image slideBackgroundImage;
        [SerializeField] private Image vignetteOverlayImage;
        [SerializeField] private Text titleText;
        [SerializeField] private Text subtitleText;
        [SerializeField] private Text badgeText;
        [SerializeField] private Transform dotsContainer;
        [SerializeField] private GameObject prevButton;
        [SerializeField] private GameObject nextButton;
        [SerializeField] private List<Image> paginationDots = new List<Image>();
        [SerializeField] private CanvasGroup contentCanvasGroup;

        [Header("Dot Colors")]
        [SerializeField] private Color activeDotColor = Color.white;
        [SerializeField] private Color inactiveDotColor = new Color(1f, 1f, 1f, 0.35f);

        public event Action<NewsSlide> OnSlideClicked;

        private int currentIndex = 0;
        private Coroutine autoSlideCoroutine;
        private Coroutine transitionCoroutine;
        private Coroutine fetchCoroutine;
        private Coroutine autoRefreshCoroutine;
        private List<Sprite> generatedSprites = new List<Sprite>();
        private static readonly Dictionary<string, Texture2D> textureCache = new Dictionary<string, Texture2D>();

        public int CurrentIndex => currentIndex;
        public int SlideCount => slides != null ? slides.Count : 0;
        public string RemoteApiUrl { get => remoteApiUrl; set => remoteApiUrl = value; }

        public void ApplyLayoutSettings()
        {
            RectTransform rt = GetComponent<RectTransform>();
            if (rt == null) return;

            // 1. Calculate target size
            Vector2 targetSize = cardSize;
            switch (sizePreset)
            {
                case CardSizePreset.Compact_440x248:
                    targetSize = new Vector2(440f, 247.5f);
                    break;
                case CardSizePreset.Standard_560x315:
                    targetSize = new Vector2(560f, 315f);
                    break;
                case CardSizePreset.Large_640x360:
                    targetSize = new Vector2(640f, 360f);
                    break;
                case CardSizePreset.Cinema_720x405:
                    targetSize = new Vector2(720f, 405f);
                    break;
                case CardSizePreset.Custom:
                    if (lock16x9AspectRatio && targetSize.x > 0)
                    {
                        targetSize.y = Mathf.Round(targetSize.x * 9f / 16f);
                    }
                    break;
            }
            cardSize = targetSize;
            rt.sizeDelta = targetSize;

            // 2. Apply Anchors and Pivot
            switch (anchorPosition)
            {
                case CardAnchorPreset.BottomLeft:
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.zero;
                    rt.pivot = Vector2.zero;
                    rt.anchoredPosition = new Vector2(screenMargin.x, screenMargin.y);
                    break;
                case CardAnchorPreset.BottomRight:
                    rt.anchorMin = new Vector2(1f, 0f);
                    rt.anchorMax = new Vector2(1f, 0f);
                    rt.pivot = new Vector2(1f, 0f);
                    rt.anchoredPosition = new Vector2(-screenMargin.x, screenMargin.y);
                    break;
                case CardAnchorPreset.BottomCenter:
                    rt.anchorMin = new Vector2(0.5f, 0f);
                    rt.anchorMax = new Vector2(0.5f, 0f);
                    rt.pivot = new Vector2(0.5f, 0f);
                    rt.anchoredPosition = new Vector2(0f, screenMargin.y);
                    break;
                case CardAnchorPreset.TopLeft:
                    rt.anchorMin = new Vector2(0f, 1f);
                    rt.anchorMax = new Vector2(0f, 1f);
                    rt.pivot = new Vector2(0f, 1f);
                    rt.anchoredPosition = new Vector2(screenMargin.x, -screenMargin.y);
                    break;
                case CardAnchorPreset.TopRight:
                    rt.anchorMin = Vector2.one;
                    rt.anchorMax = Vector2.one;
                    rt.pivot = Vector2.one;
                    rt.anchoredPosition = new Vector2(-screenMargin.x, -screenMargin.y);
                    break;
                case CardAnchorPreset.Center:
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = screenMargin;
                    break;
            }

            // 3. Vignette strength
            if (vignetteOverlayImage != null)
            {
                Color c = vignetteOverlayImage.color;
                c.a = vignetteStrength;
                vignetteOverlayImage.color = c;
            }

            // 4. Arrow buttons visibility
            if (prevButton != null) prevButton.SetActive(showNavigationArrows);
            if (nextButton != null) nextButton.SetActive(showNavigationArrows);

            // 5. Typography scaling
            float scaleFactor = targetSize.x / 560f;
            if (autoScaleFonts)
            {
                if (titleText != null) titleText.fontSize = Mathf.Clamp(Mathf.RoundToInt(30f * scaleFactor), 18, 56);
                if (subtitleText != null) subtitleText.fontSize = Mathf.Clamp(Mathf.RoundToInt(12f * scaleFactor), 9, 24);
                if (badgeText != null) badgeText.fontSize = Mathf.Clamp(Mathf.RoundToInt(11f * scaleFactor), 8, 20);
            }
            else
            {
                if (titleText != null) titleText.fontSize = titleFontSize;
                if (subtitleText != null) subtitleText.fontSize = subtitleFontSize;
                if (badgeText != null) badgeText.fontSize = badgeFontSize;
            }

            // 6. Dots layout
            if (dotsContainer != null)
            {
                HorizontalLayoutGroup hlg = dotsContainer.GetComponent<HorizontalLayoutGroup>();
                if (hlg != null) hlg.spacing = dotSpacing;
                for (int i = 0; i < dotsContainer.childCount; i++)
                {
                    RectTransform dotRt = dotsContainer.GetChild(i) as RectTransform;
                    if (dotRt != null) dotRt.sizeDelta = new Vector2(dotSize, dotSize);
                }
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null)
                {
                    ApplyLayoutSettings();
                }
            };
        }
#endif

        private void Awake()
        {
            ApplyLayoutSettings();

            if (slides.Count == 0)
            {
                // Default Fortnite / Game style demo fallback slides
                slides.Add(new NewsSlide(
                    "THE BIG BANG",
                    "A NEW BEGINNING • LIVE EVENT",
                    "NEWS",
                    new Color(0.24f, 0.06f, 0.44f), // Deep Cosmic Purple
                    new Color(0.85f, 0.22f, 0.65f)  // Magenta Starburst
                ));

                slides.Add(new NewsSlide(
                    "BILLBOARD SUITE 2.0",
                    "DYNAMIC 2D & 3D CAMERA FACING",
                    "UPDATE",
                    new Color(0.08f, 0.25f, 0.45f), // Midnight Blue
                    new Color(0.15f, 0.75f, 0.85f)  // Cyan Plasma
                ));

                slides.Add(new NewsSlide(
                    "IMPOSTOR BAKERY",
                    "MULTI-ANGLE SPRITE RENDERING",
                    "FEATURE",
                    new Color(0.40f, 0.15f, 0.05f), // Ember Deep
                    new Color(0.95f, 0.55f, 0.12f)  // Solar Flare
                ));

                slides.Add(new NewsSlide(
                    "COMMUNITY SHOWCASE",
                    "CREATIVE MODES & SANDBOX",
                    "FEATURED",
                    new Color(0.08f, 0.35f, 0.22f), // Forest Emerald
                    new Color(0.25f, 0.85f, 0.55f)  // Mint Glow
                ));
            }

            GenerateProceduralGradients();
            RebuildDotsUI();
        }

        private void Start()
        {
            UpdateSlideDisplay(0, immediate: true);

            if (autoAdvance)
            {
                StartAutoSlide();
            }

            if (fetchRemoteOnStart && !string.IsNullOrEmpty(remoteApiUrl))
            {
                FetchRemoteSlides();
            }

            if (autoRefreshInterval > 0f)
            {
                autoRefreshCoroutine = StartCoroutine(AutoRefreshRoutine());
            }
        }

        private float lastScrollTime = 0f;

        private void Update()
        {
            if (enableMouseScroll)
            {
                CheckHoverScrollInput();
            }
        }

        private void CheckHoverScrollInput()
        {
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) < 0.1f) return;
            if (Time.unscaledTime - lastScrollTime < scrollCooldown) return;

            RectTransform rt = GetComponent<RectTransform>();
            if (rt != null && RectTransformUtility.RectangleContainsScreenPoint(rt, Input.mousePosition, null))
            {
                TriggerScrollSlide(scroll);
            }
        }

        public void OnScroll(PointerEventData eventData)
        {
            if (!enableMouseScroll) return;
            if (Time.unscaledTime - lastScrollTime < scrollCooldown) return;

            float scroll = eventData.scrollDelta.y;
            if (Mathf.Abs(scroll) < 0.05f) return;

            TriggerScrollSlide(scroll);
        }

        private void TriggerScrollSlide(float deltaY)
        {
            if (invertMouseScroll) deltaY = -deltaY;

            if (deltaY < 0f)
            {
                NextSlide();
                lastScrollTime = Time.unscaledTime;
            }
            else if (deltaY > 0f)
            {
                PreviousSlide();
                lastScrollTime = Time.unscaledTime;
            }
        }

        private void OnDisable()
        {
            StopAutoSlide();
            if (autoRefreshCoroutine != null)
            {
                StopCoroutine(autoRefreshCoroutine);
                autoRefreshCoroutine = null;
            }
            if (fetchCoroutine != null)
            {
                StopCoroutine(fetchCoroutine);
                fetchCoroutine = null;
            }
        }

        [ContextMenu("Fetch Remote Slides Now")]
        public void FetchRemoteSlides()
        {
            if (fetchCoroutine != null) StopCoroutine(fetchCoroutine);
            fetchCoroutine = StartCoroutine(FetchRemoteSlidesRoutine());
        }

        private IEnumerator AutoRefreshRoutine()
        {
            while (autoRefreshInterval > 0f)
            {
                yield return new WaitForSecondsRealtime(autoRefreshInterval);
                yield return FetchRemoteSlidesRoutine();
            }
        }

        private IEnumerator FetchRemoteSlidesRoutine()
        {
            if (string.IsNullOrEmpty(remoteApiUrl)) yield break;

            Debug.Log($"[NewsCarousel] Connecting to CMS endpoint: {remoteApiUrl}");
            using (UnityWebRequest req = UnityWebRequest.Get(remoteApiUrl))
            {
                req.timeout = 8;
                yield return req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"[NewsCarousel] Remote fetch notice: {req.error}. Using active local slides.");
                    yield break;
                }

                string json = req.downloadHandler.text;
                RemoteCarouselResponse response = null;
                try
                {
                    response = JsonUtility.FromJson<RemoteCarouselResponse>(json);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[NewsCarousel] Failed to parse JSON response: {ex.Message}");
                    yield break;
                }

                if (response == null || response.slides == null || response.slides.Count == 0)
                {
                    Debug.LogWarning("[NewsCarousel] Received empty or invalid slide list from server.");
                    yield break;
                }

                List<NewsSlide> newSlides = new List<NewsSlide>();
                List<Sprite> newSprites = new List<Sprite>();

                for (int i = 0; i < response.slides.Count; i++)
                {
                    RemoteSlideData data = response.slides[i];
                    Color startCol = ParseHexColor(data.bgGradientStart, new Color(0.20f, 0.10f, 0.40f));
                    Color endCol = ParseHexColor(data.bgGradientEnd, new Color(0.80f, 0.20f, 0.60f));

                    NewsSlide slide = new NewsSlide(data.title, data.subtitle, data.badge, startCol, endCol)
                    {
                        imageUrl = data.imageUrl,
                        actionUrl = data.actionUrl
                    };

                    Sprite slideSprite = null;

                    // Download image if imageUrl is provided
                    if (!string.IsNullOrEmpty(data.imageUrl))
                    {
                        if (textureCache.TryGetValue(data.imageUrl, out Texture2D cachedTex) && cachedTex != null)
                        {
                            slideSprite = Sprite.Create(cachedTex, new Rect(0, 0, cachedTex.width, cachedTex.height), new Vector2(0.5f, 0.5f));
                        }
                        else
                        {
                            using (UnityWebRequest imgReq = UnityWebRequestTexture.GetTexture(data.imageUrl))
                            {
                                imgReq.timeout = 10;
                                yield return imgReq.SendWebRequest();
                                if (imgReq.result == UnityWebRequest.Result.Success)
                                {
                                    Texture2D tex = DownloadHandlerTexture.GetContent(imgReq);
                                    textureCache[data.imageUrl] = tex;
                                    slideSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                                }
                                else
                                {
                                    Debug.LogWarning($"[NewsCarousel] Failed to load image {data.imageUrl}: {imgReq.error}");
                                }
                            }
                        }
                    }

                    // Procedural gradient fallback if no custom image
                    if (slideSprite == null)
                    {
                        Texture2D gradTex = CreateDiagonalGradientTexture(128, 72, startCol, endCol);
                        slideSprite = Sprite.Create(gradTex, new Rect(0, 0, gradTex.width, gradTex.height), new Vector2(0.5f, 0.5f));
                    }

                    slide.customImage = slideSprite;
                    newSlides.Add(slide);
                    newSprites.Add(slideSprite);
                }

                // Apply downloaded slides
                slides = newSlides;
                generatedSprites = newSprites;

                RebuildDotsUI();

                currentIndex = Mathf.Clamp(currentIndex, 0, slides.Count - 1);
                UpdateSlideDisplay(currentIndex, immediate: false);

                Debug.Log($"[NewsCarousel] Successfully updated {slides.Count} carousel slides from web server!");
            }
        }

        private Color ParseHexColor(string hex, Color defaultColor)
        {
            if (string.IsNullOrEmpty(hex)) return defaultColor;
            if (!hex.StartsWith("#")) hex = "#" + hex;
            if (ColorUtility.TryParseHtmlString(hex, out Color parsed))
            {
                return parsed;
            }
            return defaultColor;
        }

        private static Sprite sharedCircleSprite;

        private static Sprite GetOrCreateCircleSprite()
        {
            if (sharedCircleSprite != null) return sharedCircleSprite;

            int size = 32;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            float radius = size * 0.5f;
            Vector2 center = new Vector2(radius, radius);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    float alpha = Mathf.Clamp01(radius - dist);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            sharedCircleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            return sharedCircleSprite;
        }

        public void RebuildDotsUI()
        {
            if (dotsContainer != null)
            {
                Sprite circleSp = GetOrCreateCircleSprite();
                int currentDotCount = dotsContainer.childCount;
                int targetDotCount = slides.Count;

                paginationDots.Clear();

                // Destroy excess dots
                for (int i = targetDotCount; i < currentDotCount; i++)
                {
                    Transform child = dotsContainer.GetChild(i);
                    Destroy(child.gameObject);
                }

                // Create or reuse dots
                for (int i = 0; i < targetDotCount; i++)
                {
                    GameObject dotObj;
                    if (i < currentDotCount)
                    {
                        dotObj = dotsContainer.GetChild(i).gameObject;
                    }
                    else
                    {
                        dotObj = new GameObject($"Dot_{i}", typeof(RectTransform), typeof(Image), typeof(Button));
                        dotObj.transform.SetParent(dotsContainer, false);
                        RectTransform rt = dotObj.GetComponent<RectTransform>();
                        rt.sizeDelta = new Vector2(10f, 10f);
                    }

                    Image img = dotObj.GetComponent<Image>();
                    img.sprite = circleSp;
                    img.type = Image.Type.Simple;

                    Button btn = dotObj.GetComponent<Button>();

                    paginationDots.Add(img);

                    int targetIndex = i;
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => GoToSlide(targetIndex));
                }
            }
            else
            {
                // Ensure pre-existing dots have circle sprite
                Sprite circleSp = GetOrCreateCircleSprite();
                for (int i = 0; i < paginationDots.Count; i++)
                {
                    if (paginationDots[i] != null && paginationDots[i].sprite == null)
                    {
                        paginationDots[i].sprite = circleSp;
                    }
                }
            }

            UpdateDots(currentIndex);
        }

        private void GenerateProceduralGradients()
        {
            generatedSprites.Clear();
            for (int i = 0; i < slides.Count; i++)
            {
                if (slides[i].customImage != null)
                {
                    generatedSprites.Add(slides[i].customImage);
                }
                else
                {
                    Texture2D tex = CreateDiagonalGradientTexture(256, 144, slides[i].bgGradientStart, slides[i].bgGradientEnd);
                    Sprite sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                    generatedSprites.Add(sp);
                }
            }
        }

        private Texture2D CreateDiagonalGradientTexture(int width, int height, Color c1, Color c2)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            for (int y = 0; y < height; y++)
            {
                float v = (float)y / (height - 1); // 0 at bottom, 1 at top
                for (int x = 0; x < width; x++)
                {
                    float u = (float)x / (width - 1); // 0 at left, 1 at right
                    // 135 degrees: top-left (u=0, v=1) -> bottom-right (u=1, v=0)
                    float t = Mathf.Clamp01((u + (1f - v)) * 0.5f);
                    Color color = Color.Lerp(c1, c2, t);
                    tex.SetPixel(x, y, color);
                }
            }
            tex.Apply();
            return tex;
        }

        public void NextSlide()
        {
            if (slides.Count == 0) return;
            int next = (currentIndex + 1) % slides.Count;
            GoToSlide(next);
        }

        public void PreviousSlide()
        {
            if (slides.Count == 0) return;
            int prev = (currentIndex - 1 + slides.Count) % slides.Count;
            GoToSlide(prev);
        }

        public void GoToSlide(int index)
        {
            if (index < 0 || index >= slides.Count || index == currentIndex) return;

            RestartAutoSlide();
            UpdateSlideDisplay(index, immediate: false);
        }

        public void HandleSlideClicked()
        {
            if (currentIndex >= 0 && currentIndex < slides.Count)
            {
                NewsSlide activeSlide = slides[currentIndex];
                Debug.Log($"[NewsCarousel] Slide clicked: {activeSlide.title}");
                OnSlideClicked?.Invoke(activeSlide);

                // Open external URL in default browser if provided
                if (!string.IsNullOrEmpty(activeSlide.actionUrl))
                {
                    Application.OpenURL(activeSlide.actionUrl);
                }
            }
        }

        private void UpdateSlideDisplay(int targetIndex, bool immediate)
        {
            if (slides.Count == 0) return;

            currentIndex = Mathf.Clamp(targetIndex, 0, slides.Count - 1);
            UpdateDots(currentIndex);

            if (immediate || contentCanvasGroup == null)
            {
                ApplySlideData(slides[currentIndex], currentIndex);
                if (contentCanvasGroup != null) contentCanvasGroup.alpha = 1f;
            }
            else
            {
                if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
                transitionCoroutine = StartCoroutine(CrossfadeRoutine(currentIndex));
            }
        }

        private IEnumerator CrossfadeRoutine(int targetIndex)
        {
            float duration = Mathf.Max(0.05f, crossfadeDuration);

            // Fade out
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                contentCanvasGroup.alpha = 1f - (t / duration);
                yield return null;
            }
            contentCanvasGroup.alpha = 0f;

            // Swap content
            ApplySlideData(slides[targetIndex], targetIndex);

            // Fade in
            for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
            {
                contentCanvasGroup.alpha = t / duration;
                yield return null;
            }
            contentCanvasGroup.alpha = 1f;
            transitionCoroutine = null;
        }

        private void ApplySlideData(NewsSlide slide, int index)
        {
            if (titleText != null) titleText.text = slide.title;
            if (subtitleText != null) subtitleText.text = slide.subtitle;
            if (badgeText != null) badgeText.text = slide.badge;

            if (slideBackgroundImage != null)
            {
                if (index < generatedSprites.Count && generatedSprites[index] != null)
                {
                    slideBackgroundImage.sprite = generatedSprites[index];
                }
                else if (slide.customImage != null)
                {
                    slideBackgroundImage.sprite = slide.customImage;
                }
                slideBackgroundImage.color = Color.white;
            }
        }

        private void UpdateDots(int activeIndex)
        {
            for (int i = 0; i < paginationDots.Count; i++)
            {
                if (paginationDots[i] != null)
                {
                    bool isActive = (i == activeIndex);
                    paginationDots[i].color = isActive ? activeDotColor : inactiveDotColor;
                    paginationDots[i].transform.localScale = isActive ? new Vector3(1.25f, 1.25f, 1f) : Vector3.one;
                }
            }
        }

        private void StartAutoSlide()
        {
            StopAutoSlide();
            autoSlideCoroutine = StartCoroutine(AutoSlideTimerRoutine());
        }

        private void StopAutoSlide()
        {
            if (autoSlideCoroutine != null)
            {
                StopCoroutine(autoSlideCoroutine);
                autoSlideCoroutine = null;
            }
        }

        private void RestartAutoSlide()
        {
            if (autoAdvance)
            {
                StartAutoSlide();
            }
        }

        private IEnumerator AutoSlideTimerRoutine()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(autoAdvanceInterval);
                if (slides.Count > 1)
                {
                    int next = (currentIndex + 1) % slides.Count;
                    UpdateSlideDisplay(next, immediate: false);
                }
            }
        }
    }
}
