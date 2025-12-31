using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class LoadingManager : MonoBehaviour
{
    public static string TargetSceneName = "Level_0_Tutorial";

    [Header("UI References")]
    [Tooltip("Progress Bar olarak kullanılan Image (Fill Amount ile çalışır)")]
    [SerializeField] private Image progressBarImage;
    [SerializeField] private TMP_Text progressText;
    [Tooltip("Text to show when loading is done (e.g. Press any key to continue)")]
    [SerializeField] private TMP_Text promptText; 
    [SerializeField] private TMP_Text hintText;
    [SerializeField] private CanvasGroup contentCanvasGroup;

    [Header("Settings")]
    [SerializeField] private float minimumLoadingTime = 2.5f; 
    
    [TextArea]
    [SerializeField] private string[] hints = new string[]
    {
        "Light is your only sanctuary in the dark. Use your battery wisely.",
        "Listen to the sounds. They may guide you... or warn you of approaching danger.",
        "Sometimes running is better than fighting.",
        "Every loop brings you closer to the truth.",
        "Your eyes may deceive you, but the light never lies.",
        "Watch the corners; you never know what hides in the shadows.",
        "If you get lost, try returning to where you started.",
        "Some doors open only at the right time."
    };

    private void Start()
    {
        Time.timeScale = 1f;

        // UI Preparation
        if (progressBarImage) progressBarImage.fillAmount = 0f;
        if (progressText) progressText.text = "0%";
        if (promptText) promptText.gameObject.SetActive(false);
        
        // Select a random hint
        if (hints != null && hints.Length > 0 && hintText != null)
        {
            hintText.text = hints[Random.Range(0, hints.Length)];
        }

        // Start loading
        StartCoroutine(LoadSceneAsync(TargetSceneName));
    }

    public static void LoadScene(string sceneName)
    {
        TargetSceneName = sceneName;
        SceneManager.LoadScene("LoadingScreen");
    }

    private IEnumerator LoadSceneAsync(string sceneName)
    {
        float startTime = Time.time;
        
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        while (!operation.isDone)
        {
            float progress = Mathf.Clamp01(operation.progress / 0.9f);
            
            if (progressBarImage) progressBarImage.fillAmount = progress;
            if (progressText) progressText.text = Mathf.RoundToInt(progress * 100) + "%";

            bool isLoaded = operation.progress >= 0.9f;
            bool timeElapsed = (Time.time - startTime) >= minimumLoadingTime;

            if (isLoaded && timeElapsed)
            {
                if (progressBarImage) progressBarImage.fillAmount = 1f;
                if (progressText) progressText.text = "100%";
                
                if (promptText)
                {
                    promptText.gameObject.SetActive(true);
                    
                    // Simple blink effect
                    float alpha = Mathf.PingPong(Time.time * 2f, 1f);
                    promptText.color = new Color(promptText.color.r, promptText.color.g, promptText.color.b, alpha);
                }

                if (Input.anyKeyDown)
                {
                    operation.allowSceneActivation = true;
                }
            }

            yield return null;
        }
    }
}
