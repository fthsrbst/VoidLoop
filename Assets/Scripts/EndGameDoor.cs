using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Trigger for the end game door in Final scene.
/// When player enters, fades to black and loads Credits scene.
/// Creates its own fade overlay if none is assigned.
/// </summary>
public class EndGameDoor : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Duration of fade to black")]
    [SerializeField] private float fadeDuration = 1.5f;
    
    [Tooltip("Scene to load after fade")]
    [SerializeField] private string creditsSceneName = "Credits";
    
    [Tooltip("Tag of the player object")]
    [SerializeField] private string playerTag = "Player";
    
    [Header("References (Optional)")]
    [Tooltip("Full screen fade overlay Image. If not assigned, will be created automatically.")]
    [SerializeField] private Image fadeOverlay;
    
    private bool hasTriggered = false;
    
    private void Start()
    {
        // Create fade overlay if not assigned
        if (fadeOverlay == null)
        {
            CreateFadeOverlay();
        }
        
        // Ensure fade overlay starts invisible
        if (fadeOverlay != null)
        {
            fadeOverlay.gameObject.SetActive(true);
            Color c = fadeOverlay.color;
            c.a = 0f;
            fadeOverlay.color = c;
        }
    }
    
    private void CreateFadeOverlay()
    {
        // Create Canvas
        GameObject canvasObj = new GameObject("FadeCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999; // On top of everything
        canvasObj.AddComponent<CanvasScaler>();
        
        // Create fade panel
        GameObject panelObj = new GameObject("FadeOverlay");
        panelObj.transform.SetParent(canvasObj.transform, false);
        
        fadeOverlay = panelObj.AddComponent<Image>();
        fadeOverlay.color = new Color(0, 0, 0, 0);
        
        // Stretch to fill screen
        RectTransform rt = fadeOverlay.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        
        // Don't destroy on load so it persists during scene transition
        DontDestroyOnLoad(canvasObj);
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;
        
        if (other.CompareTag(playerTag))
        {
            hasTriggered = true;
            StartCoroutine(EndGameSequence());
        }
    }
    
    private IEnumerator EndGameSequence()
    {
        // Fade to black
        if (fadeOverlay != null)
        {
            float elapsed = 0f;
            Color fadeColor = fadeOverlay.color;
            
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / fadeDuration;
                t = t * t * (3f - 2f * t); // Smooth step
                
                fadeColor.a = Mathf.Lerp(0f, 1f, t);
                fadeOverlay.color = fadeColor;
                
                yield return null;
            }
            
            fadeColor.a = 1f;
            fadeOverlay.color = fadeColor;
        }
        
        // Small delay at black
        yield return new WaitForSecondsRealtime(0.3f);
        
        // Load credits scene
        SceneManager.LoadScene(creditsSceneName);
    }
}

