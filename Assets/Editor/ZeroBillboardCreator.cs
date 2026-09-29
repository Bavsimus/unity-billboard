using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.Events;
using BillboardTool.UI;

namespace BillboardTool.Editor
{
    /// <summary>
    /// One-click creator, prefab generator, and package exporter for the Zero Billboard widget.
    /// Can be added to any scene/canvas via:
    /// - Hierarchy right-click: UI > Zero Billboard (16:9)
    /// - Top Menu: Tools > Zero Billboard > Add Zero Billboard to Scene
    /// - Prefab: Assets/Prefabs/ZeroBillboard.prefab
    /// </summary>
    [InitializeOnLoad]
    public static class ZeroBillboardCreator
    {
        public const string PREFAB_DIR = "Assets/Prefabs";
        public const string PREFAB_PATH = "Assets/Prefabs/ZeroBillboard.prefab";

        static ZeroBillboardCreator()
        {
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(PREFAB_PATH) && !EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    CreateOrUpdatePrefab(false);
                }
            };
        }

        [MenuItem("GameObject/UI/Zero Billboard (16:9)", false, 20)]
        public static void CreateFromHierarchy(MenuCommand menuCommand)
        {
            GameObject context = menuCommand.context as GameObject;
            AddZeroBillboardToScene(context);
        }

        [MenuItem("Tools/Zero Billboard/Add Zero Billboard to Scene", false, 10)]
        public static void CreateFromMenu()
        {
            AddZeroBillboardToScene(Selection.activeGameObject);
        }

        [MenuItem("Tools/Zero Billboard/Create or Update Zero Billboard Prefab", false, 11)]
        public static void CreateOrUpdatePrefabMenu()
        {
            CreateOrUpdatePrefab(true);
        }

        [MenuItem("Tools/Zero Billboard/Export Zero Billboard Package (.unitypackage)", false, 25)]
        public static void ExportUnityPackage()
        {
            // Ensure prefab is up to date first
            CreateOrUpdatePrefab(false);

            List<string> assetPaths = new List<string>
            {
                "Assets/Scripts/UI/ZeroBillboard.cs",
                "Assets/Scripts/UI/UIButtonAnimator.cs",
                "Assets/Editor/ZeroBillboardCreator.cs"
            };

            if (File.Exists(PREFAB_PATH))
            {
                assetPaths.Add(PREFAB_PATH);
            }

            string exportPath = "ZeroBillboard.unitypackage";
            AssetDatabase.ExportPackage(assetPaths.ToArray(), exportPath, ExportPackageOptions.Default);
            Debug.Log($"[ZeroBillboard] Successfully exported standalone package to: {Path.GetFullPath(exportPath)}");
            EditorUtility.RevealInFinder(exportPath);
        }

        public static GameObject AddZeroBillboardToScene(GameObject context = null)
        {
            Transform parent = null;
            if (context != null)
            {
                Canvas inParent = context.GetComponentInParent<Canvas>();
                if (inParent != null)
                {
                    parent = context.transform;
                }
            }

            if (parent == null)
            {
                Canvas sceneCanvas = FindSceneObject<Canvas>();
                if (sceneCanvas == null)
                {
                    sceneCanvas = CreateDefaultCanvas();
                }
                parent = sceneCanvas.transform;
            }

            // Ensure EventSystem is present in scene
            if (FindSceneObject<EventSystem>() == null)
            {
                CreateDefaultEventSystem();
            }

            GameObject instance = null;

            // Ensure prefab exists
            if (!File.Exists(PREFAB_PATH))
            {
                CreateOrUpdatePrefab(false);
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
            if (prefab != null)
            {
                instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                Undo.RegisterCreatedObjectUndo(instance, "Create Zero Billboard");
            }
            else
            {
                instance = BuildZeroBillboard(parent, new Vector2(60f, 60f));
                Undo.RegisterCreatedObjectUndo(instance, "Create Zero Billboard");
            }

            Selection.activeGameObject = instance;
            Debug.Log("[ZeroBillboard] Zero Billboard added to Canvas successfully!");
            return instance;
        }

        public static GameObject CreateOrUpdatePrefab(bool logMessage = true)
        {
            EnsureDirectory(PREFAB_DIR);

            GameObject billboardObj = BuildZeroBillboard(null, new Vector2(60f, 60f));
            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(billboardObj, PREFAB_PATH);
            Object.DestroyImmediate(billboardObj);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (logMessage)
            {
                Debug.Log($"[ZeroBillboard] Prefab created/updated successfully at: {PREFAB_PATH}");
                EditorGUIUtility.PingObject(savedPrefab);
            }

            return savedPrefab;
        }

        public static GameObject BuildZeroBillboard(Transform parent, Vector2? anchoredPosition = null)
        {
            Font defaultFont = GetDefaultFont();

            // 1. Root Card Object
            GameObject billboardCard = CreateUIObject("ZeroBillboard", parent);
            RectTransform ncRect = billboardCard.GetComponent<RectTransform>();
            ncRect.anchorMin = new Vector2(0f, 0f);
            ncRect.anchorMax = new Vector2(0f, 0f);
            ncRect.pivot = new Vector2(0f, 0f);
            ncRect.anchoredPosition = anchoredPosition ?? new Vector2(60f, 60f);
            ncRect.sizeDelta = new Vector2(560f, 315f);

            Image ncBorderImg = billboardCard.AddComponent<Image>();
            ncBorderImg.color = new Color(0.25f, 0.35f, 0.48f, 0.70f);

            Shadow cardShadow = billboardCard.AddComponent<Shadow>();
            cardShadow.effectColor = new Color(0f, 0f, 0f, 0.65f);
            cardShadow.effectDistance = new Vector2(3f, -4f);

            Button ncButton = billboardCard.AddComponent<Button>();
            billboardCard.AddComponent<UIButtonAnimator>();

            // 2. Inner clipping container with rounded corner Mask
            GameObject innerContainer = CreateUIObject("InnerContainer", billboardCard.transform);
            RectTransform icRect = innerContainer.GetComponent<RectTransform>();
            StretchFull(icRect);
            icRect.offsetMin = new Vector2(2f, 2f);
            icRect.offsetMax = new Vector2(-2f, -2f);
            Image maskImg = innerContainer.AddComponent<Image>();
            Mask mask = innerContainer.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            // 3. Background Image
            GameObject bgImageObj = CreateUIObject("SlideBackgroundImage", innerContainer.transform);
            StretchFull(bgImageObj.GetComponent<RectTransform>());
            Image slideBgImage = bgImageObj.AddComponent<Image>();
            slideBgImage.color = Color.white;

            // 4. Smooth vertical vignette overlay
            GameObject gradOverlay = CreateUIObject("VignetteOverlay", innerContainer.transform);
            StretchFull(gradOverlay.GetComponent<RectTransform>());
            Image goImg = gradOverlay.AddComponent<Image>();
            goImg.sprite = CreateVerticalVignetteSprite(16, 128);
            goImg.color = Color.white;

            // 5. Content Layer
            GameObject contentObj = CreateUIObject("ContentLayer", innerContainer.transform);
            StretchFull(contentObj.GetComponent<RectTransform>());
            CanvasGroup contentCg = contentObj.AddComponent<CanvasGroup>();

            // 6. Pagination Dots Container
            GameObject dotsContainer = CreateUIObject("DotsContainer", contentObj.transform);
            RectTransform dcRect = dotsContainer.GetComponent<RectTransform>();
            dcRect.anchorMin = new Vector2(0f, 1f);
            dcRect.anchorMax = new Vector2(0f, 1f);
            dcRect.pivot = new Vector2(0f, 1f);
            dcRect.anchoredPosition = new Vector2(22f, -18f);
            dcRect.sizeDelta = new Vector2(160f, 20f);

            HorizontalLayoutGroup hlg = dotsContainer.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8f;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            Sprite circleSprite = CreateCircleSprite(32);
            List<Image> dotsList = new List<Image>();
            List<Button> dotButtons = new List<Button>();
            for (int i = 0; i < 4; i++)
            {
                GameObject dot = CreateUIObject($"Dot_{i}", dotsContainer.transform);
                RectTransform dotRect = dot.GetComponent<RectTransform>();
                dotRect.sizeDelta = new Vector2(10f, 10f);
                Image dotImg = dot.AddComponent<Image>();
                dotImg.sprite = circleSprite;
                dotImg.color = (i == 0) ? Color.white : new Color(1f, 1f, 1f, 0.35f);
                dot.transform.localScale = (i == 0) ? new Vector3(1.3f, 1.3f, 1f) : Vector3.one;

                Button dotBtn = dot.AddComponent<Button>();
                dotsList.Add(dotImg);
                dotButtons.Add(dotBtn);
            }

            // 7. News Badge Pill
            GameObject badgeObj = CreateUIObject("NewsBadge", contentObj.transform);
            RectTransform bRect = badgeObj.GetComponent<RectTransform>();
            bRect.anchorMin = new Vector2(1f, 1f);
            bRect.anchorMax = new Vector2(1f, 1f);
            bRect.pivot = new Vector2(1f, 1f);
            bRect.anchoredPosition = new Vector2(-18f, -16f);
            bRect.sizeDelta = new Vector2(74f, 26f);

            Image badgeImg = badgeObj.AddComponent<Image>();
            badgeImg.sprite = CreateRoundedRectSprite(74, 26, 5f);
            badgeImg.color = new Color(0.06f, 0.09f, 0.14f, 0.88f);

            GameObject badgeTextObj = CreateTextObject("Text", "NEWS", 11, FontStyle.Bold, new Color(0.86f, 0.92f, 1f), badgeObj.transform, defaultFont);
            StretchFull(badgeTextObj.GetComponent<RectTransform>());
            Text badgeText = badgeTextObj.GetComponent<Text>();
            badgeText.alignment = TextAnchor.MiddleCenter;

            // 8. Subtitle & Title
            GameObject subTitleTextObj = CreateTextObject("SlideSubtitle", "A NEW BEGINNING • LIVE EVENT", 12, FontStyle.Bold, new Color(0.58f, 0.78f, 1f), contentObj.transform, defaultFont);
            RectTransform stRect = subTitleTextObj.GetComponent<RectTransform>();
            stRect.anchorMin = new Vector2(0f, 0f);
            stRect.anchorMax = new Vector2(1f, 0f);
            stRect.pivot = new Vector2(0f, 0f);
            stRect.anchoredPosition = new Vector2(24f, 56f);
            stRect.sizeDelta = new Vector2(-48f, 22f);
            Text slideSubText = subTitleTextObj.GetComponent<Text>();
            slideSubText.alignment = TextAnchor.MiddleLeft;

            Shadow subShadow = subTitleTextObj.AddComponent<Shadow>();
            subShadow.effectColor = new Color(0f, 0f, 0f, 0.75f);
            subShadow.effectDistance = new Vector2(1f, -1f);

            GameObject titleTextObj = CreateTextObject("SlideTitle", "THE BIG BANG", 30, FontStyle.Bold, Color.white, contentObj.transform, defaultFont);
            RectTransform ttRect = titleTextObj.GetComponent<RectTransform>();
            ttRect.anchorMin = new Vector2(0f, 0f);
            ttRect.anchorMax = new Vector2(1f, 0f);
            ttRect.pivot = new Vector2(0f, 0f);
            ttRect.anchoredPosition = new Vector2(24f, 16f);
            ttRect.sizeDelta = new Vector2(-48f, 40f);
            Text slideTitleText = titleTextObj.GetComponent<Text>();
            slideTitleText.alignment = TextAnchor.MiddleLeft;

            Shadow titleShadow = titleTextObj.AddComponent<Shadow>();
            titleShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            titleShadow.effectDistance = new Vector2(2f, -2f);

            // 9. Left & Right Navigation Arrows
            Sprite navArrowSprite = CreateRoundedRectSprite(32, 50, 6f);

            GameObject btnPrevObj = CreateUIObject("BtnPrevSlide", billboardCard.transform);
            RectTransform bpRect = btnPrevObj.GetComponent<RectTransform>();
            bpRect.anchorMin = new Vector2(0f, 0.5f);
            bpRect.anchorMax = new Vector2(0f, 0.5f);
            bpRect.anchoredPosition = new Vector2(16f, 0f);
            bpRect.sizeDelta = new Vector2(32f, 50f);
            Button btnPrev = btnPrevObj.AddComponent<Button>();
            Image bpImg = btnPrevObj.AddComponent<Image>();
            bpImg.sprite = navArrowSprite;
            bpImg.color = new Color(0.07f, 0.10f, 0.16f, 0.65f);
            GameObject bpText = CreateTextObject("Arrow", "‹", 26, FontStyle.Bold, Color.white, btnPrevObj.transform, defaultFont);
            StretchFull(bpText.GetComponent<RectTransform>());

            GameObject btnNextObj = CreateUIObject("BtnNextSlide", billboardCard.transform);
            RectTransform bnRect = btnNextObj.GetComponent<RectTransform>();
            bnRect.anchorMin = new Vector2(1f, 0.5f);
            bnRect.anchorMax = new Vector2(1f, 0.5f);
            bnRect.anchoredPosition = new Vector2(-16f, 0f);
            bnRect.sizeDelta = new Vector2(32f, 50f);
            Button btnNext = btnNextObj.AddComponent<Button>();
            Image bnImg = btnNextObj.AddComponent<Image>();
            bnImg.sprite = navArrowSprite;
            bnImg.color = new Color(0.07f, 0.10f, 0.16f, 0.65f);
            GameObject bnText = CreateTextObject("Arrow", "›", 26, FontStyle.Bold, Color.white, btnNextObj.transform, defaultFont);
            StretchFull(bnText.GetComponent<RectTransform>());

            // 10. Attach ZeroBillboard component and wire serialized references
            ZeroBillboard billboard = billboardCard.AddComponent<ZeroBillboard>();
            SerializedObject billboardSO = new SerializedObject(billboard);
            billboardSO.FindProperty("slideBackgroundImage").objectReferenceValue = slideBgImage;
            billboardSO.FindProperty("titleText").objectReferenceValue = slideTitleText;
            billboardSO.FindProperty("subtitleText").objectReferenceValue = slideSubText;
            billboardSO.FindProperty("badgeText").objectReferenceValue = badgeText;
            billboardSO.FindProperty("contentCanvasGroup").objectReferenceValue = contentCg;
            billboardSO.FindProperty("dotsContainer").objectReferenceValue = dotsContainer.transform;
            billboardSO.FindProperty("vignetteOverlayImage").objectReferenceValue = goImg;
            billboardSO.FindProperty("prevButton").objectReferenceValue = btnPrevObj;
            billboardSO.FindProperty("nextButton").objectReferenceValue = btnNextObj;
            billboardSO.FindProperty("cardBorderImage").objectReferenceValue = ncBorderImg;
            billboardSO.FindProperty("maskImage").objectReferenceValue = maskImg;
            billboardSO.FindProperty("cardShadow").objectReferenceValue = cardShadow;
            billboardSO.FindProperty("badgeBackgroundImage").objectReferenceValue = badgeImg;
            billboardSO.FindProperty("prevButtonImage").objectReferenceValue = bpImg;
            billboardSO.FindProperty("nextButtonImage").objectReferenceValue = bnImg;
            billboardSO.FindProperty("fetchRemoteOnStart").boolValue = true;
            billboardSO.FindProperty("remoteApiUrl").stringValue = "http://localhost:3000/api/carousel";

            SerializedProperty dotsProp = billboardSO.FindProperty("paginationDots");
            dotsProp.ClearArray();
            for (int i = 0; i < dotsList.Count; i++)
            {
                dotsProp.InsertArrayElementAtIndex(i);
                dotsProp.GetArrayElementAtIndex(i).objectReferenceValue = dotsList[i];
            }
            billboardSO.ApplyModifiedProperties();

            // Wire Prev / Next & Click
            UnityEventTools.AddPersistentListener(btnPrev.onClick, billboard.PreviousSlide);
            UnityEventTools.AddPersistentListener(btnNext.onClick, billboard.NextSlide);
            UnityEventTools.AddPersistentListener(ncButton.onClick, billboard.HandleSlideClicked);

            for (int i = 0; i < dotButtons.Count; i++)
            {
                UnityEventTools.AddIntPersistentListener(dotButtons[i].onClick, billboard.GoToSlide, i);
            }

            // Apply layout and styling (curved corners, borders, shadows) immediately
            billboard.ApplyLayoutSettings();

            return billboardCard;
        }

        private static T FindSceneObject<T>() where T : Object
        {
#if UNITY_2023_1_OR_NEWER
            return Object.FindAnyObjectByType<T>();
#else
            return Object.FindObjectOfType<T>();
#endif
        }

        private static Canvas CreateDefaultCanvas()
        {
            GameObject canvasObj = new GameObject("Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();
            Undo.RegisterCreatedObjectUndo(canvasObj, "Create Canvas");
            return canvas;
        }

        private static void CreateDefaultEventSystem()
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();
            Undo.RegisterCreatedObjectUndo(esObj, "Create EventSystem");
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }
            return go;
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static GameObject CreateTextObject(string name, string text, int fontSize, FontStyle style, Color color, Transform parent, Font font)
        {
            GameObject obj = CreateUIObject(name, parent);
            Text t = obj.AddComponent<Text>();
            t.text = text;
            t.fontSize = fontSize;
            t.fontStyle = style;
            t.color = color;
            t.alignment = TextAnchor.MiddleCenter;
            t.font = font;
            return obj;
        }

        private static Font GetDefaultFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            return font;
        }

        private static Sprite CreateCircleSprite(int size)
        {
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
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private static Sprite CreateVerticalVignetteSprite(int width, int height)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            for (int y = 0; y < height; y++)
            {
                float t = (float)y / (height - 1);
                float alpha;
                if (t > 0.45f)
                {
                    float normalized = (t - 0.45f) / 0.55f;
                    alpha = Mathf.Lerp(0.35f, 0.08f, normalized);
                }
                else
                {
                    float normalized = t / 0.45f;
                    alpha = Mathf.Lerp(0.92f, 0.35f, normalized);
                }
                Color col = new Color(0.04f, 0.06f, 0.10f, alpha);
                for (int x = 0; x < width; x++)
                {
                    tex.SetPixel(x, y, col);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
        }

        private static Sprite CreateRoundedRectSprite(int width, int height, float radius)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dx = Mathf.Max(0, Mathf.Abs(x - width * 0.5f) - (width * 0.5f - radius));
                    float dy = Mathf.Max(0, Mathf.Abs(y - height * 0.5f) - (height * 0.5f - radius));
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(radius - dist);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
        }

        private static void EnsureDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
    }
}
