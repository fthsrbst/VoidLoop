using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// El feneri pil durumunu gösteren UI elementi.
/// Slider veya Image fill ile çalışır.
/// </summary>
public class FlashlightBatteryUI : MonoBehaviour
{
    [Header("Referanslar")]
    [Tooltip("Flashlight scripti (boş bırakılırsa otomatik bulunur)")]
    [SerializeField] private Flashlight flashlight;
    
    [Header("UI Elementleri")]
    [SerializeField] private Slider batterySlider;
    [SerializeField] private Image batteryFillImage;
    [SerializeField] private TMP_Text batteryText;
    [SerializeField] private Image batteryIcon;
    
    [Header("Renk Ayarları")]
    [SerializeField] private Color fullColor = new Color(0.2f, 0.8f, 0.2f); // Yeşil
    [SerializeField] private Color mediumColor = new Color(1f, 0.8f, 0f);   // Sarı
    [SerializeField] private Color lowColor = new Color(0.9f, 0.2f, 0.2f);  // Kırmızı
    [SerializeField] private float mediumThreshold = 0.5f;
    [SerializeField] private float lowThreshold = 0.25f;
    
    [Header("Animasyon")]
    [SerializeField] private bool pulseOnLow = true;
    [SerializeField] private float pulseSpeed = 3f;
    
    [Header("Görünürlük")]
    [SerializeField] private bool hideWhenFull = false;
    [SerializeField] private bool hideWhenOff = true;
    [SerializeField] private float fadeSpeed = 5f;
    
    private CanvasGroup canvasGroup;
    private float targetAlpha = 1f;

    private void Start()
    {
        // Flashlight'ı bul
        if (flashlight == null)
        {
            flashlight = FindObjectOfType<Flashlight>();
            if (flashlight == null)
            {
                Debug.LogWarning("[FlashlightBatteryUI] Flashlight bulunamadı!");
            }
        }
        
        // CanvasGroup ekle (fade için)
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        // Slider ayarları
        if (batterySlider != null)
        {
            batterySlider.minValue = 0;
            batterySlider.maxValue = 1;
        }
    }

    private void Update()
    {
        if (flashlight == null) return;
        
        float batteryPercent = flashlight.BatteryPercent;
        
        // UI güncelle
        UpdateBatteryDisplay(batteryPercent);
        UpdateColor(batteryPercent);
        UpdateVisibility(batteryPercent);
        UpdatePulse(batteryPercent);
        
        // Fade animasyonu
        if (canvasGroup != null)
        {
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, fadeSpeed * Time.deltaTime);
        }
    }

    private void UpdateBatteryDisplay(float percent)
    {
        // Slider
        if (batterySlider != null)
        {
            batterySlider.value = percent;
        }
        
        // Image Fill
        if (batteryFillImage != null)
        {
            batteryFillImage.fillAmount = percent;
        }
        
        // Text
        if (batteryText != null)
        {
            batteryText.text = $"{Mathf.RoundToInt(percent * 100)}%";
        }
    }

    private void UpdateColor(float percent)
    {
        Color targetColor;
        
        if (percent <= lowThreshold)
        {
            targetColor = lowColor;
        }
        else if (percent <= mediumThreshold)
        {
            // Sarı-yeşil arası lerp
            float t = (percent - lowThreshold) / (mediumThreshold - lowThreshold);
            targetColor = Color.Lerp(lowColor, mediumColor, t);
        }
        else
        {
            // Yeşil
            float t = (percent - mediumThreshold) / (1f - mediumThreshold);
            targetColor = Color.Lerp(mediumColor, fullColor, t);
        }
        
        // Rengi uygula
        if (batteryFillImage != null)
        {
            batteryFillImage.color = targetColor;
        }
        
        if (batterySlider != null && batterySlider.fillRect != null)
        {
            Image fillImage = batterySlider.fillRect.GetComponent<Image>();
            if (fillImage != null)
            {
                fillImage.color = targetColor;
            }
        }
        
        if (batteryText != null)
        {
            batteryText.color = targetColor;
        }
    }

    private void UpdateVisibility(float percent)
    {
        // Kapalıyken gizle
        if (hideWhenOff && !flashlight.IsOn)
        {
            targetAlpha = 0f;
            return;
        }
        
        // Tam doluyken gizle
        if (hideWhenFull && percent >= 0.99f)
        {
            targetAlpha = 0f;
            return;
        }
        
        targetAlpha = 1f;
    }

    private void UpdatePulse(float percent)
    {
        if (!pulseOnLow || percent > lowThreshold) return;
        
        // Düşük pilde nabız efekti
        float pulse = Mathf.Sin(Time.time * pulseSpeed) * 0.5f + 0.5f;
        
        if (batteryIcon != null)
        {
            Color iconColor = batteryIcon.color;
            iconColor.a = Mathf.Lerp(0.5f, 1f, pulse);
            batteryIcon.color = iconColor;
        }
    }
    
    // Public metodlar
    public void SetFlashlight(Flashlight fl)
    {
        flashlight = fl;
    }
}
