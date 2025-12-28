using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance;

    [Header("UI")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject pauseSettingsPanel;

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

        if (pausePanel != null) pausePanel.SetActive(false);
        if (pauseSettingsPanel != null) pauseSettingsPanel.SetActive(false);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null && !disableWhilePaused.Contains(player))
        {
            disableWhilePaused.Add(player);
        }

        // Menüde pause kapalı
        if (disablePauseInMainMenu && scene.name == mainMenuSceneName)
        {
            canPause = false;
            ForceResume(); // <- HATA VEREN ForceResume artık var

            if (pausePanel != null) pausePanel.SetActive(false);
            if (pauseSettingsPanel != null) pauseSettingsPanel.SetActive(false);
            return;
        }

        canPause = true;

        // Sahne değişince listede null kalanları temizle
        disableWhilePaused.RemoveAll(x => x == null);
    }

    private void Update()
    {
        if (!canPause) return;

        // ESC / Gamepad Start
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton7))
        {
            if (!isPaused) Pause();
            else Resume();
        }
    }

    // PauseRegister.cs'nin çağırdığı metod (HATA 3 buradan çözülüyor)
    public void RegisterForPause(params Behaviour[] behaviours)
    {
        if (behaviours == null) return;

        foreach (var b in behaviours)
        {
            if (b == null) continue;
            if (!disableWhilePaused.Contains(b))
                disableWhilePaused.Add(b);
        }

        // Eğer şu an pausedaysak eklenenleri anında kapat
        if (isPaused)
        {
            foreach (var b in behaviours)
                if (b != null) b.enabled = false;
        }
    }

    public void Pause()
    {
        if (isPaused) return;
        isPaused = true;

        Time.timeScale = 0f;

        // PlayerController dahil tüm scriptleri kapat
        for (int i = 0; i < disableWhilePaused.Count; i++)
        {
            if (disableWhilePaused[i] != null)
                disableWhilePaused[i].enabled = false;
        }

        if (pausePanel != null) pausePanel.SetActive(true);
        if (pauseSettingsPanel != null) pauseSettingsPanel.SetActive(false);

        if (showCursorOnPause)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    public void Resume()
    {
        if (!isPaused) return;
        isPaused = false;

        Time.timeScale = 1f;

        // !!! EKRANINDAKİ VİRGÜL HATASI BURADA OLUYOR !!!
        // ; olacak, virgül değil.
        if (pausePanel != null) pausePanel.SetActive(false);
        if (pauseSettingsPanel != null) pauseSettingsPanel.SetActive(false);

        if (lockCursorOnResume)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        // PlayerController içindeki ESC ToggleCursor çakışmasın diye 1 frame sonra aç
        StartCoroutine(EnableScriptsNextFrame());
    }

    private IEnumerator EnableScriptsNextFrame()
    {
        yield return null;

        for (int i = 0; i < disableWhilePaused.Count; i++)
        {
            if (disableWhilePaused[i] != null)
                disableWhilePaused[i].enabled = true;
        }
    }

    public void OpenSettings()
    {
        if (!isPaused) Pause();
        if (pauseSettingsPanel != null) pauseSettingsPanel.SetActive(true);
    }

    public void BackToMenu()
    {
        ForceResume();
        SceneManager.LoadScene(mainMenuSceneName);
    }

    // HATA 1'i çözen fonksiyon (ForceResume yoktu)
    private void ForceResume()
    {
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
}
