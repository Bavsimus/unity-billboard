using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using BillboardTool.UI;

namespace BillboardTool.Editor
{
    [InitializeOnLoad]
    public static class MainMenuSceneCreator
    {
        private static Font defaultFont;

        static MainMenuSceneCreator()
        {
            EditorApplication.delayCall += () =>
            {
                string scenePath = "Assets/Scenes/MainMenu.unity";
                if (!File.Exists(scenePath))
                {
                    Debug.Log("[MainMenuSceneCreator] Auto-creating initial 2D Main Menu scene...");
                    CreateMainMenuScene();
                }
            };
        }

        [MenuItem("Tools/Billboard/Create 2D Main Menu Scene")]
        public static void CreateSceneMenu()
        {
            CreateMainMenuScene();
        }

        public static void CreateSceneFromCommandLine()
        {
            CreateMainMenuScene();
            EditorApplication.Exit(0);
        }

        public static void CreateMainMenuScene()
        {
            defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont == null)
            {
                defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            // Create a new empty scene
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Setup 2D Camera
            GameObject cameraObj = new GameObject("Main Camera");
            cameraObj.tag = "MainCamera";
            Camera cam = cameraObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.07f, 0.09f, 0.13f, 1f); // Sleek dark slate
            cameraObj.AddComponent<AudioListener>();
            cameraObj.transform.position = new Vector3(0, 0, -10f);

            // 2. Setup EventSystem
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            // 3. Setup Canvas
            GameObject canvasObj = new GameObject("MainMenuCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();
            MainMenuController controller = canvasObj.AddComponent<MainMenuController>();

            // 4. Background
            GameObject bgObj = CreateUIObject("Background", canvasObj.transform);
            StretchFull(bgObj.GetComponent<RectTransform>());
            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0.08f, 0.11f, 0.16f, 1f);

            // Subtle vignette / inner frame
            GameObject frameObj = CreateUIObject("BorderAccent", canvasObj.transform);
            StretchFull(frameObj.GetComponent<RectTransform>());
            Image frameImage = frameObj.AddComponent<Image>();
            frameImage.color = new Color(0.12f, 0.17f, 0.24f, 0.35f);

            // 5. Main Menu Panel
            GameObject mainMenuPanel = CreateUIObject("MainMenuPanel", canvasObj.transform);
            RectTransform mmRect = mainMenuPanel.GetComponent<RectTransform>();
            mmRect.anchorMin = new Vector2(0.5f, 0.5f);
            mmRect.anchorMax = new Vector2(0.5f, 0.5f);
            mmRect.sizeDelta = new Vector2(850, 850);
            mmRect.anchoredPosition = Vector2.zero;

            // Title
            GameObject titleObj = CreateTextObject("TitleText", "BILLBOARD TOOL", 58, FontStyle.Bold, new Color(0.95f, 0.97f, 1f), mainMenuPanel.transform);
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0, -60);
            titleRect.sizeDelta = new Vector2(700, 80);

            // Subtitle
            GameObject subTitleObj = CreateTextObject("SubtitleText", "2D & 3D Interactive Billboarding Suite", 22, FontStyle.Normal, new Color(0.45f, 0.65f, 0.85f), mainMenuPanel.transform);
            RectTransform subTitleRect = subTitleObj.GetComponent<RectTransform>();
            subTitleRect.anchorMin = new Vector2(0.5f, 1f);
            subTitleRect.anchorMax = new Vector2(0.5f, 1f);
            subTitleRect.anchoredPosition = new Vector2(0, -125);
            subTitleRect.sizeDelta = new Vector2(700, 40);

            // Buttons Container
            GameObject btnContainer = CreateUIObject("ButtonsContainer", mainMenuPanel.transform);
            RectTransform bcRect = btnContainer.GetComponent<RectTransform>();
            bcRect.anchorMin = new Vector2(0.5f, 0.5f);
            bcRect.anchorMax = new Vector2(0.5f, 0.5f);
            bcRect.anchoredPosition = new Vector2(0, -60);
            bcRect.sizeDelta = new Vector2(460, 520);

