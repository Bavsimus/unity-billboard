using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BillboardTool.UI
{
    /// <summary>
    /// Handles 2D Main Menu UI interactions, screen navigation, puppet/mock actions, and application state.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject creditsPanel;

        [Header("Toast / Puppet Feedback")]
        [SerializeField] private GameObject toastPanel;
        [SerializeField] private Text toastText;

        [Header("Settings Controls")]
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private Toggle fullscreenToggle;

        [Header("Zero Billboard (Fortnite The Big Bang Style)")]
        [UnityEngine.Serialization.FormerlySerializedAs("newsCarousel")]
        [SerializeField] private ZeroBillboard zeroBillboard;

        public ZeroBillboard ZeroBillboard => zeroBillboard;

        [Header("Scene Configuration")]
        [SerializeField] private string targetPlaySceneName = "";

        private Coroutine toastCoroutine;

        private static readonly string[] RandomPuppetQuotes = new[]
        {
            "Puppet: 'Whatever you say, Boss!'",
            "Puppet: 'Rotating billboard towards camera... Done!'",
            "Puppet: 'Mock action executed smoothly!'",
            "Puppet: 'Simulating 1,000 billboard sprites!'",
            "Puppet: 'Whatever button clicked! High five!'"
        };

        private void Start()
        {
            ShowMainMenu();

            if (toastPanel != null)
            {
                toastPanel.SetActive(false);
            }

            if (volumeSlider != null)
            {
                volumeSlider.value = AudioListener.volume;
                volumeSlider.onValueChanged.AddListener(SetVolume);
            }

            if (fullscreenToggle != null)
            {
                fullscreenToggle.isOn = Screen.fullScreen;
                fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
            }

            if (zeroBillboard != null)
            {
                zeroBillboard.OnSlideClicked += OnBillboardSlideClicked;
            }
        }

        public void OnBillboardSlideClicked(ZeroBillboard.BillboardSlide slide)
        {
            if (slide == null) return;
            ShowToast($"🚀 News Event: '{slide.title}' ({slide.badge})");
        }

        public void OnNewsSlideClicked(ZeroBillboard.BillboardSlide slide)
        {
            OnBillboardSlideClicked(slide);
        }

        // --- Puppet / Dummy Actions ---

        public void PlayPuppet()
        {
            Debug.Log("[MainMenu] Puppet 'Play' clicked!");
            if (!string.IsNullOrEmpty(targetPlaySceneName))
            {
                SceneManager.LoadScene(targetPlaySceneName);
            }
            else
            {
                ShowToast("▶ Play: Starting mock play session... (Assign a target scene in inspector to load a real scene)");
            }
        }

        public void WhateverPuppet()
        {
            Debug.Log("[MainMenu] Puppet 'Whatever' clicked!");
            ShowToast("🤷 Whatever: Clicked! Dummy action completed successfully.");
        }

        public void SandboxPuppet()
        {
            Debug.Log("[MainMenu] Puppet 'Sandbox' clicked!");
            ShowToast("🎪 Puppet Sandbox: Initialized mock 2D billboard playground.");
        }

        public void RandomPuppet()
        {
            string randomMsg = RandomPuppetQuotes[Random.Range(0, RandomPuppetQuotes.Length)];
            Debug.Log($"[MainMenu] Puppet Random Action: {randomMsg}");
            ShowToast(randomMsg);
        }

        public void PuppetAction(string actionName)
        {
            Debug.Log($"[MainMenu] Puppet Action: {actionName}");
            ShowToast($"✨ Puppet: [{actionName}] triggered!");
        }

        // --- Toast Banner System ---

        public void ShowToast(string message)
        {
            if (toastPanel == null || toastText == null)
            {
                Debug.Log($"[Toast] {message}");
                return;
            }

            if (toastCoroutine != null)
            {
                StopCoroutine(toastCoroutine);
            }

            toastCoroutine = StartCoroutine(ToastRoutine(message));
        }

        private IEnumerator ToastRoutine(string message)
        {
            toastText.text = message;
            toastPanel.SetActive(true);

            CanvasGroup cg = toastPanel.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 0f;
                // Fade in
                for (float t = 0; t < 0.2f; t += Time.unscaledDeltaTime)
                {
                    cg.alpha = t / 0.2f;
                    yield return null;
                }
                cg.alpha = 1f;

                // Hold
                yield return new WaitForSecondsRealtime(2.5f);

                // Fade out
                for (float t = 0; t < 0.3f; t += Time.unscaledDeltaTime)
                {
                    cg.alpha = 1f - (t / 0.3f);
                    yield return null;
                }
                cg.alpha = 0f;
            }
            else
            {
                yield return new WaitForSecondsRealtime(2.5f);
            }

            toastPanel.SetActive(false);
            toastCoroutine = null;
        }

        // --- Panel Navigation ---

        public void ShowMainMenu()
        {
            SetPanelActive(mainMenuPanel, true);
            SetPanelActive(settingsPanel, false);
            SetPanelActive(creditsPanel, false);
        }

        public void ShowSettings()
        {
            SetPanelActive(mainMenuPanel, false);
            SetPanelActive(settingsPanel, true);
            SetPanelActive(creditsPanel, false);
        }

        public void ShowCredits()
        {
            SetPanelActive(mainMenuPanel, false);
            SetPanelActive(settingsPanel, false);
            SetPanelActive(creditsPanel, true);
        }

        public void SetVolume(float volume)
        {
            AudioListener.volume = Mathf.Clamp01(volume);
        }

        public void SetFullscreen(bool isFullscreen)
        {
            Screen.fullScreen = isFullscreen;
        }

        public void QuitGame()
        {
            Debug.Log("[MainMenu] Quitting Application...");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null)
            {
                panel.SetActive(active);
            }
        }
    }
}
