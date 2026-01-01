using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Professional credits screen manager with scrolling text and studio logo.
/// Self-contained - creates its own UI elements with fade in/out.
/// Credits start from bottom edge of screen and scroll upward until completely off-screen.
/// </summary>
public class CreditsManager : MonoBehaviour
{
    [Header("Fade Settings")]
    [SerializeField] private float fadeInDuration = 1.5f;
    [SerializeField] private float fadeOutDuration = 2f;
    
    [Header("Scroll Settings")]
    [Tooltip("Speed of credits scroll (pixels per second)")]
    [SerializeField] private float scrollSpeed = 80f;
    
    [Tooltip("Starting Y position offset. 0 = bottom of screen, positive = higher up")]
    [SerializeField] private float startYOffset = 0f;
    
    [Header("Font Settings")]
    [Tooltip("Optional: Custom font for credits text. Leave empty to use default.")]
    [SerializeField] private TMP_FontAsset customFont;
    
    [Tooltip("Base font size for credits text")]
    [SerializeField] private float baseFontSize = 36f;
    
    [Header("Studio Logo Settings")]
    [Tooltip("Studio logo sprite to show after credits")]
    [SerializeField] private Sprite studioLogo;
    
    [Tooltip("Size of the logo (width, height)")]
    [SerializeField] private Vector2 logoSize = new Vector2(400, 400);
    
    [Tooltip("How long the logo stays visible")]
    [SerializeField] private float logoDisplayDuration = 3f;
    
    [Tooltip("Logo fade in/out duration")]
    [SerializeField] private float logoFadeDuration = 1f;
    
    [Header("Scene Settings")]
    [SerializeField] private string mainMenuSceneName = "UIMenu";

    // Professional English credits content
    private readonly string creditsContent = @"




<size=80><b>VOID LOOP</b></size>




<size=32><i>The loop has been broken...</i></size>

<size=28><i>You walked through the dark corridors of the anomaly.
You pushed the boundaries of reality.
And finally... you found freedom.</i></size>

<size=32><i>The cycle has ended.</i></size>






<size=50>— CREDITS —</size>




<size=40><b>DEVELOPMENT TEAM</b></size>




<size=38><b>Fatih Serbest</b></size>
<size=26>Lead Game Developer
Programming & Game Design</size>




<size=38><b>Beyzanur Baki</b></size>
<size=26>3D Artist & Game Developer
Environment Design & Level Art</size>




<size=38><b>Mülayim Can Parmak</b></size>
<size=26>SFX/VFX Artist & UI Designer
Audio Design & Visual Effects & Game Developer</size>








<size=36><b>SPECIAL THANKS</b></size>

<size=26>To everyone who played and supported this game.
Your feedback made this journey worthwhile.</size>






<size=32>Thank you for playing!</size>




<size=22>© 2026 VOID LOOP - All Rights Reserved</size>




";

    private Canvas canvas;
    private CanvasScaler canvasScaler;
    private Image fadeOverlay;
    private Image logoImage;
    private RectTransform creditsTextRT;
    private TextMeshProUGUI creditsTextTMP;
    
    private float referenceHeight = 1080f;
    
    private void Awake()
    {
        // Destroy any leftover fade canvas from previous scene
        var oldCanvas = GameObject.Find("FadeCanvas");
        if (oldCanvas != null) Destroy(oldCanvas);
        
        CreateUI();
    }
    