            VerticalLayoutGroup vlg = btnContainer.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 14f;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // Puppet & Navigation Buttons
            Button btnPlay = CreateMenuButton("BtnPlay", "▶  PLAY", new Color(0.16f, 0.55f, 0.42f), btnContainer.transform);
            Button btnWhatever = CreateMenuButton("BtnWhatever", "🤷  WHATEVER", new Color(0.72f, 0.48f, 0.18f), btnContainer.transform);
            Button btnSandbox = CreateMenuButton("BtnSandbox", "🎪  PUPPET SANDBOX", new Color(0.22f, 0.38f, 0.62f), btnContainer.transform);
            Button btnRandom = CreateMenuButton("BtnRandom", "🎲  RANDOM ACTION", new Color(0.48f, 0.28f, 0.65f), btnContainer.transform);
            Button btnSettings = CreateMenuButton("BtnSettings", "⚙  SETTINGS", new Color(0.20f, 0.26f, 0.35f), btnContainer.transform);
            Button btnCredits = CreateMenuButton("BtnCredits", "ℹ  CREDITS", new Color(0.20f, 0.26f, 0.35f), btnContainer.transform);
            Button btnQuit = CreateMenuButton("BtnQuit", "✕  QUIT", new Color(0.52f, 0.22f, 0.22f), btnContainer.transform);

            // Toast notification banner at bottom
            GameObject toastPanel = CreateUIObject("ToastBanner", canvasObj.transform);
            RectTransform toastRect = toastPanel.GetComponent<RectTransform>();
            toastRect.anchorMin = new Vector2(0.5f, 0f);
            toastRect.anchorMax = new Vector2(0.5f, 0f);
            toastRect.anchoredPosition = new Vector2(0, 75);
            toastRect.sizeDelta = new Vector2(760, 60);

            Image toastImg = toastPanel.AddComponent<Image>();
            toastImg.color = new Color(0.10f, 0.14f, 0.20f, 0.95f);
            CanvasGroup toastCg = toastPanel.AddComponent<CanvasGroup>();
            toastCg.alpha = 0f;

            GameObject toastTextObj = CreateTextObject("ToastText", "Puppet action triggered!", 19, FontStyle.Bold, new Color(0.35f, 0.85f, 0.95f), toastPanel.transform);
            StretchFull(toastTextObj.GetComponent<RectTransform>());
            Text toastText = toastTextObj.GetComponent<Text>();
            toastPanel.SetActive(false);

            // 5b. News Carousel Card (Fortnite "THE BIG BANG" news slider style)
            GameObject newsCardObj = CreateUIObject("NewsCarouselCard", canvasObj.transform);
            RectTransform ncRect = newsCardObj.GetComponent<RectTransform>();
            ncRect.anchorMin = new Vector2(0f, 0f);
            ncRect.anchorMax = new Vector2(0f, 0f);
            ncRect.pivot = new Vector2(0f, 0f);
            ncRect.anchoredPosition = new Vector2(50f, 45f);
            ncRect.sizeDelta = new Vector2(400f, 215f);

            Image ncBorderImg = newsCardObj.AddComponent<Image>();
            ncBorderImg.color = new Color(0.13f, 0.17f, 0.26f, 0.95f);

            Button ncButton = newsCardObj.AddComponent<Button>();
            newsCardObj.AddComponent<UIButtonAnimator>();

            // Inner clipping container
            GameObject innerContainer = CreateUIObject("InnerContainer", newsCardObj.transform);
            RectTransform icRect = innerContainer.GetComponent<RectTransform>();
            StretchFull(icRect);
            icRect.offsetMin = new Vector2(3, 3);
            icRect.offsetMax = new Vector2(-3, -3);
            innerContainer.AddComponent<RectMask2D>();

            // Background Image (Will be assigned procedural cosmic gradient in NewsCarousel)
            GameObject bgImageObj = CreateUIObject("SlideBackgroundImage", innerContainer.transform);
            StretchFull(bgImageObj.GetComponent<RectTransform>());
            Image slideBgImage = bgImageObj.AddComponent<Image>();
            slideBgImage.color = Color.white;

