using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject infoPanel;

    [Header("Scene To Load On Play")]
    [SerializeField] private string gameSceneName = "Level_0_Tutorial";

    private void Awake()
    {
        // PC menü: mouse serbest
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Açılış düzeni
        ShowMainMenu();
        
        // Zaman akışını düzelt (pause'dan dönülmüş olabilir)
        Time.timeScale = 1f;
    }

    public void ShowMainMenu()
    {
        if (mainMenuPanel) mainMenuPanel.SetActive(true);
        if (settingsPanel) settingsPanel.SetActive(false);
        if (infoPanel) infoPanel.SetActive(false);
    }

    public void OpenSettings()
    {
        if (mainMenuPanel) mainMenuPanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(true);
        if (infoPanel) infoPanel.SetActive(false);
    }

    public void OpenInfo()
    {
        if (mainMenuPanel) mainMenuPanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(false);
        if (infoPanel) infoPanel.SetActive(true);
    }

    public void PlayGame()
    {
        if (string.IsNullOrEmpty(gameSceneName))
        {
            Debug.LogWarning("gameSceneName boş. Inspector'dan Level_0_Tutorial yazmalısın.");
            return;
        }

        // Yeni sistem: LoadingManager üzerinden git
        LoadingManager.LoadScene(gameSceneName);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
