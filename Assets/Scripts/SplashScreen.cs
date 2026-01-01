using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class SplashScreen : MonoBehaviour
{
    [Header("Fade Settings")]
    [SerializeField] private float fadeInDuration = 1.5f;
    [SerializeField] private float displayDuration = 2f;
    [SerializeField] private float fadeOutDuration = 1.5f;
    
    [Header("Next Scene")]
    [SerializeField] private string nextSceneName = "UIMenu";
    
    private Image fadeImage;
    private Canvas fadeCanvas;
    
    private void Start()
    {
        CreateFadeCanvas();
        StartCoroutine(SplashSequence());
    }
    
    private void CreateFadeCanvas()
    {
        // Create fade canvas
        GameObject canvasObj = new GameObject("FadeCanvas");
        fadeCanvas = canvasObj.AddComponent<Canvas>();
        fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fadeCanvas.sortingOrder = 999;
        
        // Add CanvasScaler
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        // Add GraphicRaycaster
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // Create fade image
        GameObject imageObj = new GameObject("FadeImage");
        imageObj.transform.SetParent(canvasObj.transform, false);
        
        fadeImage = imageObj.AddComponent<Image>();
        fadeImage.color = Color.black;
        
        // Set to fill entire screen
        RectTransform rectTransform = fadeImage.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.anchoredPosition = Vector2.zero;
    }
    
    private IEnumerator SplashSequence()
    {
        // Start with black screen (fade image fully visible)
        fadeImage.color = new Color(0, 0, 0, 1);
        
        // Fade In (black -> transparent, revealing splash content)
        yield return StartCoroutine(Fade(1f, 0f, fadeInDuration));
        
        // Display splash screen content
        yield return new WaitForSeconds(displayDuration);
        
        // Fade Out (transparent -> black)
        yield return StartCoroutine(Fade(0f, 1f, fadeOutDuration));
        
        // Load next scene
        SceneManager.LoadScene(nextSceneName);
    }
    
    private IEnumerator Fade(float startAlpha, float endAlpha, float duration)
    {
        float elapsed = 0f;
        Color color = fadeImage.color;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            
            // Smooth easing
            t = t * t * (3f - 2f * t);
            
            color.a = Mathf.Lerp(startAlpha, endAlpha, t);
            fadeImage.color = color;
            
            yield return null;
        }
        
        color.a = endAlpha;
        fadeImage.color = color;
    }
}
