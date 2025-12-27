using UnityEngine;
using UnityEngine.SceneManagement;
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
    
    [Header("Oyun Ayarları")]
    [Tooltip("Anomali çıkma olasılığı (0-1)")]
    [Range(0f, 1f)]
    [SerializeField] private float anomalyChance = 0.5f;
    
    [Tooltip("Kaç doğru seçim ile kazanılır")]
    [SerializeField] private int winCondition = 8;

    [Header("Geçiş Ayarları")]
    [SerializeField] private float sceneTransitionDelay = 0.3f;

    [Header("UI")]
    [SerializeField] private TMPro.TMP_Text levelText;
    [SerializeField] private TMPro.TMP_Text anomalyStatusText;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;

    // Oyun durumu (static - sahneler arası korunur)
    private static int currentRound = 0;
    private static bool currentSceneIsAnomaly = false;
    private static int correctChoices = 0;
    private static int wrongChoices = 0;
    private static List<int> usedAnomalyIndices = new List<int>();

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
        UpdateLevelUI();
        
        if (showDebugInfo)
        {
            Debug.Log($"[LevelManager] Sahne: {scene.name}, Round: {currentRound}, Anomali: {currentSceneIsAnomaly}, Doğru: {correctChoices}");
        }
    }

    private void DetermineCurrentSceneState(string sceneName)
    {
        if (sceneName == normalSceneName)
        {
            currentSceneIsAnomaly = false;
        }
        else
        {
            // Anomali sahnelerinde mi kontrol et
            foreach (string anomalyScene in anomalySceneNames)
            {
                if (anomalyScene == sceneName)
                {
                    currentSceneIsAnomaly = true;
                    return;
                }
            }
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
        
        // Kazandık mı?
        if (correctChoices >= winCondition)
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

        // Oyunu sıfırla
        ResetProgress();
        
        // Normal sahneye dön
        StartCoroutine(LoadSceneWithDelay(normalSceneName));
    }

    private void LoadNextScene()
    {
        // Rastgele anomali mi normal mi?
        bool willBeAnomaly = Random.value < anomalyChance && 
                            anomalySceneNames != null && 
                            anomalySceneNames.Length > 0 &&
                            usedAnomalyIndices.Count < anomalySceneNames.Length;
        
        string sceneToLoad;
        
        if (willBeAnomaly)
        {
            sceneToLoad = GetRandomUnusedAnomalyScene();
        }
        else
        {
            sceneToLoad = normalSceneName;
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"[LevelManager] Sıradaki sahne: {sceneToLoad} (Anomali: {willBeAnomaly})");
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

    private void WinGame()
    {
        if (showDebugInfo)
        {
            Debug.Log($"[LevelManager] KAZANILDI! Doğru: {correctChoices}, Yanlış: {wrongChoices}");
        }
        
        // Win işlemleri (sahne yükle veya UI göster)
        // SceneManager.LoadScene("WinScene");
    }

    private void ResetProgress()
    {
        currentRound = 0;
        correctChoices = 0;
        usedAnomalyIndices.Clear();
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

    // Debug UI
    private void OnGUI()
    {
        if (!showDebugInfo) return;

        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.fontSize = 14;
        
        string info = $"Round: {currentRound + 1}\n" +
                     $"Anomali: {currentSceneIsAnomaly}\n" +
                     $"Doğru: {correctChoices}/{winCondition}\n" +
                     $"Yanlış: {wrongChoices}";
        
        GUI.Box(new Rect(10, 10, 180, 100), info, style);
    }
}
