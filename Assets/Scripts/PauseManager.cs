using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance;

    [Header("UI")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject pauseSettingsPanel;
    [SerializeField] private float fadeDuration = 0.3f;

    [Header("Buttons")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button menuButton;
    [SerializeField] private Button closeSettingsButton; // New button for closing settings

    [Header("Scenes")]
    [SerializeField] private string mainMenuSceneName = "UIMenu";
    [SerializeField] private bool disablePauseInMainMenu = true;

    [Header("Disable While Paused (Player / Camera Scripts)")]
    [SerializeField] private List<Behaviour> disableWhilePaused = new List<Behaviour>();

    [Header("Cursor")]
    [SerializeField] private bool showCursorOnPause = true;
    [SerializeField] private bool lockCursorOnResume = true;

    private bool isPaused;
    private bool canPause = true;
    private CanvasGroup pausePanelCanvasGroup;
    private CanvasGroup settingsPanelCanvasGroup;

    private void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Auto-find Settings Panel if missing
        if (pauseSettingsPanel == null)
        {
            // Try to find it in the scene (active or inactive)
            // Note: Resources.FindObjectsOfTypeAll is heavy, usually better to rely on direct finding if possible
            // But if it's a child of the Canvas...
            if (pausePanel != null && pausePanel.transform.parent != null)
            {
                 Transform canvasTransform = pausePanel.transform.parent;
                 Transform found = canvasTransform.Find("SettingsPanel");
                 if (found == null) found = canvasTransform.Find("Panel_Settings");
                 if (found != null) pauseSettingsPanel = found.gameObject;
            }
        }

        // Setup CanvasGroups
        SetupCanvasGroup(pausePanel, ref pausePanelCanvasGroup);
        SetupCanvasGroup(pauseSettingsPanel, ref settingsPanelCanvasGroup);

        // Initial state
        if (pausePanel != null) 
        {
            pausePanel.SetActive(false);
            if (pausePanelCanvasGroup != null) pausePanelCanvasGroup.alpha = 0f;
        }
        if (pauseSettingsPanel != null) 
        {
            pauseSettingsPanel.SetActive(false);
            if (settingsPanelCanvasGroup != null) settingsPanelCanvasGroup.alpha = 0f;
        }

        // Auto-find and setup buttons
        if (pausePanel != null)
        {
            if (resumeButton == null) resumeButton = pausePanel.transform.Find("Btn_Continue")?.GetComponent<Button>();
            if (settingsButton == null) settingsButton = pausePanel.transform.Find("Btn_Settings")?.GetComponent<Button>();
            if (menuButton == null) menuButton = pausePanel.transform.Find("Btn_BackToMenu")?.GetComponent<Button>();
        }
        
        // Auto-find settings close button if possible
        if (pauseSettingsPanel != null && closeSettingsButton == null)
        {
             // Try to find a back button in settings panel
             closeSettingsButton = pauseSettingsPanel.transform.Find("Btn_Back")?.GetComponent<Button>();
             if (closeSettingsButton == null) closeSettingsButton = pauseSettingsPanel.transform.Find("Img_Back")?.GetComponent<Button>();
             if (closeSettingsButton == null) closeSettingsButton = pauseSettingsPanel.transform.Find("Back")?.GetComponent<Button>();
             // If still null, maybe deep search? For now, direct children are safest.
        }

        if (resumeButton != null) 
        {
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(Resume);
        }
        if (settingsButton != null) 
        {
            settingsButton.onClick.RemoveAllListeners();
            settingsButton.onClick.AddListener(OpenSettings);
        }
        if (menuButton != null) 
        {
            menuButton.onClick.RemoveAllListeners();
            menuButton.onClick.AddListener(BackToMenu);
        }
        if (closeSettingsButton != null)
        {
            closeSettingsButton.onClick.RemoveAllListeners();
            closeSettingsButton.onClick.AddListener(CloseSettings);
        }

        // Check current scene immediately in case we started in Loading Screen
        CheckSceneValidity(SceneManager.GetActiveScene());

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void SetupCanvasGroup(GameObject panel, ref CanvasGroup cg)
    {
        if (panel != null)
        {
            cg = panel.GetComponent<CanvasGroup>();
            if (cg == null) cg = panel.AddComponent<CanvasGroup>();
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"PauseManager: Scene loaded: {scene.name}");
        CheckSceneValidity(scene);
        SetupSceneRequirements();
        RewireButtons(); // Re-wire buttons on every scene load
    }

    private void RewireButtons()
    {
        Debug.Log("PauseManager: Rewiring buttons...");
        
        // Re-find buttons if needed
        if (pausePanel != null)
        {
            if (resumeButton == null) 
            {
                resumeButton = pausePanel.transform.Find("Btn_Continue")?.GetComponent<Button>();
            }
            if (settingsButton == null) 
            {
                settingsButton = pausePanel.transform.Find("Btn_Settings")?.GetComponent<Button>();
            }
            if (menuButton == null) 
            {
                menuButton = pausePanel.transform.Find("Btn_BackToMenu")?.GetComponent<Button>();
            }
        }
        
        if (pauseSettingsPanel != null && closeSettingsButton == null)
        {
            closeSettingsButton = pauseSettingsPanel.transform.Find("Btn_Back")?.GetComponent<Button>();
            if (closeSettingsButton == null) closeSettingsButton = pauseSettingsPanel.transform.Find("Img_Back")?.GetComponent<Button>();
            if (closeSettingsButton == null) closeSettingsButton = pauseSettingsPanel.transform.Find("Back")?.GetComponent<Button>();
        }

        // ALWAYS re-wire listeners (remove old ones first)
        if (resumeButton != null) 
        {
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(Resume);
            Debug.Log($"PauseManager: Wired Resume button: {resumeButton.name}, interactable={resumeButton.interactable}");
        }
        else
        {
            Debug.LogWarning("PauseManager: Resume button is NULL!");
        }
        
        if (settingsButton != null) 
        {
            settingsButton.onClick.RemoveAllListeners();
            settingsButton.onClick.AddListener(OpenSettings);
            Debug.Log($"PauseManager: Wired Settings button: {settingsButton.name}");
        }
        if (menuButton != null) 
        {
            menuButton.onClick.RemoveAllListeners();
            menuButton.onClick.AddListener(BackToMenu);
            Debug.Log($"PauseManager: Wired Menu button: {menuButton.name}");
        }
        if (closeSettingsButton != null)
        {
            closeSettingsButton.onClick.RemoveAllListeners();
            closeSettingsButton.onClick.AddListener(CloseSettings);
            Debug.Log($"PauseManager: Wired CloseSettings button: {closeSettingsButton.name}");
        }

        // Ensure CanvasGroups are set up correctly
        if (pausePanelCanvasGroup != null)
        {
            pausePanelCanvasGroup.interactable = true;
            pausePanelCanvasGroup.blocksRaycasts = true;
            Debug.Log($"PauseManager: PausePanelCanvasGroup interactable={pausePanelCanvasGroup.interactable}, blocksRaycasts={pausePanelCanvasGroup.blocksRaycasts}");
        }
    }

    private void SetupSceneRequirements()
    {
        // 1. Setup Canvas (Sorting Order & Render Mode & GraphicRaycaster)
        // ÖNCELİK: pausePanel'in bağlı olduğu Canvas'ı kullan
        Canvas canvas = null;
        
        if (pausePanel != null)
        {
            canvas = pausePanel.GetComponentInParent<Canvas>();
        }
        
        // Fallback: PauseManager'ın kendi Canvas'ına bak
        if (canvas == null)
        {
            canvas = GetComponent<Canvas>();
            if (canvas == null) canvas = GetComponentInParent<Canvas>();
        }

        if (canvas != null)
        {
            // FORCE OVERLAY MODE for maximum reliability across scene loads
            // This removes dependency on Camera references
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 29999; // BlinkTransition (999) üstünde olmalı
            
            Debug.Log($"PauseManager: Canvas '{canvas.name}' set to Overlay mode with sortingOrder {canvas.sortingOrder}");
            
            // Ensure GraphicRaycaster exists
            GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster == null)
            {
                 raycaster = canvas.gameObject.AddComponent<GraphicRaycaster>();
            }
            raycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None;
        }

        // 2. Manage EventSystems (Aggressive Cleanup)
        EventSystem[] systems = FindObjectsOfType<EventSystem>();
        
        if (systems.Length > 1)
        {
            Debug.LogWarning($"PauseManager: Found {systems.Length} EventSystems. Attempting to clean up duplicates.");
            
            // Preference: Prefer the SCENE'S EventSystem over the GlobalUI one.
            // Why? The scene might have specific input configurations (InputSystemUIModule vs StandaloneInputModule).
            
            EventSystem sceneSystem = null;
            EventSystem globalSystem = null;
            EventSystem bestSystem = null;
            
            foreach (var es in systems)
            {
                if (es.transform.root == this.transform.root)
                {
                    globalSystem = es;
                }
                else
                {
                    sceneSystem = es;
                }
            }

            if (sceneSystem != null)
            {
                bestSystem = sceneSystem;
                Debug.Log($"PauseManager: Preferring Scene EventSystem: {sceneSystem.name}");
            }
            else
            {
                bestSystem = globalSystem;
                Debug.Log($"PauseManager: Using Global EventSystem (No scene specific system found).");
            }
            
            // If still null, just pick the first
            if (bestSystem == null && systems.Length > 0) bestSystem = systems[0];
            
            // Destroy/Disable others
            foreach (var es in systems)
            {
                if (es != bestSystem)
                {
                    Debug.Log($"PauseManager: Disabling duplicate EventSystem '{es.name}' on '{es.gameObject.name}'");
                    es.gameObject.SetActive(false); // Just disable, destroying might be too destructive for Singletons
                }
            }
        }
        else if (systems.Length == 0)
        {
            // Create if missing
            GameObject eventSystemGO = new GameObject("EventSystem_AutoCreated");
            eventSystemGO.AddComponent<EventSystem>();
            eventSystemGO.AddComponent<StandaloneInputModule>();
            Debug.Log("PauseManager: Auto-created EventSystem for scene.");
        }
        else
        {
             Debug.Log($"PauseManager: Single EventSystem confirmed: {systems[0].name}");
        }
    }

    private void CheckSceneValidity(Scene scene)
    {
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null && !disableWhilePaused.Contains(player))
        {
            disableWhilePaused.Add(player);
        }

        // Disable pause in Main Menu OR Loading Screen
        if ((disablePauseInMainMenu && scene.name == mainMenuSceneName) || scene.name == "LoadingScreen")
        {
            canPause = false;
            ForceResume();

            if (pausePanel != null) pausePanel.SetActive(false);
            if (pauseSettingsPanel != null) pauseSettingsPanel.SetActive(false);
            return;
        }

        canPause = true;

        // Clean nulls
        disableWhilePaused.RemoveAll(x => x == null);
        
        // Ensure buttons are ready if pause happens
        if (pausePanel != null && resumeButton == null) 
        {
             // Try fetching again in case they were lost
             resumeButton = pausePanel.transform.Find("Btn_Continue")?.GetComponent<Button>();
             // ... wire up logic again if needed, or rely on Awake wiring persisting (it should)
        }
    }

    private void Update()
    {
        if (!canPause) return;

        // Force Cursor state if paused (Fight against other scripts locking it)
        if (isPaused && showCursorOnPause)
        {
            if (Cursor.lockState != CursorLockMode.None || !Cursor.visible)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
        }

        // ESC / Gamepad Start
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton7))
        {
            if (!isPaused) Pause();
            else 
            {
                // If settings is open, close settings and go back to pause menu? 
                // Or just Resume game? Usually ESC in settings goes back to previous menu.
                if (pauseSettingsPanel != null && pauseSettingsPanel.activeSelf)
                {
                    CloseSettings();
                }
                else
                {
                    Resume();
                }
            }
        }
    }

    public void RegisterForPause(params Behaviour[] behaviours)
    {
        if (behaviours == null) return;

        foreach (var b in behaviours)
        {
            if (b == null) continue;
            if (!disableWhilePaused.Contains(b))
                disableWhilePaused.Add(b);
        }

        if (isPaused)
        {
            foreach (var b in behaviours)
                if (b != null) b.enabled = false;
        }
    }

    public void Pause()
    {
        Debug.Log("=== PauseManager: Pause() START ===");
        if (isPaused) return;
        isPaused = true;

        Time.timeScale = 0f;

        for (int i = 0; i < disableWhilePaused.Count; i++)
        {
            if (disableWhilePaused[i] != null)
                disableWhilePaused[i].enabled = false;
        }

        if (showCursorOnPause)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        // Close settings if open, open pause panel
        if (pauseSettingsPanel != null) pauseSettingsPanel.SetActive(false);
        
        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
            StartCoroutine(FadeCanvasGroup(pausePanelCanvasGroup, 0f, 1f));
            
            // DEBUG LOGS
            Debug.Log($"[DEBUG] pausePanel active: {pausePanel.activeSelf}");
            Debug.Log($"[DEBUG] pausePanelCanvasGroup null: {pausePanelCanvasGroup == null}");
            if (pausePanelCanvasGroup != null)
            {
                Debug.Log($"[DEBUG] CanvasGroup - alpha: {pausePanelCanvasGroup.alpha}, interactable: {pausePanelCanvasGroup.interactable}, blocksRaycasts: {pausePanelCanvasGroup.blocksRaycasts}");
            }
            
            // Canvas bilgisi
            Canvas canvas = pausePanel.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                Debug.Log($"[DEBUG] Canvas found: {canvas.name}, renderMode: {canvas.renderMode}, sortingOrder: {canvas.sortingOrder}");
                GraphicRaycaster gr = canvas.GetComponent<GraphicRaycaster>();
                Debug.Log($"[DEBUG] GraphicRaycaster on Canvas: {(gr != null ? "YES" : "NO")}");
            }
            else
            {
                Debug.LogError("[DEBUG] NO CANVAS FOUND for pausePanel!");
            }
            
            // EventSystem bilgisi
            var eventSystem = UnityEngine.EventSystems.EventSystem.current;
            Debug.Log($"[DEBUG] Current EventSystem: {(eventSystem != null ? eventSystem.name : "NULL")}");
            
            // Button durumları
            Debug.Log($"[DEBUG] resumeButton null: {resumeButton == null}");
            if (resumeButton != null)
            {
                Debug.Log($"[DEBUG] resumeButton.interactable: {resumeButton.interactable}, gameObject.active: {resumeButton.gameObject.activeInHierarchy}");
            }
        }
        else
        {
            Debug.LogError("[DEBUG] pausePanel is NULL!");
        }
        Debug.Log("=== PauseManager: Pause() END ===");
    }

    public void Resume()
    {
        Debug.Log("PauseManager: Resume called");
        if (!isPaused) return;
        
        // Start resuming process
        StartCoroutine(ResumeRoutine());
    }

    private IEnumerator ResumeRoutine()
    {
        // Fade out panels
        if (pauseSettingsPanel != null && pauseSettingsPanel.activeSelf)
        {
            yield return StartCoroutine(FadeCanvasGroup(settingsPanelCanvasGroup, 1f, 0f));
            pauseSettingsPanel.SetActive(false);
        }
        else if (pausePanel != null && pausePanel.activeSelf)
        {
            yield return StartCoroutine(FadeCanvasGroup(pausePanelCanvasGroup, 1f, 0f));
            pausePanel.SetActive(false);
        }

        isPaused = false;
        Time.timeScale = 1f;

        if (lockCursorOnResume)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        // Enable scripts
        for (int i = 0; i < disableWhilePaused.Count; i++)
        {
            if (disableWhilePaused[i] != null)
                disableWhilePaused[i].enabled = true;
        }
    }

    public void OpenSettings()
    {
        Debug.Log("PauseManager: OpenSettings called");
        
        // Ensure game is paused if opened from outside (rare but possible)
        if (!isPaused) Pause(); // This sets timeScale 0 etc.

        // Bind audio sliders when settings opens
        BindAudioSliders();

        StartCoroutine(SwitchToSettingsRoutine());
    }

    private void BindAudioSliders()
    {
        if (pauseSettingsPanel == null) return;

        // Find sliders in settings panel
        var sliders = pauseSettingsPanel.GetComponentsInChildren<Slider>(true);
        
        float musicVol = PlayerPrefs.GetFloat("VOL_MUSIC", 1f);
        float sfxVol = PlayerPrefs.GetFloat("VOL_SFX", 1f);

        foreach (var slider in sliders)
        {
            if (slider.name == "MusicSlider")
            {
                slider.SetValueWithoutNotify(musicVol);
                slider.onValueChanged.RemoveAllListeners();
                slider.onValueChanged.AddListener((v) => {
                    v = Mathf.Clamp01(v);
                    PlayerPrefs.SetFloat("VOL_MUSIC", v);
                    PlayerPrefs.Save();
                    if (BgMusicPersistent.Instance != null)
                        BgMusicPersistent.Instance.SetVolume(v);
                });
            }
            else if (slider.name == "SFXSlider")
            {
                slider.SetValueWithoutNotify(sfxVol);
                slider.onValueChanged.RemoveAllListeners();
                slider.onValueChanged.AddListener((v) => {
                    v = Mathf.Clamp01(v);
                    PlayerPrefs.SetFloat("VOL_SFX", v);
                    PlayerPrefs.Save();
                    // Apply to all non-music audio sources
                    ApplySfxVolumeToAll(v);
                });
            }
        }
    }

    private void ApplySfxVolumeToAll(float v)
    {
        var allSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
        AudioSource musicSource = BgMusicPersistent.Instance?.GetComponent<AudioSource>();

        foreach (var src in allSources)
        {
            if (src == null) continue;
            if (musicSource != null && src == musicSource) continue;
            src.volume = v;
        }
    }

    private IEnumerator SwitchToSettingsRoutine()
    {
        // Fade out pause panel
        if (pausePanel != null && pausePanel.activeSelf)
        {
            yield return StartCoroutine(FadeCanvasGroup(pausePanelCanvasGroup, 1f, 0f));
            pausePanel.SetActive(false);
        }

        // Fade in settings panel
        if (pauseSettingsPanel != null)
        {
            pauseSettingsPanel.SetActive(true);
            yield return StartCoroutine(FadeCanvasGroup(settingsPanelCanvasGroup, 0f, 1f));
        }
    }

    public void CloseSettings()
    {
        Debug.Log("PauseManager: CloseSettings called");
        StartCoroutine(SwitchToPauseMenuRoutine());
    }

    private IEnumerator SwitchToPauseMenuRoutine()
    {
         // Fade out settings panel
        if (pauseSettingsPanel != null && pauseSettingsPanel.activeSelf)
        {
            yield return StartCoroutine(FadeCanvasGroup(settingsPanelCanvasGroup, 1f, 0f));
            pauseSettingsPanel.SetActive(false);
        }

        // Fade in pause panel
        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
            yield return StartCoroutine(FadeCanvasGroup(pausePanelCanvasGroup, 0f, 1f));
        }
    }

    public void BackToMenu()
    {
        Debug.Log("PauseManager: BackToMenu called");
        ForceResume();
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void ForceResume()
    {
        StopAllCoroutines(); // Stop any active fades
        isPaused = false;
        Time.timeScale = 1f;

        for (int i = 0; i < disableWhilePaused.Count; i++)
        {
            if (disableWhilePaused[i] != null)
                disableWhilePaused[i].enabled = true;
        }

        if (pausePanel != null) pausePanel.SetActive(false);
        if (pauseSettingsPanel != null) pauseSettingsPanel.SetActive(false);
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float startAlpha, float endAlpha)
    {
        if (cg == null) yield break;

        // Determine if we are fading in or out
        bool fadingIn = endAlpha > startAlpha;

        // If fading in, enable interaction immediately (or you can wait till end, but typically user wants to click fast)
        // Actually for smoothness, let's enable blocksRaycasts ONLY if alpha > 0.
        // Usually, set blocksRaycasts = true at start if fading in, false at end if fading out.
        
        if (fadingIn)
        {
            cg.blocksRaycasts = true;
            cg.interactable = true;
            cg.alpha = startAlpha;
        }
        else
        {
            cg.blocksRaycasts = false; // Block immediately on fade out to prevent double clicks
            cg.interactable = false;
        }

        float elapsed = 0f;
        cg.alpha = startAlpha;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / fadeDuration);
            yield return null;
        }

        cg.alpha = endAlpha;

        // Ensure final state
        if (!fadingIn && endAlpha <= 0.01f)
        {
            cg.blocksRaycasts = false;
            cg.interactable = false;
        }
        else
        {
            cg.blocksRaycasts = true;
            cg.interactable = true;
        }
    }
}