            // Subtle dark bottom gradient overlay so text always contrasts
            GameObject gradOverlay = CreateUIObject("BottomGradient", innerContainer.transform);
            RectTransform goRect = gradOverlay.GetComponent<RectTransform>();
            goRect.anchorMin = new Vector2(0f, 0f);
            goRect.anchorMax = new Vector2(1f, 0.65f);
            goRect.offsetMin = Vector2.zero;
            goRect.offsetMax = Vector2.zero;
            Image goImg = gradOverlay.AddComponent<Image>();
            goImg.color = new Color(0.04f, 0.06f, 0.12f, 0.82f);

            // Animated content container with CanvasGroup for crossfades
            GameObject contentObj = CreateUIObject("ContentLayer", innerContainer.transform);
            StretchFull(contentObj.GetComponent<RectTransform>());
            CanvasGroup contentCg = contentObj.AddComponent<CanvasGroup>();

            // Top Bar: 4 Pagination Dots
            GameObject dotsContainer = CreateUIObject("DotsContainer", contentObj.transform);
            RectTransform dcRect = dotsContainer.GetComponent<RectTransform>();
            dcRect.anchorMin = new Vector2(0f, 1f);
            dcRect.anchorMax = new Vector2(0f, 1f);
            dcRect.pivot = new Vector2(0f, 1f);
            dcRect.anchoredPosition = new Vector2(16f, -14f);
            dcRect.sizeDelta = new Vector2(120f, 20f);

            HorizontalLayoutGroup hlg = dotsContainer.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8f;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            List<Image> dotsList = new List<Image>();
            List<Button> dotButtons = new List<Button>();
            for (int i = 0; i < 4; i++)
            {
                GameObject dot = CreateUIObject($"Dot_{i}", dotsContainer.transform);
                RectTransform dotRect = dot.GetComponent<RectTransform>();
                dotRect.sizeDelta = new Vector2(9f, 9f);
                Image dotImg = dot.AddComponent<Image>();
                dotImg.color = (i == 0) ? Color.white : new Color(1f, 1f, 1f, 0.35f);
                Button dotBtn = dot.AddComponent<Button>();
                dotsList.Add(dotImg);
                dotButtons.Add(dotBtn);
            }

            // Top Right: "NEWS" Badge
            GameObject badgeObj = CreateUIObject("NewsBadge", contentObj.transform);
            RectTransform bRect = badgeObj.GetComponent<RectTransform>();
            bRect.anchorMin = new Vector2(1f, 1f);
            bRect.anchorMax = new Vector2(1f, 1f);
            bRect.pivot = new Vector2(1f, 1f);
            bRect.anchoredPosition = new Vector2(-14f, -12f);
            bRect.sizeDelta = new Vector2(65f, 24f);

            Image badgeImg = badgeObj.AddComponent<Image>();
            badgeImg.color = new Color(0.08f, 0.10f, 0.16f, 0.75f);

            GameObject badgeTextObj = CreateTextObject("Text", "NEWS", 11, FontStyle.Bold, new Color(0.85f, 0.90f, 1f), badgeObj.transform);
            StretchFull(badgeTextObj.GetComponent<RectTransform>());
            Text badgeText = badgeTextObj.GetComponent<Text>();

            // Bottom Area: Subtitle & Title ("THE BIG BANG")
            GameObject subTitleTextObj = CreateTextObject("SlideSubtitle", "A NEW BEGINNING • LIVE EVENT", 11, FontStyle.Bold, new Color(0.72f, 0.82f, 1f), contentObj.transform);
            RectTransform stRect = subTitleTextObj.GetComponent<RectTransform>();
            stRect.anchorMin = new Vector2(0f, 0f);
            stRect.anchorMax = new Vector2(1f, 0f);
            stRect.pivot = new Vector2(0f, 0f);
            stRect.anchoredPosition = new Vector2(18f, 48f);
            stRect.sizeDelta = new Vector2(-36f, 20f);
            Text slideSubText = subTitleTextObj.GetComponent<Text>();
            slideSubText.alignment = TextAnchor.MiddleLeft;

            GameObject titleTextObj = CreateTextObject("SlideTitle", "THE BIG BANG", 26, FontStyle.Bold, Color.white, contentObj.transform);
            RectTransform ttRect = titleTextObj.GetComponent<RectTransform>();
            ttRect.anchorMin = new Vector2(0f, 0f);
            ttRect.anchorMax = new Vector2(1f, 0f);
            ttRect.pivot = new Vector2(0f, 0f);
            ttRect.anchoredPosition = new Vector2(18f, 14f);
            ttRect.sizeDelta = new Vector2(-36f, 36f);
            Text slideTitleText = titleTextObj.GetComponent<Text>();
            slideTitleText.alignment = TextAnchor.MiddleLeft;

