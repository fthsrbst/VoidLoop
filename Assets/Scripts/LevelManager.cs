using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Anomaly oyunu yöneticisi.
/// Normal sahne her zaman aynı, anomali sahneleri rastgele (tekrarsız) açılır.
/// </summary>
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Sahne Ayarları")]
    [Tooltip("Normal sahne (anomali yok)")]
    [SerializeField] private string normalSceneName = "Level_0_Tutorial";
    
    [Tooltip("Anomali sahneleri listesi")]
    [SerializeField] private string[] anomalySceneNames;
    
    [Tooltip("Final/Kazanma sahnesi")]
    [SerializeField] private string finalSceneName = "FinalMap";
    
    [Header("Oyun Ayarları")]
    [Tooltip("Anomali çıkma olasılığı (0-1). Optimal: 0.35-0.45")]
    [Range(0f, 1f)]
    [SerializeField] private float anomalyChance = 0.4f;
    
    [Tooltip("Kaç doğru seçim ile kazanılır")]
    [SerializeField] private int winCondition = 12;
    
    [Tooltip("Üst üste maksimum normal sahne sayısı")]
    [SerializeField] private int maxConsecutiveNormal = 2;
    
    [Tooltip("Üst üste maksimum anomali sahne sayısı")]
    [SerializeField] private int maxConsecutiveAnomaly = 2;

    [Header("Geçiş Ayarları")]
    [SerializeField] private float sceneTransitionDelay = 0.3f;

    [Header("UI")]
    [SerializeField] private TMPro.TMP_Text levelText;
    [SerializeField] private TMPro.TMP_Text anomalyStatusText;
    
    [Header("Timer")]
    private TMPro.TMP_Text timerText;
    private static float elapsedTime = 0f;
    private bool isTimerRunning = false;
    
    [Header("Death Panel")]
    private GameObject deathPanel;


    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    // Oyun durumu (static - sahneler arası korunur)
    private static int currentRound = 0;
    private static bool currentSceneIsAnomaly = false;
    private static int correctChoices = 0;
    private static int wrongChoices = 0;
    private static List<int> usedAnomalyIndices = new List<int>();
    private static int consecutiveNormalCount = 0;
    private static int consecutiveAnomalyCount = 0;

    // Properties
    public int CurrentRound => currentRound;
    public bool IsAnomalyScene => currentSceneIsAnomaly;
    public int CorrectChoices => correctChoices;
    public int WrongChoices => wrongChoices;
    public int WinCondition => winCondition;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Yeni sahnede state'i belirle
        DetermineCurrentSceneState(scene.name);
        
        // UI referanslarını sıfırla ve yeniden bul
        levelText = null;
        anomalyStatusText = null;
        timerText = null;
        deathPanel = null;
        deathPanel = null;
        
        UpdateLevelUI();
        FindTimerText();
        FindDeathPanel();
        
        // Final sahnesi değilse timer'ı başlat
        if (scene.name != finalSceneName)
        {
            isTimerRunning = true;
        }
        else
        {
            isTimerRunning = false;
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"[LevelManager] Sahne: {scene.name}, Round: {currentRound}, Anomali: {currentSceneIsAnomaly}, Doğru: {correctChoices}");
        }
    }
    
    private void Update()
    {
        // Timer'ı güncelle
        if (isTimerRunning)
        {
            elapsedTime += Time.deltaTime;
            UpdateTimerUI();
        }
    }
    
    private void FindTimerText()
    {
        GameObject textObj = GameObject.Find("timer");
        if (textObj != null)
        {
            timerText = textObj.GetComponent<TMPro.TMP_Text>();
            UpdateTimerUI();
        }
    }
    
    private void UpdateTimerUI()
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(elapsedTime / 60f);
            int seconds = Mathf.FloorToInt(elapsedTime % 60f);
            int milliseconds = Mathf.FloorToInt((elapsedTime * 100f) % 100f);
            timerText.text = string.Format("{0:00}:{1:00}:{2:00}", minutes, seconds, milliseconds);
        }
    }
    
    private void FindDeathPanel()
    {
        GameObject panelObj = GameObject.Find("DeathPanel");
        if (panelObj != null)
        {
            deathPanel = panelObj;
            deathPanel.SetActive(false); // Başlangıçta gizli
        }
    }
    
    private void ShowDeathPanel()
    {
        if (deathPanel != null)
        {
            deathPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("[LevelManager] DeathPanel bulunamadı! Panel olmadan restart atılıyor.");
        }

        // Timer'ı durdur
        isTimerRunning = false; 
        
        // Oyunu duraklat
        Time.timeScale = 0f;
        
        // Mouse'u göster
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 2 saniye sonra otomatik restart
        StartCoroutine(WaitAndRestart());
    }

    private IEnumerator WaitAndRestart()
    {
        // Oyun durduğu için Realtime kullanıyoruz
        yield return new WaitForSecondsRealtime(2f);

        // Zamanı tekrar başlat
        Time.timeScale = 1f;

        // Timer'ı sıfırla
        elapsedTime = 0f;

        // Oyunu sıfırla ve başa dön
        ResetProgress();
        SceneManager.LoadScene(normalSceneName);
    }
    
    public float GetElapsedTime()
    {
        return elapsedTime;
    }

    private void DetermineCurrentSceneState(string sceneName)
    {
        // Varsayılan olarak anomali değil
        currentSceneIsAnomaly = false;
        
        if (sceneName == normalSceneName)
        {
            currentSceneIsAnomaly = false;
        }
        else if (anomalySceneNames != null)
        {
            // Anomali sahnelerinde mi kontrol et
            foreach (string anomalyScene in anomalySceneNames)
            {
                if (anomalyScene == sceneName)
                {
                    currentSceneIsAnomaly = true;
                    break;
                }
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"[LevelManager] DetermineCurrentSceneState: {sceneName} -> Anomaly: {currentSceneIsAnomaly}");
        }
    }

    /// <summary>
    /// Oyuncu kapı seçimi yaptığında çağrılır.
    /// </summary>
    public void ProcessDoorChoice(bool choseAnomalyDoor)
    {
        bool correctChoice;

        // Anomali sahnesi ise -> Anomali kapısı doğru (geri dön)
        // Normal sahne ise -> Normal kapı doğru (ilerle)
        if (currentSceneIsAnomaly)
        {
            correctChoice = choseAnomalyDoor;
        }
        else
        {
            correctChoice = !choseAnomalyDoor;
        }

        if (showDebugInfo)
        {
            Debug.Log($"[LevelManager] Seçim: {(choseAnomalyDoor ? "Anomali Kapısı" : "Normal Kapı")}, " +
                     $"Sahne Anomali: {currentSceneIsAnomaly}, Doğru: {correctChoice}");
        }

        if (correctChoice)
        {
            HandleCorrectChoice();
        }
        else
        {
            HandleWrongChoice();
        }
    }

    private void HandleCorrectChoice()
    {
        correctChoices++;
        currentRound++;
        
        // Her 8 doğruda finale git (8, 16, 24...)
        if (correctChoices % winCondition == 0)
        {
            WinGame();
            return;
        }

        // Sıradaki sahneyi yükle
        LoadNextScene();
    }

    private void HandleWrongChoice()
    {
        wrongChoices++;
        
        if (showDebugInfo)
        {
            Debug.Log("[LevelManager] Yanlış seçim! Başa dönülüyor...");
        }

        // Timer'ı sıfırla
        elapsedTime = 0f;
        
        // Ölüm panelini göster
        ShowDeathPanel();
    }

    private void LoadNextScene()
    {
        bool willBeAnomaly;
        
        // Streak limit kontrolü
        if (consecutiveNormalCount >= maxConsecutiveNormal)
        {
            // Zorla anomali
            willBeAnomaly = anomalySceneNames != null && anomalySceneNames.Length > 0;
            if (showDebugInfo)
                Debug.Log($"[LevelManager] Streak limit: {maxConsecutiveNormal} normal, anomali zorlanıyor");
        }
        else if (consecutiveAnomalyCount >= maxConsecutiveAnomaly)
        {
            // Zorla normal
            willBeAnomaly = false;
            if (showDebugInfo)
                Debug.Log($"[LevelManager] Streak limit: {maxConsecutiveAnomaly} anomali, normal zorlanıyor");
        }
        else
        {
            // Normal rastgele seçim
            willBeAnomaly = Random.value < anomalyChance && 
                            anomalySceneNames != null && 
                            anomalySceneNames.Length > 0 &&
                            usedAnomalyIndices.Count < anomalySceneNames.Length;
        }
        
        string sceneToLoad;
        
        if (willBeAnomaly)
        {
            sceneToLoad = GetRandomUnusedAnomalyScene();
            consecutiveAnomalyCount++;
            consecutiveNormalCount = 0;
        }
        else
        {
            sceneToLoad = normalSceneName;
            consecutiveNormalCount++;
            consecutiveAnomalyCount = 0;
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"[LevelManager] Sıradaki sahne: {sceneToLoad} (Anomali: {willBeAnomaly}, NormalStreak: {consecutiveNormalCount}, AnomalyStreak: {consecutiveAnomalyCount})");
        }

        StartCoroutine(LoadSceneWithDelay(sceneToLoad));
    }

    private string GetRandomUnusedAnomalyScene()
    {
        // Kullanılmamış anomali index'lerini bul
        List<int> availableIndices = new List<int>();
        for (int i = 0; i < anomalySceneNames.Length; i++)
        {
            if (!usedAnomalyIndices.Contains(i))
            {
                availableIndices.Add(i);
            }
        }

        // Eğer tümü kullanıldıysa, listeyi sıfırla
        if (availableIndices.Count == 0)
        {
            usedAnomalyIndices.Clear();
            for (int i = 0; i < anomalySceneNames.Length; i++)
            {
                availableIndices.Add(i);
            }
        }

        // Rastgele bir index seç
        int randomIndex = availableIndices[Random.Range(0, availableIndices.Count)];
        usedAnomalyIndices.Add(randomIndex);
        
        return anomalySceneNames[randomIndex];
    }

    private IEnumerator LoadSceneWithDelay(string sceneName)
    {
        yield return new WaitForSeconds(sceneTransitionDelay);
        
        BlinkTransition blink = BlinkTransition.Instance;
        if (blink != null)
        {
            bool sceneLoaded = false;
            
            blink.Blink(() => {
                SceneManager.LoadScene(sceneName);
                sceneLoaded = true;
            });
            
            while (!sceneLoaded)
            {
                yield return null;
            }
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }
    
    /// <summary>
    /// Kırmızı blink efekti ile sahne yükler (yanlış seçim için)
    /// </summary>
    private IEnumerator LoadSceneWithDelayError(string sceneName)
    {
        yield return new WaitForSeconds(sceneTransitionDelay);
        
        BlinkTransition blink = BlinkTransition.Instance;
        if (blink != null)
        {
            bool sceneLoaded = false;
            
            blink.BlinkError(() => {
                SceneManager.LoadScene(sceneName);
                sceneLoaded = true;
            });
            
            while (!sceneLoaded)
            {
                yield return null;
            }
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }

    private void WinGame()
    {
        if (showDebugInfo)
        {
            Debug.Log($"[LevelManager] KAZANILDI! Doğru: {correctChoices}, Yanlış: {wrongChoices} - Final sahnesine geçiliyor...");
        }
        
        // Direkt Final sahnesine geç
        StartCoroutine(LoadSceneWithDelay(finalSceneName));
    }
    
    /// <summary>
    /// Final sahnesinden çağrılır - Skoru koruyarak Level 0'a döner
    /// </summary>
    public void ContinueFromFinal()
    {
        if (showDebugInfo)
        {
            Debug.Log($"[LevelManager] Final'den devam ediliyor. Toplam Doğru: {correctChoices}, Yanlış: {wrongChoices}");
        }
        
        // Round'u sıfırla ama SKORU KORU
        currentRound = 0;
        usedAnomalyIndices.Clear();
        
        // Level 0'a dön
        StartCoroutine(LoadSceneWithDelay(normalSceneName));
    }

    private void ResetProgress()
    {
        currentRound = 0;
        correctChoices = 0;
        usedAnomalyIndices.Clear();
        consecutiveNormalCount = 0;
        consecutiveAnomalyCount = 0;
    }

    public void ResetGame()
    {
        ResetProgress();
        wrongChoices = 0;
        SceneManager.LoadScene(normalSceneName);
    }
    
    private void UpdateLevelUI()
    {
        if (levelText == null)
        {
            FindLevelText();
        }
        
        if (levelText != null)
        {
            levelText.text = $"Round {currentRound + 1}";
        }
        
        // Anomali durumu text'ini güncelle
        if (anomalyStatusText == null)
        {
            FindAnomalyStatusText();
        }
        
        if (anomalyStatusText != null)
        {
            anomalyStatusText.text = currentSceneIsAnomaly ? "ANOMALY" : "NORMAL";
        }
    }
    
    private void FindLevelText()
    {
        GameObject textObj = GameObject.Find("LevelText");
        if (textObj != null)
        {
            levelText = textObj.GetComponent<TMPro.TMP_Text>();
        }
    }
    
    private void FindAnomalyStatusText()
    {
        GameObject textObj = GameObject.Find("AnomalyStatusText");
        if (textObj != null)
        {
            anomalyStatusText = textObj.GetComponent<TMPro.TMP_Text>();
        }
    }

}