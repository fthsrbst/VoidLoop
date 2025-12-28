using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject infoPanel;

    [Header("Scene To Load On Play")]
    [SerializeField] private string gameSceneName = "LoadingScreen";

    private void Awake()
    {
        // PC menü: mouse serbest
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Açýlýþ düzeni
        ShowMainMenu();
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
            Debug.LogWarning("gameSceneName boþ. Inspector'dan Level_0_Tutorial yaz.");
            return;
        }

        SceneManager.LoadScene(gameSceneName);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        // Unity Editor'da oyunu kapatmanýn karþýlýðý: Play Mode'u durdurmak
        UnityEditor.EditorApplication.isPlaying = false;
#else
    // Build alýnca gerçek uygulamayý kapatýr
    Application.Quit();
#endif
    }

}