            // Subtle Left / Right Navigation Arrows
            GameObject btnPrevObj = CreateUIObject("BtnPrevSlide", newsCardObj.transform);
            RectTransform bpRect = btnPrevObj.GetComponent<RectTransform>();
            bpRect.anchorMin = new Vector2(0f, 0.5f);
            bpRect.anchorMax = new Vector2(0f, 0.5f);
            bpRect.anchoredPosition = new Vector2(12f, 0f);
            bpRect.sizeDelta = new Vector2(24f, 40f);
            Button btnPrev = btnPrevObj.AddComponent<Button>();
            Image bpImg = btnPrevObj.AddComponent<Image>();
            bpImg.color = new Color(0.08f, 0.10f, 0.16f, 0.55f);
            GameObject bpText = CreateTextObject("Arrow", "‹", 22, FontStyle.Bold, Color.white, btnPrevObj.transform);
            StretchFull(bpText.GetComponent<RectTransform>());

            GameObject btnNextObj = CreateUIObject("BtnNextSlide", newsCardObj.transform);
            RectTransform bnRect = btnNextObj.GetComponent<RectTransform>();
            bnRect.anchorMin = new Vector2(1f, 0.5f);
            bnRect.anchorMax = new Vector2(1f, 0.5f);
            bnRect.anchoredPosition = new Vector2(-12f, 0f);
            bnRect.sizeDelta = new Vector2(24f, 40f);
            Button btnNext = btnNextObj.AddComponent<Button>();
            Image bnImg = btnNextObj.AddComponent<Image>();
            bnImg.color = new Color(0.08f, 0.10f, 0.16f, 0.55f);
            GameObject bnText = CreateTextObject("Arrow", "›", 22, FontStyle.Bold, Color.white, btnNextObj.transform);
            StretchFull(bnText.GetComponent<RectTransform>());

            // Attach NewsCarousel component and wire serialized references
            NewsCarousel carousel = newsCardObj.AddComponent<NewsCarousel>();
            SerializedObject carouselSO = new SerializedObject(carousel);
            carouselSO.FindProperty("slideBackgroundImage").objectReferenceValue = slideBgImage;
            carouselSO.FindProperty("titleText").objectReferenceValue = slideTitleText;
            carouselSO.FindProperty("subtitleText").objectReferenceValue = slideSubText;
            carouselSO.FindProperty("badgeText").objectReferenceValue = badgeText;
            carouselSO.FindProperty("contentCanvasGroup").objectReferenceValue = contentCg;
            carouselSO.FindProperty("dotsContainer").objectReferenceValue = dotsContainer.transform;
            carouselSO.FindProperty("fetchRemoteOnStart").boolValue = true;
            carouselSO.FindProperty("remoteApiUrl").stringValue = "http://localhost:3000/api/carousel";

            SerializedProperty dotsProp = carouselSO.FindProperty("paginationDots");
            dotsProp.ClearArray();
            for (int i = 0; i < dotsList.Count; i++)
            {
                dotsProp.InsertArrayElementAtIndex(i);
                dotsProp.GetArrayElementAtIndex(i).objectReferenceValue = dotsList[i];
            }
            carouselSO.ApplyModifiedProperties();

            // Wire Prev / Next & Click
            UnityEventTools.AddPersistentListener(btnPrev.onClick, carousel.PreviousSlide);
            UnityEventTools.AddPersistentListener(btnNext.onClick, carousel.NextSlide);
            UnityEventTools.AddPersistentListener(ncButton.onClick, carousel.HandleSlideClicked);

            for (int i = 0; i < dotButtons.Count; i++)
            {
                int dotIndex = i;
                UnityEventTools.AddPersistentListener(dotButtons[i].onClick, () => carousel.GoToSlide(dotIndex));
            }

            // 6. Settings Panel
            GameObject settingsPanel = CreateSubCardPanel("SettingsPanel", "SETTINGS", canvasObj.transform);
            settingsPanel.SetActive(false);

