using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Göz kırpma efekti ile sahne geçişi.
/// Canvas üzerinde siyah veya kırmızı panel ile fade in/out yapar.
/// </summary>
public class BlinkTransition : MonoBehaviour
{
    public static BlinkTransition Instance { get; private set; }

    [Header("Geçiş Ayarları")]
    [SerializeField] private float blinkDuration = 0.3f;
    [SerializeField] private float holdDuration = 0.1f;
    [SerializeField] private AnimationCurve blinkCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Renk Ayarları")]
    [SerializeField] private Color normalBlinkColor = Color.black;
    [SerializeField] private Color errorBlinkColor = new Color(0.8f, 0f, 0f, 1f); // Koyu kırmızı
    
    [Header("UI Referansları")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private Canvas fadeCanvas;

    private bool isTransitioning = false;
    private Color currentBlinkColor;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SetupUI();
            currentBlinkColor = normalBlinkColor;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void SetupUI()
    {
        // Eğer referanslar atanmamışsa otomatik oluştur
        if (fadeCanvas == null)
        {
            GameObject canvasObj = new GameObject("BlinkCanvas");
            canvasObj.transform.SetParent(transform);
            
            fadeCanvas = canvasObj.AddComponent<Canvas>();
            fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            fadeCanvas.sortingOrder = 999; // En üstte
            
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        if (fadeImage == null)
        {
            GameObject imageObj = new GameObject("FadeImage");
            imageObj.transform.SetParent(fadeCanvas.transform);
            
            fadeImage = imageObj.AddComponent<Image>();
            fadeImage.color = new Color(0, 0, 0, 0);
            
            // Tam ekran kaplasın
            RectTransform rect = fadeImage.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        // Başlangıçta şeffaf
        SetAlpha(0);
    }

    /// <summary>
    /// Normal siyah göz kırpma efekti
    /// </summary>
    public void Blink(System.Action onBlinkPeak = null)
    {
        if (!isTransitioning)
        {
            currentBlinkColor = normalBlinkColor;
            StartCoroutine(BlinkRoutine(onBlinkPeak));
        }
    }
    
    /// <summary>
    /// Kırmızı blink efekti (yanlış seçim için)
    /// </summary>
    public void BlinkError(System.Action onBlinkPeak = null)
    {
        if (!isTransitioning)
        {
            currentBlinkColor = errorBlinkColor;
            StartCoroutine(BlinkRoutine(onBlinkPeak));
        }
    }

    /// <summary>
    /// Sadece karartma (göz kapanma)
    /// </summary>
    public void FadeOut(System.Action onComplete = null)
    {
        if (!isTransitioning)
        {
            currentBlinkColor = normalBlinkColor;
            StartCoroutine(FadeRoutine(0, 1, onComplete));
        }
    }

    /// <summary>
    /// Sadece açılma (göz açılma)
    /// </summary>
    public void FadeIn(System.Action onComplete = null)
    {
        StartCoroutine(FadeRoutine(1, 0, onComplete));
    }

    private IEnumerator BlinkRoutine(System.Action onBlinkPeak)
    {
        isTransitioning = true;

        // Karart (göz kapanıyor)
        yield return StartCoroutine(FadeRoutine(0, 1, null));
        
        // Callback'i çağır (sahne değişimi burada olur)
        onBlinkPeak?.Invoke();
        
        // Kısa bekleme
        yield return new WaitForSeconds(holdDuration);
        
        // Aç (göz açılıyor)
        yield return StartCoroutine(FadeRoutine(1, 0, null));

        isTransitioning = false;
        
        // Rengi normale döndür
        currentBlinkColor = normalBlinkColor;
    }

    private IEnumerator FadeRoutine(float startAlpha, float endAlpha, System.Action onComplete)
    {
        float elapsed = 0f;
        SetColorWithAlpha(startAlpha);

        while (elapsed < blinkDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = blinkCurve.Evaluate(elapsed / blinkDuration);
            float alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            SetColorWithAlpha(alpha);
            yield return null;
        }

        SetColorWithAlpha(endAlpha);
        onComplete?.Invoke();
    }

    private void SetAlpha(float alpha)
    {
        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = alpha;
            fadeImage.color = c;
        }
    }
    
    private void SetColorWithAlpha(float alpha)
    {
        if (fadeImage != null)
        {
            Color c = currentBlinkColor;
            c.a = alpha;
            fadeImage.color = c;
        }
    }

    public bool IsTransitioning => isTransitioning;
}