    private void CreateUI()
    {
        // Create Canvas
        GameObject canvasObj = new GameObject("CreditsCanvas");
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        
        canvasScaler = canvasObj.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920, 1080);
        canvasScaler.matchWidthOrHeight = 0.5f;
        referenceHeight = canvasScaler.referenceResolution.y;
        
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // Create black background
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform, false);
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = Color.black;
        RectTransform bgRT = bgImage.rectTransform;
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;
        
        // Create credits text container
        GameObject textObj = new GameObject("CreditsText");
        textObj.transform.SetParent(canvasObj.transform, false);
        creditsTextTMP = textObj.AddComponent<TextMeshProUGUI>();
        creditsTextTMP.text = creditsContent;
        creditsTextTMP.fontSize = baseFontSize;
        creditsTextTMP.alignment = TextAlignmentOptions.Center;
        creditsTextTMP.color = Color.white;
        creditsTextTMP.enableWordWrapping = true;
        
        // Apply custom font if assigned
        if (customFont != null)
        {
            creditsTextTMP.font = customFont;
        }
        
        creditsTextRT = creditsTextTMP.rectTransform;
        // Anchor to bottom of screen, full width
        creditsTextRT.anchorMin = new Vector2(0.1f, 0);
        creditsTextRT.anchorMax = new Vector2(0.9f, 0);
        creditsTextRT.pivot = new Vector2(0.5f, 1f); // Top-center pivot
        creditsTextRT.sizeDelta = new Vector2(0, 0);
        
        // Create studio logo (centered, hidden initially)
        GameObject logoObj = new GameObject("StudioLogo");
        logoObj.transform.SetParent(canvasObj.transform, false);
        logoImage = logoObj.AddComponent<Image>();
        logoImage.sprite = studioLogo;
        logoImage.preserveAspect = true;
        logoImage.color = new Color(1, 1, 1, 0); // Start invisible
        logoImage.raycastTarget = false;
        RectTransform logoRT = logoImage.rectTransform;
        logoRT.anchorMin = new Vector2(0.5f, 0.5f);
        logoRT.anchorMax = new Vector2(0.5f, 0.5f);
        logoRT.pivot = new Vector2(0.5f, 0.5f);
        logoRT.sizeDelta = logoSize;
        logoRT.anchoredPosition = Vector2.zero;
        
        // Create fade overlay (on top of everything)
        GameObject fadeObj = new GameObject("FadeOverlay");
        fadeObj.transform.SetParent(canvasObj.transform, false);
        fadeOverlay = fadeObj.AddComponent<Image>();
        fadeOverlay.color = Color.black; // Start fully black
        fadeOverlay.raycastTarget = false;
        RectTransform fadeRT = fadeOverlay.rectTransform;
        fadeRT.anchorMin = Vector2.zero;
        fadeRT.anchorMax = Vector2.one;
        fadeRT.offsetMin = Vector2.zero;
        fadeRT.offsetMax = Vector2.zero;
    }
    
    private void Start()
    {
        StartCoroutine(CreditsSequence());
    }
    
    private IEnumerator CreditsSequence()
    {
        // Wait a frame for layout to initialize
        yield return null;
        Canvas.ForceUpdateCanvases();
        
        // Force TextMeshPro to recalculate
        creditsTextTMP.ForceMeshUpdate();
        
        // Get the actual text height
        float textHeight = creditsTextTMP.preferredHeight;
        creditsTextRT.sizeDelta = new Vector2(creditsTextRT.sizeDelta.x, textHeight);
        
        // Position text - startYOffset controls starting height (0 = bottom edge, positive = higher)
        creditsTextRT.anchoredPosition = new Vector2(0, startYOffset);
        
        // Fade in from black
        yield return StartCoroutine(Fade(1f, 0f, fadeInDuration));
        
        // Calculate total scroll distance
        float totalScrollDistance = referenceHeight + textHeight;
        float scrolled = 0f;
        
        // Scroll credits upward until completely off screen
        while (scrolled < totalScrollDistance)
        {
            float delta = scrollSpeed * Time.deltaTime;
            creditsTextRT.anchoredPosition += new Vector2(0, delta);
            scrolled += delta;
            yield return null;
        }
        
        // Hide credits text
        creditsTextTMP.gameObject.SetActive(false);
        
        // Show studio logo if assigned
        if (studioLogo != null)
        {
            // Fade in logo
            yield return StartCoroutine(FadeImage(logoImage, 0f, 1f, logoFadeDuration));
            
            // Display logo
            yield return new WaitForSeconds(logoDisplayDuration);
            
            // Fade out logo
            yield return StartCoroutine(FadeImage(logoImage, 1f, 0f, logoFadeDuration));
        }
        
        // Fade out to black
        yield return StartCoroutine(Fade(0f, 1f, fadeOutDuration));
        
        // Small delay before scene change
        yield return new WaitForSeconds(0.3f);
        
        // Return to main menu
        SceneManager.LoadScene(mainMenuSceneName);
    }
    
    private IEnumerator Fade(float startAlpha, float endAlpha, float duration)
    {
        if (fadeOverlay == null) yield break;
        
        float elapsed = 0f;
        Color fadeColor = fadeOverlay.color;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t); // Smooth step
            
            fadeColor.a = Mathf.Lerp(startAlpha, endAlpha, t);
            fadeOverlay.color = fadeColor;
            
            yield return null;
        }
        
        fadeColor.a = endAlpha;
        fadeOverlay.color = fadeColor;
    }
    
    private IEnumerator FadeImage(Image image, float startAlpha, float endAlpha, float duration)
    {
        if (image == null) yield break;
        
        float elapsed = 0f;
        Color imgColor = image.color;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t); // Smooth step
            
            imgColor.a = Mathf.Lerp(startAlpha, endAlpha, t);
            image.color = imgColor;
            
            yield return null;
        }
        
        imgColor.a = endAlpha;
        image.color = imgColor;
    }
}