            // Volume Slider in Settings
            GameObject volContainer = CreateUIObject("VolumeSetting", settingsPanel.transform);
            RectTransform volRect = volContainer.GetComponent<RectTransform>();
            volRect.anchorMin = new Vector2(0.5f, 0.6f);
            volRect.anchorMax = new Vector2(0.5f, 0.6f);
            volRect.sizeDelta = new Vector2(450, 60);

            GameObject volLabel = CreateTextObject("VolLabel", "Master Volume", 20, FontStyle.Normal, Color.white, volContainer.transform);
            RectTransform vlRect = volLabel.GetComponent<RectTransform>();
            vlRect.anchorMin = new Vector2(0f, 0.5f);
            vlRect.anchorMax = new Vector2(0.4f, 0.5f);
            vlRect.sizeDelta = new Vector2(180, 40);
            vlRect.anchoredPosition = new Vector2(90, 0);

            Slider volSlider = CreateSlider("Slider", volContainer.transform);
            RectTransform slRect = volSlider.GetComponent<RectTransform>();
            slRect.anchorMin = new Vector2(0.45f, 0.5f);
            slRect.anchorMax = new Vector2(1f, 0.5f);
            slRect.anchoredPosition = new Vector2(120, 0);
            slRect.sizeDelta = new Vector2(220, 30);

            // Fullscreen Toggle in Settings
            GameObject fsContainer = CreateUIObject("FullscreenSetting", settingsPanel.transform);
            RectTransform fsRect = fsContainer.GetComponent<RectTransform>();
            fsRect.anchorMin = new Vector2(0.5f, 0.42f);
            fsRect.anchorMax = new Vector2(0.5f, 0.42f);
            fsRect.sizeDelta = new Vector2(450, 50);

            GameObject fsLabel = CreateTextObject("FSLabel", "Fullscreen Mode", 20, FontStyle.Normal, Color.white, fsContainer.transform);
            RectTransform fslRect = fsLabel.GetComponent<RectTransform>();
            fslRect.anchorMin = new Vector2(0f, 0.5f);
            fslRect.anchorMax = new Vector2(0.5f, 0.5f);
            fslRect.sizeDelta = new Vector2(200, 40);
            fslRect.anchoredPosition = new Vector2(100, 0);

            Toggle fsToggle = CreateToggle("Toggle", fsContainer.transform);
            RectTransform fstRect = fsToggle.GetComponent<RectTransform>();
            fstRect.anchorMin = new Vector2(0.6f, 0.5f);
            fstRect.anchorMax = new Vector2(0.6f, 0.5f);
            fstRect.sizeDelta = new Vector2(30, 30);
            fstRect.anchoredPosition = Vector2.zero;

            Button btnBackSettings = CreateMenuButton("BtnBack", "BACK", new Color(0.25f, 0.3f, 0.4f), settingsPanel.transform);
            RectTransform bsRect = btnBackSettings.GetComponent<RectTransform>();
            bsRect.anchorMin = new Vector2(0.5f, 0.15f);
            bsRect.anchorMax = new Vector2(0.5f, 0.15f);
            bsRect.sizeDelta = new Vector2(240, 55);

            // 7. Credits Panel
            GameObject creditsPanel = CreateSubCardPanel("CreditsPanel", "ABOUT & CREDITS", canvasObj.transform);
            creditsPanel.SetActive(false);

            GameObject creditTextObj = CreateTextObject(
                "InfoText",
                "Billboard Tool for Unity\n\nDesigned for 2D & 3D dynamic camera-oriented billboarding,\nsprite baking, and UI elements.\n\nVersion 1.0.0",
                20,
                FontStyle.Normal,
                new Color(0.8f, 0.85f, 0.9f),
                creditsPanel.transform);
            RectTransform ctRect = creditTextObj.GetComponent<RectTransform>();
            ctRect.anchorMin = new Vector2(0.5f, 0.52f);
            ctRect.anchorMax = new Vector2(0.5f, 0.52f);
            ctRect.sizeDelta = new Vector2(500, 200);

