using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace BillboardTool.UI
{
    /// <summary>
    /// Interactive Zero Billboard widget inspired by Fortnite's "THE BIG BANG" news banner.
    /// Supports remote live CMS updates over HTTP, image downloads, dynamic pagination dots,
    /// curved corners, borders, drop shadow, mouse wheel scrolling, auto-sliding, smooth crossfades,
    /// and click callbacks (with action URLs).
    /// </summary>
    public class ZeroBillboard : MonoBehaviour, IScrollHandler
    {
        [System.Serializable]
        public class BillboardSlide
        {
            public string title;
            public string subtitle;
            public string badge;
            public Color bgGradientStart;
            public Color bgGradientEnd;
            public Sprite customImage;
            public string imageUrl;
            public string actionUrl;

            public BillboardSlide(string title, string subtitle, string badge, Color start, Color end)
            {
                this.title = title;
                this.subtitle = subtitle;
                this.badge = badge;
                this.bgGradientStart = start;
                this.bgGradientEnd = end;
            }

            public BillboardSlide() { }
        }

        // Backward compatibility type alias
        [System.Serializable]
        public class NewsSlide : BillboardSlide
        {
            public NewsSlide(string title, string subtitle, string badge, Color start, Color end) : base(title, subtitle, badge, start, end) { }
            public NewsSlide() : base() { }
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
        private class RemoteBillboardResponse
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
        [Tooltip("Quick presets for standard 16:9 dimensions.")]
        [SerializeField] private CardSizePreset sizePreset = CardSizePreset.Standard_560x315;
        [Tooltip("Custom size in pixels for the card.")]
        [SerializeField] private Vector2 cardSize = new Vector2(560f, 315f);
        [Tooltip("If true, changing width in custom mode automatically locks the card to a 16:9 aspect ratio.")]
        [SerializeField] private bool lock16x9AspectRatio = true;
        [Tooltip("Screen anchor location on the Canvas.")]
        [SerializeField] private CardAnchorPreset anchorPosition = CardAnchorPreset.BottomLeft;
        [Tooltip("Margin/Padding distance from screen edges.")]
        [SerializeField] private Vector2 screenMargin = new Vector2(60f, 60f);

        [Header("Curved Corners & Frame Styling")]
        [Range(0f, 32f)]
        [Tooltip("Curved corner radius in pixels for the card.")]
        [SerializeField] private float cornerRadius = 14f;

        [Tooltip("Toggle visible card outer border.")]
        [SerializeField] private bool showBorder = true;
        [Range(0f, 8f)]
        [Tooltip("Thickness in pixels of the outer card border frame.")]
        [SerializeField] private float borderWidth = 2f;
        [Tooltip("Color of the outer border frame.")]
        [SerializeField] private Color borderColor = new Color(0.25f, 0.35f, 0.48f, 0.70f);

        [Tooltip("Show a soft drop shadow behind the card.")]
        [SerializeField] private bool showCardShadow = true;
        [SerializeField] private Color cardShadowColor = new Color(0f, 0f, 0f, 0.65f);
        [SerializeField] private Vector2 cardShadowOffset = new Vector2(3f, -4f);

        [Header("Visual Styling & Overlays")]
        [Range(0f, 1f)]
        [Tooltip("Strength/opacity of the bottom dark text vignette overlay.")]
        [SerializeField] private float vignetteStrength = 1f;
        [SerializeField] private Color vignetteColor = new Color(0.04f, 0.06f, 0.10f, 1f);

        [Tooltip("Toggle visibility of left/right arrow buttons.")]
        [SerializeField] private bool showNavigationArrows = true;
        [Range(0f, 16f)]
        [SerializeField] private float arrowCornerRadius = 6f;
        [SerializeField] private Color arrowBackgroundColor = new Color(0.07f, 0.10f, 0.16f, 0.65f);
        [SerializeField] private Color arrowIconColor = Color.white;

        [Tooltip("Toggle visibility of the category badge (e.g. NEWS).")]
        [SerializeField] private bool showBadge = true;
        [Range(0f, 12f)]
        [SerializeField] private float badgeCornerRadius = 5f;
        [SerializeField] private Color badgeBackgroundColor = new Color(0.06f, 0.09f, 0.14f, 0.88f);
        [SerializeField] private Color badgeTextColor = new Color(0.86f, 0.92f, 1f);

        [Header("Pagination Dots")]
        [Tooltip("Diameter of pagination dots in pixels.")]
        [SerializeField] private float dotSize = 10f;
        [Tooltip("Spacing between pagination dots.")]
        [SerializeField] private float dotSpacing = 8f;
        [SerializeField] private Color activeDotColor = Color.white;
        [SerializeField] private Color inactiveDotColor = new Color(1f, 1f, 1f, 0.35f);
        [Range(1f, 2f)]
        [SerializeField] private float activeDotScale = 1.3f;

        [Header("Typography Scaling")]
        [Tooltip("If true, fonts automatically scale proportionally when you resize the card.")]
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
        [Tooltip("Full URL to the JSON endpoint (e.g. http://localhost:3000/api/carousel).")]
        [SerializeField] private string remoteApiUrl = "http://localhost:3000/api/carousel";
        [Tooltip("Interval in seconds to re-fetch slides in the background. Set to 0 to fetch only once on startup.")]
        [SerializeField] private float autoRefreshInterval = 0f;
        [Tooltip("If true, the card stays hidden until the slides from the web server have loaded, so the local placeholder slides are never shown. If the server can't be reached, the card stays hidden.")]
        [SerializeField] private bool hideUntilRemoteLoaded = true;

        [Header("Slides Configuration")]
        [SerializeField] private List<BillboardSlide> slides = new List<BillboardSlide>();
        [SerializeField] private float autoAdvanceInterval = 4.5f;
        [SerializeField] private bool autoAdvance = true;

        [Header("UI References")]
        [SerializeField] private Image cardBorderImage;
        [SerializeField] private Image maskImage;
        [SerializeField] private Shadow cardShadow;
        [SerializeField] private Image slideBackgroundImage;
        [SerializeField] private Image vignetteOverlayImage;
        [SerializeField] private Text titleText;
        [SerializeField] private Text subtitleText;
        [SerializeField] private Text badgeText;
        [SerializeField] private Image badgeBackgroundImage;
        [SerializeField] private Transform dotsContainer;
        [SerializeField] private GameObject prevButton;
        [SerializeField] private GameObject nextButton;
        [SerializeField] private Image prevButtonImage;
        [SerializeField] private Image nextButtonImage;
        [SerializeField] private List<Image> paginationDots = new List<Image>();
        [SerializeField] private CanvasGroup contentCanvasGroup;

        public event Action<BillboardSlide> OnSlideClicked;

        private int currentIndex = 0;
        private Coroutine autoSlideCoroutine;
        private Coroutine transitionCoroutine;
        private Coroutine fetchCoroutine;
        private Coroutine autoRefreshCoroutine;
        private List<Sprite> generatedSprites = new List<Sprite>();
        private static readonly Dictionary<string, Texture2D> textureCache = new Dictionary<string, Texture2D>();
        private static readonly Dictionary<int, Sprite> slicedSpriteCache = new Dictionary<int, Sprite>();

        // Slides of the last successful fetch per endpoint, so a scene reload shows them at once.
        private static readonly Dictionary<string, List<BillboardSlide>> remoteSlidesCache = new Dictionary<string, List<BillboardSlide>>();
        private CanvasGroup rootCanvasGroup;
        private bool hasStarted;
        private bool waitingForRemote;
        private bool hasRemoteSlides;

        /// <summary>True while the card is hidden because the remote slides have not arrived yet.</summary>
        public bool IsWaitingForRemote => waitingForRemote;
        /// <summary>True when this card is set up to take its slides from the web server.</summary>
        public bool ExpectsRemoteSlides => fetchRemoteOnStart && !string.IsNullOrEmpty(remoteApiUrl);
        /// <summary>True once the slides on show are the web server's, not the local placeholders.</summary>
        public bool HasRemoteSlides => hasRemoteSlides;
        /// <summary>A host that draws the billboard itself turns this off and waits on <see cref="HasRemoteSlides"/>.</summary>
        public bool HideUntilRemoteLoaded
        {
            get => hideUntilRemoteLoaded;
            set
            {
                hideUntilRemoteLoaded = value;
                if (!value && waitingForRemote) SetCardHidden(false);
            }
        }

        // Downloaded banner images are kept on disk so they are only fetched once per
        // machine instead of on every game launch.
        private const string DiskCacheFolder = "ZeroBillboardCache";
        private const double DiskCacheMaxAgeDays = 30.0;

        public int CurrentIndex => currentIndex;
        public int SlideCount => slides != null ? slides.Count : 0;
        public string RemoteApiUrl { get => remoteApiUrl; set => remoteApiUrl = value; }

        public static Sprite GetOrCreateSlicedRoundedSprite(int radius)
        {
            if (radius <= 0) return null;
            if (slicedSpriteCache.TryGetValue(radius, out Sprite cached) && cached != null)
            {
                return cached;
            }

            int size = Mathf.Max(radius * 2 + 4, 16);
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(0, Mathf.Abs(x - size * 0.5f) - (size * 0.5f - radius));
                    float dy = Mathf.Max(0, Mathf.Abs(y - size * 0.5f) - (size * 0.5f - radius));
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(radius - dist);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();

            Vector4 border = new Vector4(radius, radius, radius, radius);
            Sprite sp = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
            slicedSpriteCache[radius] = sp;
            return sp;
        }

        public void ApplyLayoutSettings()
        {
            RectTransform rt = GetComponent<RectTransform>();
            if (rt == null) return;

            // Auto-detect UI components if unassigned
            if (cardBorderImage == null) cardBorderImage = GetComponent<Image>();
            if (cardShadow == null) cardShadow = GetComponent<Shadow>();
            if (maskImage == null)
            {
                Transform inner = transform.Find("InnerContainer");
                if (inner != null) maskImage = inner.GetComponent<Image>();
            }
            if (slideBackgroundImage == null)
            {
                Transform bg = transform.Find("InnerContainer/SlideBackgroundImage");
                if (bg != null) slideBackgroundImage = bg.GetComponent<Image>();
            }
            if (vignetteOverlayImage == null)
            {
                Transform vo = transform.Find("InnerContainer/VignetteOverlay");
                if (vo != null) vignetteOverlayImage = vo.GetComponent<Image>();
            }
            if (contentCanvasGroup == null)
            {
                Transform cl = transform.Find("InnerContainer/ContentLayer");
                if (cl != null) contentCanvasGroup = cl.GetComponent<CanvasGroup>();
            }
            if (titleText == null)
            {
                Transform t = transform.Find("InnerContainer/ContentLayer/SlideTitle");
                if (t != null) titleText = t.GetComponent<Text>();
            }
            if (subtitleText == null)
            {
                Transform st = transform.Find("InnerContainer/ContentLayer/SlideSubtitle");
                if (st != null) subtitleText = st.GetComponent<Text>();
            }
            if (badgeText == null)
            {
                Transform bt = transform.Find("InnerContainer/ContentLayer/NewsBadge/Text");
                if (bt != null) badgeText = bt.GetComponent<Text>();
            }
            if (badgeBackgroundImage == null)
            {
                Transform nb = transform.Find("InnerContainer/ContentLayer/NewsBadge");
                if (nb != null) badgeBackgroundImage = nb.GetComponent<Image>();
            }
            if (dotsContainer == null)
            {
                Transform dc = transform.Find("InnerContainer/ContentLayer/DotsContainer");
                if (dc != null) dotsContainer = dc;
            }
            if (prevButton == null)
            {
                Transform pb = transform.Find("BtnPrevSlide");
                if (pb != null) prevButton = pb.gameObject;
            }
            if (nextButton == null)
            {
                Transform nb = transform.Find("BtnNextSlide");
                if (nb != null) nextButton = nb.gameObject;
            }
            if (prevButtonImage == null && prevButton != null) prevButtonImage = prevButton.GetComponent<Image>();
            if (nextButtonImage == null && nextButton != null) nextButtonImage = nextButton.GetComponent<Image>();

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

            // 3. Curved Corners & Outer Border
            int cr = Mathf.RoundToInt(cornerRadius);
            Sprite cardRoundedSprite = GetOrCreateSlicedRoundedSprite(cr);

            if (cardBorderImage != null)
            {
                cardBorderImage.enabled = showBorder;
                cardBorderImage.sprite = cardRoundedSprite;
                cardBorderImage.type = (cardRoundedSprite != null) ? Image.Type.Sliced : Image.Type.Simple;
                cardBorderImage.color = borderColor;
            }

            // 4. Inner Container Masking (Clips slide images to curved corners)
            if (maskImage != null)
            {
                maskImage.sprite = cardRoundedSprite;
                maskImage.type = (cardRoundedSprite != null) ? Image.Type.Sliced : Image.Type.Simple;
                maskImage.color = Color.white;

                CanvasRenderer maskCr = maskImage.GetComponent<CanvasRenderer>();
                if (maskCr != null) maskCr.cullTransparentMesh = false;

                RectTransform maskRt = maskImage.rectTransform;
                float bw = (showBorder && borderWidth > 0f) ? borderWidth : 0f;
                maskRt.offsetMin = new Vector2(bw, bw);
                maskRt.offsetMax = new Vector2(-bw, -bw);

                Mask mask = maskImage.GetComponent<Mask>();
                if (mask != null)
                {
                    mask.enabled = (cornerRadius > 0f);
                    mask.showMaskGraphic = false;
                }
            }

            // 5. Card Drop Shadow
            if (cardShadow != null)
            {
                cardShadow.enabled = showCardShadow;
                cardShadow.effectColor = cardShadowColor;
                cardShadow.effectDistance = cardShadowOffset;
            }

            // 6. Vignette overlay
            if (vignetteOverlayImage != null)
            {
                Color c = vignetteColor;
                c.a = vignetteStrength * 0.75f;
                vignetteOverlayImage.color = c;
                vignetteOverlayImage.raycastTarget = false;
            }

            // 7. Navigation Arrows
            if (prevButton != null) prevButton.SetActive(showNavigationArrows);
            if (nextButton != null) nextButton.SetActive(showNavigationArrows);

            int ar = Mathf.RoundToInt(arrowCornerRadius);
            Sprite arrowSp = GetOrCreateSlicedRoundedSprite(ar);
            if (prevButtonImage != null)
            {
                prevButtonImage.sprite = arrowSp;
                prevButtonImage.type = (arrowSp != null) ? Image.Type.Sliced : Image.Type.Simple;
                prevButtonImage.color = arrowBackgroundColor;
            }
            if (nextButtonImage != null)
            {
                nextButtonImage.sprite = arrowSp;
                nextButtonImage.type = (arrowSp != null) ? Image.Type.Sliced : Image.Type.Simple;
                nextButtonImage.color = arrowBackgroundColor;
            }

            // 8. Badge Styling
            if (badgeBackgroundImage != null)
            {
                badgeBackgroundImage.gameObject.SetActive(showBadge);
                int br = Mathf.RoundToInt(badgeCornerRadius);
                Sprite badgeSp = GetOrCreateSlicedRoundedSprite(br);
                badgeBackgroundImage.sprite = badgeSp;
                badgeBackgroundImage.type = (badgeSp != null) ? Image.Type.Sliced : Image.Type.Simple;
                badgeBackgroundImage.color = badgeBackgroundColor;
            }
            if (badgeText != null)
            {
                badgeText.color = badgeTextColor;
            }

            // 9. Typography scaling
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

            // 10. Dots layout
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
                    if (!Application.isPlaying && slides != null && slides.Count > 0)
                    {
                        GenerateProceduralGradients();
                        UpdateSlideDisplay(Mathf.Clamp(currentIndex, 0, slides.Count - 1), immediate: true);
                    }
                }
            };
        }

        [ContextMenu("Sync from Web Dashboard Now")]
        public void EditorSyncFromWeb()
        {
            string targetUrl = remoteApiUrl != null ? remoteApiUrl.Trim() : "";
            if (string.IsNullOrEmpty(targetUrl))
            {
                Debug.LogWarning("[ZeroBillboard] remoteApiUrl is empty! Set it to http://localhost:3000/api/carousel");
                return;
            }

            if (!targetUrl.Contains("/api/carousel") && !targetUrl.Contains("/api/billboard"))
            {
                if (targetUrl.EndsWith("/")) targetUrl = targetUrl.Substring(0, targetUrl.Length - 1);
                targetUrl += "/api/carousel";
            }

            UnityEditor.EditorUtility.DisplayProgressBar("Zero Billboard", "Connecting to " + targetUrl, 0.15f);
            try
            {
                using (UnityWebRequest req = UnityWebRequest.Get(targetUrl))
                {
                    req.timeout = 10;
                    var op = req.SendWebRequest();
                    while (!op.isDone)
                    {
                        System.Threading.Thread.Sleep(20);
                    }

                    if (req.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogError($"[ZeroBillboard] Could not connect to API: {req.error}");
                        return;
                    }

                    string json = req.downloadHandler.text;
                    RemoteBillboardResponse response = JsonUtility.FromJson<RemoteBillboardResponse>(json);
                    if (response == null || response.slides == null || response.slides.Count == 0)
                    {
                        Debug.LogWarning("[ZeroBillboard] Server returned empty slides list.");
                        return;
                    }

                    List<BillboardSlide> newSlides = new List<BillboardSlide>();
                    List<Sprite> newSprites = new List<Sprite>();

                    for (int i = 0; i < response.slides.Count; i++)
                    {
                        var data = response.slides[i];
                        UnityEditor.EditorUtility.DisplayProgressBar("Zero Billboard", $"Downloading slide '{data.title}' ({i + 1}/{response.slides.Count})", 0.2f + 0.7f * ((float)i / response.slides.Count));

                        Color startCol = ParseHexColor(data.bgGradientStart, new Color(0.20f, 0.10f, 0.40f));
                        Color endCol = ParseHexColor(data.bgGradientEnd, new Color(0.80f, 0.20f, 0.60f));
                        BillboardSlide slide = new BillboardSlide(data.title, data.subtitle, data.badge, startCol, endCol)
                        {
                            imageUrl = data.imageUrl,
                            actionUrl = data.actionUrl
                        };

                        Sprite slideSprite = null;
                        if (!string.IsNullOrEmpty(data.imageUrl))
                        {
                            string imgUrl = data.imageUrl.Trim();
                            using (UnityWebRequest imgReq = UnityWebRequest.Get(imgUrl))
                            {
                                imgReq.timeout = 10;
                                var imgOp = imgReq.SendWebRequest();
                                while (!imgOp.isDone)
                                {
                                    System.Threading.Thread.Sleep(20);
                                }

                                if (imgReq.result == UnityWebRequest.Result.Success && imgReq.downloadHandler.data != null && imgReq.downloadHandler.data.Length > 0)
                                {
                                    Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                                    if (tex.LoadImage(imgReq.downloadHandler.data))
                                    {
                                        tex.filterMode = FilterMode.Bilinear;
                                        tex.wrapMode = TextureWrapMode.Clamp;
                                        slideSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                                        slideSprite.name = $"RemoteSlide_{i}";
                                    }
                                }
                            }
                        }

                        if (slideSprite == null)
                        {
                            Texture2D gradTex = CreateDiagonalGradientTexture(256, 144, startCol, endCol);
                            slideSprite = Sprite.Create(gradTex, new Rect(0, 0, gradTex.width, gradTex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                        }

                        slide.customImage = slideSprite;
                        newSlides.Add(slide);
                        newSprites.Add(slideSprite);
                    }

                    slides = newSlides;
                    generatedSprites = newSprites;
                    RebuildDotsUI();
                    currentIndex = 0;
                    ApplyLayoutSettings();
                    UpdateSlideDisplay(0, immediate: true);
                    UnityEditor.EditorUtility.SetDirty(this);
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);

                    Debug.Log($"[ZeroBillboard] Successfully synced {slides.Count} slides from web server in Editor!");
                }
            }
            finally
            {
                UnityEditor.EditorUtility.ClearProgressBar();
            }
        }
#endif

        private void Awake()
        {
            ApplyLayoutSettings();

            if (slides.Count == 0)
            {
                // Default Fortnite / Game style demo fallback slides
                slides.Add(new BillboardSlide(
                    "THE BIG BANG",
                    "A NEW BEGINNING • LIVE EVENT",
                    "NEWS",
                    new Color(0.24f, 0.06f, 0.44f), // Deep Cosmic Purple
                    new Color(0.85f, 0.22f, 0.65f)  // Magenta Starburst
                ));

                slides.Add(new BillboardSlide(
                    "BILLBOARD SUITE 2.0",
                    "DYNAMIC 2D & 3D CAMERA FACING",
                    "UPDATE",
                    new Color(0.08f, 0.25f, 0.45f), // Midnight Blue
                    new Color(0.15f, 0.75f, 0.85f)  // Cyan Plasma
                ));

                slides.Add(new BillboardSlide(
                    "IMPOSTOR BAKERY",
                    "MULTI-ANGLE SPRITE RENDERING",
                    "FEATURE",
                    new Color(0.40f, 0.15f, 0.05f), // Ember Deep
                    new Color(0.95f, 0.55f, 0.12f)  // Solar Flare
                ));

                slides.Add(new BillboardSlide(
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
                if (TryGetKnownRemoteSlides(out List<BillboardSlide> known))
                {
                    ApplyRemoteSlides(known);
                }
                else if (hideUntilRemoteLoaded)
                {
                    SetCardHidden(true);
                }

                FetchRemoteSlides();
            }

            hasStarted = true;

            if (autoRefreshInterval > 0f)
            {
                autoRefreshCoroutine = StartCoroutine(AutoRefreshRoutine());
            }
        }

        private float lastScrollTime = 0f;

        private void Update()
        {
            if (enableMouseScroll && !waitingForRemote)
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

        private void OnEnable()
        {
            // The fetch is stopped with the object; without this the card would stay hidden for good.
            if (hasStarted && waitingForRemote)
            {
                FetchRemoteSlides();
            }
        }

        private void SetCardHidden(bool hidden)
        {
            waitingForRemote = hidden;
            if (rootCanvasGroup == null) rootCanvasGroup = GetComponent<CanvasGroup>();
            if (rootCanvasGroup == null) rootCanvasGroup = gameObject.AddComponent<CanvasGroup>();
            rootCanvasGroup.alpha = hidden ? 0f : 1f;
            rootCanvasGroup.interactable = !hidden;
            rootCanvasGroup.blocksRaycasts = !hidden;
        }

        private bool TryGetKnownRemoteSlides(out List<BillboardSlide> known)
        {
            known = null;
            if (!remoteSlidesCache.TryGetValue(remoteApiUrl.Trim(), out List<BillboardSlide> cached)) return false;

            // Sprites can be unloaded between scenes; fall back to a normal fetch then
            foreach (BillboardSlide slide in cached)
            {
                if (slide.customImage == null) return false;
            }

            known = cached;
            return known.Count > 0;
        }

        private void ApplyRemoteSlides(List<BillboardSlide> remoteSlides)
        {
            slides = new List<BillboardSlide>(remoteSlides);
            generatedSprites = new List<Sprite>();
            foreach (BillboardSlide slide in slides) generatedSprites.Add(slide.customImage);

            RebuildDotsUI();

            currentIndex = Mathf.Clamp(currentIndex, 0, slides.Count - 1);
            UpdateSlideDisplay(currentIndex, immediate: true);

            hasRemoteSlides = true;
            if (waitingForRemote) SetCardHidden(false);
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

        private static string GetDiskCacheDirectory()
        {
            return Path.Combine(Application.persistentDataPath, DiskCacheFolder);
        }

        private static string GetDiskCachePath(string url)
        {
            return Path.Combine(GetDiskCacheDirectory(), Hash128.Compute(url).ToString() + ".img");
        }

        private static bool TryLoadTextureFromDisk(string url, out Texture2D texture)
        {
            texture = null;
            try
            {
                string path = GetDiskCachePath(url);
                if (!File.Exists(path)) return false;

                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!tex.LoadImage(File.ReadAllBytes(path)))
                {
                    Destroy(tex);
                    File.Delete(path);
                    return false;
                }

                tex.filterMode = FilterMode.Bilinear;
                tex.wrapMode = TextureWrapMode.Clamp;
                // Mark as recently used so PruneDiskCache keeps it
                File.SetLastWriteTimeUtc(path, DateTime.UtcNow);
                texture = tex;
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ZeroBillboard] Could not read cached image: {ex.Message}");
                return false;
            }
        }

        private static void SaveTextureToDisk(string url, byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return;
            try
            {
                Directory.CreateDirectory(GetDiskCacheDirectory());
                File.WriteAllBytes(GetDiskCachePath(url), bytes);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ZeroBillboard] Could not write image cache: {ex.Message}");
            }
        }

        private static void PruneDiskCache()
        {
            try
            {
                string dir = GetDiskCacheDirectory();
                if (!Directory.Exists(dir)) return;

                DateTime cutoff = DateTime.UtcNow.AddDays(-DiskCacheMaxAgeDays);
                foreach (string file in Directory.GetFiles(dir, "*.img"))
                {
                    if (File.GetLastWriteTimeUtc(file) < cutoff) File.Delete(file);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ZeroBillboard] Could not prune image cache: {ex.Message}");
            }
        }

        [ContextMenu("Clear Image Cache & Reload")]
        public void ClearImageCacheAndReload()
        {
            textureCache.Clear();
            try
            {
                string dir = GetDiskCacheDirectory();
                if (Directory.Exists(dir)) Directory.Delete(dir, true);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ZeroBillboard] Could not clear disk image cache: {ex.Message}");
            }
            Debug.Log("[ZeroBillboard] Image texture cache cleared.");
            FetchRemoteSlides();
        }

        private IEnumerator FetchRemoteSlidesRoutine()
        {
            string targetUrl = remoteApiUrl != null ? remoteApiUrl.Trim() : "";
            if (string.IsNullOrEmpty(targetUrl)) yield break;

            // Auto-detect and fix misplaced image URLs or root URLs in Inspector
            if (targetUrl.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                targetUrl.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                targetUrl.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                targetUrl.EndsWith(".webp", StringComparison.OrdinalIgnoreCase) ||
                targetUrl.Contains("/uploads/"))
            {
                Debug.LogError($"[ZeroBillboard] 'Remote API URL' is currently set to an image URL ('{targetUrl}') instead of the JSON API endpoint!\n" +
                               "👉 Please change 'Remote API URL' in the Inspector back to: http://localhost:3000/api/carousel");
                yield break;
            }

            if (!targetUrl.Contains("/api/carousel") && !targetUrl.Contains("/api/billboard"))
            {
                if (targetUrl.EndsWith("/")) targetUrl = targetUrl.Substring(0, targetUrl.Length - 1);
                targetUrl += "/api/carousel";
                Debug.LogWarning($"[ZeroBillboard] 'Remote API URL' was missing the API route. Automatically connecting to: {targetUrl}");
            }

            Debug.Log($"[ZeroBillboard] Connecting to CMS endpoint: {targetUrl}");
            using (UnityWebRequest req = UnityWebRequest.Get(targetUrl))
            {
                req.timeout = 8;
                yield return req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"[ZeroBillboard] Remote fetch notice: {req.error}. Using active local slides.");
                    yield break;
                }

                string json = req.downloadHandler.text;
                if (string.IsNullOrEmpty(json))
                {
                    Debug.LogWarning("[ZeroBillboard] Server returned empty response.");
                    yield break;
                }

                if (json.TrimStart().StartsWith("<"))
                {
                    Debug.LogError($"[ZeroBillboard] The server at '{targetUrl}' returned an HTML page instead of JSON!\n" +
                                   "👉 Make sure 'Remote API URL' in Inspector is set to: http://localhost:3000/api/carousel");
                    yield break;
                }

                RemoteBillboardResponse response = null;
                try
                {
                    response = JsonUtility.FromJson<RemoteBillboardResponse>(json);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ZeroBillboard] Failed to parse JSON response: {ex.Message}. Response preview: {(json.Length > 100 ? json.Substring(0, 100) + "..." : json)}");
                    yield break;
                }

                if (response == null || response.slides == null || response.slides.Count == 0)
                {
                    Debug.LogWarning("[ZeroBillboard] Received empty or invalid slide list from server.");
                    yield break;
                }

                List<BillboardSlide> newSlides = new List<BillboardSlide>();
                List<Sprite> newSprites = new List<Sprite>();

                for (int i = 0; i < response.slides.Count; i++)
                {
                    RemoteSlideData data = response.slides[i];
                    Color startCol = ParseHexColor(data.bgGradientStart, new Color(0.20f, 0.10f, 0.40f));
                    Color endCol = ParseHexColor(data.bgGradientEnd, new Color(0.80f, 0.20f, 0.60f));

                    BillboardSlide slide = new BillboardSlide(data.title, data.subtitle, data.badge, startCol, endCol)
                    {
                        imageUrl = data.imageUrl,
                        actionUrl = data.actionUrl
                    };

                    Sprite slideSprite = null;

                    // Download image if imageUrl is provided
                    if (!string.IsNullOrEmpty(data.imageUrl))
                    {
                        string imgUrl = data.imageUrl.Trim();
                        if (textureCache.TryGetValue(imgUrl, out Texture2D cachedTex) && cachedTex != null)
                        {
                            slideSprite = Sprite.Create(cachedTex, new Rect(0, 0, cachedTex.width, cachedTex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                        }
                        else if (TryLoadTextureFromDisk(imgUrl, out Texture2D diskTex))
                        {
                            textureCache[imgUrl] = diskTex;
                            slideSprite = Sprite.Create(diskTex, new Rect(0, 0, diskTex.width, diskTex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                        }
                        else
                        {
                            using (UnityWebRequest imgReq = UnityWebRequestTexture.GetTexture(imgUrl))
                            {
                                imgReq.timeout = 10;
                                yield return imgReq.SendWebRequest();
                                if (imgReq.result == UnityWebRequest.Result.Success)
                                {
                                    Texture2D tex = DownloadHandlerTexture.GetContent(imgReq);
                                    if (tex != null)
                                    {
                                        tex.filterMode = FilterMode.Bilinear;
                                        tex.wrapMode = TextureWrapMode.Clamp;
                                        textureCache[imgUrl] = tex;
                                        SaveTextureToDisk(imgUrl, imgReq.downloadHandler.data);
                                        slideSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                                        Debug.Log($"[ZeroBillboard] Downloaded banner image for '{data.title}': {imgUrl} ({tex.width}x{tex.height})");
                                    }
                                }
                                else
                                {
                                    // Fallback attempt: Raw byte download + ImageConversion.LoadImage
                                    using (UnityWebRequest rawReq = UnityWebRequest.Get(imgUrl))
                                    {
                                        rawReq.timeout = 10;
                                        yield return rawReq.SendWebRequest();
                                        if (rawReq.result == UnityWebRequest.Result.Success && rawReq.downloadHandler.data != null && rawReq.downloadHandler.data.Length > 0)
                                        {
                                            Texture2D fallbackTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                                            if (fallbackTex.LoadImage(rawReq.downloadHandler.data))
                                            {
                                                fallbackTex.filterMode = FilterMode.Bilinear;
                                                fallbackTex.wrapMode = TextureWrapMode.Clamp;
                                                textureCache[imgUrl] = fallbackTex;
                                                SaveTextureToDisk(imgUrl, rawReq.downloadHandler.data);
                                                slideSprite = Sprite.Create(fallbackTex, new Rect(0, 0, fallbackTex.width, fallbackTex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                                                Debug.Log($"[ZeroBillboard] Fallback loaded banner image for '{data.title}': {imgUrl} ({fallbackTex.width}x{fallbackTex.height})");
                                            }
                                        }
                                        else
                                        {
                                            Debug.LogWarning($"[ZeroBillboard] Could not download image for slide '{data.title}' from '{imgUrl}': {imgReq.error}");
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // Procedural gradient fallback if no custom image
                    if (slideSprite == null)
                    {
                        Texture2D gradTex = CreateDiagonalGradientTexture(256, 144, startCol, endCol);
                        slideSprite = Sprite.Create(gradTex, new Rect(0, 0, gradTex.width, gradTex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                    }

                    slide.customImage = slideSprite;
                    newSlides.Add(slide);
                    newSprites.Add(slideSprite);
                }

                PruneDiskCache();

                // Apply downloaded slides
                remoteSlidesCache[remoteApiUrl.Trim()] = newSlides;
                ApplyRemoteSlides(newSlides);

                Debug.Log($"[ZeroBillboard] Successfully updated {slides.Count} slides from web server!");
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
                BillboardSlide activeSlide = slides[currentIndex];
                Debug.Log($"[ZeroBillboard] Slide clicked: {activeSlide.title}");
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

        private void ApplySlideData(BillboardSlide slide, int index)
        {
            if (titleText != null) titleText.text = slide.title;
            if (subtitleText != null) subtitleText.text = slide.subtitle;
            if (badgeText != null) badgeText.text = slide.badge;

            if (slideBackgroundImage == null)
            {
                Transform bg = transform.Find("InnerContainer/SlideBackgroundImage");
                if (bg != null) slideBackgroundImage = bg.GetComponent<Image>();
            }

            if (slideBackgroundImage != null)
            {
                slideBackgroundImage.gameObject.SetActive(true);
                slideBackgroundImage.enabled = true;
                slideBackgroundImage.type = Image.Type.Simple;
                slideBackgroundImage.preserveAspect = false;

                if (slide.customImage != null)
                {
                    slideBackgroundImage.sprite = slide.customImage;
                }
                else if (index >= 0 && index < generatedSprites.Count && generatedSprites[index] != null)
                {
                    slideBackgroundImage.sprite = generatedSprites[index];
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
                    paginationDots[i].transform.localScale = isActive ? new Vector3(activeDotScale, activeDotScale, 1f) : Vector3.one;
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

    [System.Obsolete("Use ZeroBillboard instead.")]
    public class NewsCarousel : ZeroBillboard
    {
    }
}