            Button btnBackCredits = CreateMenuButton("BtnBackCredits", "BACK", new Color(0.25f, 0.3f, 0.4f), creditsPanel.transform);
            RectTransform bcCredRect = btnBackCredits.GetComponent<RectTransform>();
            bcCredRect.anchorMin = new Vector2(0.5f, 0.15f);
            bcCredRect.anchorMax = new Vector2(0.5f, 0.15f);
            bcCredRect.sizeDelta = new Vector2(240, 55);

            // 8. Serialized Fields Setup on MainMenuController
            SerializedObject so = new SerializedObject(controller);
            so.FindProperty("mainMenuPanel").objectReferenceValue = mainMenuPanel;
            so.FindProperty("settingsPanel").objectReferenceValue = settingsPanel;
            so.FindProperty("creditsPanel").objectReferenceValue = creditsPanel;
            so.FindProperty("toastPanel").objectReferenceValue = toastPanel;
            so.FindProperty("toastText").objectReferenceValue = toastText;
            so.FindProperty("newsCarousel").objectReferenceValue = carousel;
            so.FindProperty("volumeSlider").objectReferenceValue = volSlider;
            so.FindProperty("fullscreenToggle").objectReferenceValue = fsToggle;
            so.ApplyModifiedProperties();

            // 9. Wire Button Click Events
            UnityEventTools.AddPersistentListener(btnPlay.onClick, controller.PlayPuppet);
            UnityEventTools.AddPersistentListener(btnWhatever.onClick, controller.WhateverPuppet);
            UnityEventTools.AddPersistentListener(btnSandbox.onClick, controller.SandboxPuppet);
            UnityEventTools.AddPersistentListener(btnRandom.onClick, controller.RandomPuppet);
            UnityEventTools.AddPersistentListener(btnSettings.onClick, controller.ShowSettings);
            UnityEventTools.AddPersistentListener(btnCredits.onClick, controller.ShowCredits);
            UnityEventTools.AddPersistentListener(btnQuit.onClick, controller.QuitGame);
            UnityEventTools.AddPersistentListener(btnBackSettings.onClick, controller.ShowMainMenu);
            UnityEventTools.AddPersistentListener(btnBackCredits.onClick, controller.ShowMainMenu);

            // 10. Save Scene to disk
            string sceneDirectory = "Assets/Scenes";
            if (!Directory.Exists(sceneDirectory))
            {
                Directory.CreateDirectory(sceneDirectory);
            }

            string scenePath = Path.Combine(sceneDirectory, "MainMenu.unity").Replace("\\", "/");
            EditorSceneManager.SaveScene(scene, scenePath);
            EditorSceneManager.OpenScene(scenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Add to EditorBuildSettings
            EditorBuildSettingsScene[] originalScenes = EditorBuildSettings.scenes;
            bool alreadyInBuild = false;
            foreach (var s in originalScenes)
            {
                if (s.path == scenePath)
                {
                    alreadyInBuild = true;
                    break;
                }
            }

            if (!alreadyInBuild)
            {
                EditorBuildSettingsScene[] newScenes = new EditorBuildSettingsScene[originalScenes.Length + 1];
                newScenes[0] = new EditorBuildSettingsScene(scenePath, true);
                for (int i = 0; i < originalScenes.Length; i++)
                {
                    newScenes[i + 1] = originalScenes[i];
                }
                EditorBuildSettings.scenes = newScenes;
            }

            Debug.Log($"[MainMenuSceneCreator] Successfully generated 2D Main Menu at {scenePath}");
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static GameObject CreateTextObject(string name, string text, int fontSize, FontStyle style, Color color, Transform parent)
        {
            GameObject obj = CreateUIObject(name, parent);
            Text t = obj.AddComponent<Text>();
            t.text = text;
            t.fontSize = fontSize;
            t.fontStyle = style;
            t.color = color;
            t.alignment = TextAnchor.MiddleCenter;
            t.font = defaultFont;
            return obj;
        }

        private static Button CreateMenuButton(string name, string label, Color baseColor, Transform parent)
        {
            GameObject btnObj = CreateUIObject(name, parent);
            RectTransform rect = btnObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(340, 60);

            Image img = btnObj.AddComponent<Image>();
            img.color = baseColor;

            Button btn = btnObj.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = baseColor;
            cb.highlightedColor = baseColor * 1.25f;
            cb.pressedColor = baseColor * 0.8f;
            cb.selectedColor = cb.highlightedColor;
            btnObj.AddComponent<UIButtonAnimator>();

            LayoutElement le = btnObj.AddComponent<LayoutElement>();
            le.preferredHeight = 54f;
            le.minHeight = 48f;

            GameObject textObj = CreateTextObject("Text", label, 20, FontStyle.Bold, Color.white, btnObj.transform);
            StretchFull(textObj.GetComponent<RectTransform>());

            return btn;
        }

        private static GameObject CreateSubCardPanel(string name, string title, Transform parent)
        {
            GameObject panel = CreateUIObject(name, parent);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(650, 520);
            rect.anchoredPosition = Vector2.zero;

            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.16f, 0.22f, 0.98f);

            GameObject titleObj = CreateTextObject("Title", title, 36, FontStyle.Bold, Color.white, panel.transform);
            RectTransform tRect = titleObj.GetComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0.5f, 1f);
            tRect.anchorMax = new Vector2(0.5f, 1f);
            tRect.anchoredPosition = new Vector2(0, -50);
            tRect.sizeDelta = new Vector2(500, 60);

            return panel;
        }

        private static Slider CreateSlider(string name, Transform parent, Color? fillColor = null)
        {
            GameObject sliderObj = CreateUIObject(name, parent);
            Slider slider = sliderObj.AddComponent<Slider>();

            // Background
            GameObject bg = CreateUIObject("Background", sliderObj.transform);
            StretchFull(bg.GetComponent<RectTransform>());
            Image bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.18f, 0.22f, 0.30f);

            // Fill Area
            GameObject fillArea = CreateUIObject("Fill Area", sliderObj.transform);
            RectTransform faRect = fillArea.GetComponent<RectTransform>();
            faRect.anchorMin = new Vector2(0f, 0.2f);
            faRect.anchorMax = new Vector2(1f, 0.8f);
            faRect.offsetMin = new Vector2(6, 0);
            faRect.offsetMax = new Vector2(-6, 0);

            GameObject fill = CreateUIObject("Fill", fillArea.transform);
            RectTransform fRect = fill.GetComponent<RectTransform>();
            fRect.sizeDelta = Vector2.zero;
            Image fillImg = fill.AddComponent<Image>();
            fillImg.color = fillColor ?? new Color(0.28f, 0.65f, 0.85f);
            slider.fillRect = fRect;

            // Handle Slide Area
            GameObject handleSlideArea = CreateUIObject("Handle Slide Area", sliderObj.transform);
            RectTransform hsaRect = handleSlideArea.GetComponent<RectTransform>();
            hsaRect.anchorMin = Vector2.zero;
            hsaRect.anchorMax = Vector2.one;
            hsaRect.offsetMin = new Vector2(10, 0);
            hsaRect.offsetMax = new Vector2(-10, 0);

            GameObject handle = CreateUIObject("Handle", handleSlideArea.transform);
            RectTransform hRect = handle.GetComponent<RectTransform>();
            hRect.sizeDelta = new Vector2(16, 16);
            Image handleImg = handle.AddComponent<Image>();
            handleImg.color = new Color(0.92f, 0.96f, 1f);
            slider.handleRect = hRect;
            slider.targetGraphic = handleImg;

            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;

            return slider;
        }

        private static Toggle CreateToggle(string name, Transform parent)
        {
            GameObject toggleObj = CreateUIObject(name, parent);
            Toggle toggle = toggleObj.AddComponent<Toggle>();

            // Background box
            GameObject bg = CreateUIObject("Background", toggleObj.transform);
            StretchFull(bg.GetComponent<RectTransform>());
            Image bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.2f, 0.25f, 0.32f);

            // Checkmark
            GameObject check = CreateUIObject("Checkmark", bg.transform);
            RectTransform cRect = check.GetComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0.2f, 0.2f);
            cRect.anchorMax = new Vector2(0.8f, 0.8f);
            cRect.offsetMin = Vector2.zero;
            cRect.offsetMax = Vector2.zero;
            Image checkImg = check.AddComponent<Image>();
            checkImg.color = new Color(0.35f, 0.85f, 0.55f);

            toggle.graphic = checkImg;
            toggle.targetGraphic = bgImg;
            toggle.isOn = true;

            return toggle;
        }
    }
}
