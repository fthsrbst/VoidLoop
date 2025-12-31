using UnityEngine;

/// <summary>
/// Nesnenin rengini sürekli değiştirir.
/// Renderer veya Light component'ı olan nesnelerde çalışır.
/// </summary>
public class ColorChanger : MonoBehaviour
{
    [Header("Renk Değişim Modu")]
    [Tooltip("Rainbow: Gökkuşağı döngüsü, Random: Rastgele renkler")]
    [SerializeField] private ColorMode mode = ColorMode.Rainbow;
    
    [Header("Hız Ayarları")]
    [Tooltip("Renk değişim hızı")]
    [SerializeField] private float changeSpeed = 1f;
    
    [Tooltip("Rastgele modda renk değişim aralığı")]
    [SerializeField] private float randomChangeInterval = 0.5f;
    
    [Header("Parlaklık")]
    [Tooltip("Emission (parlama) kullan")]
    [SerializeField] private bool useEmission = true;
    
    [Tooltip("Emission şiddeti")]
    [SerializeField] private float emissionIntensity = 2f;
    
    public enum ColorMode
    {
        Rainbow,
        Random
    }
    
    private Renderer targetRenderer;
    private Light targetLight;
    private MaterialPropertyBlock propertyBlock;
    private float hue;
    private float randomTimer;
    private Color targetColor;
    private Color currentColor;

    private void Awake()
    {
        targetRenderer = GetComponent<Renderer>();
        targetLight = GetComponent<Light>();
        
        if (targetRenderer == null && targetLight == null)
        {
            Debug.LogWarning("[ColorChanger] Bu script Renderer veya Light component'ı olan bir objeye eklenmelidir!");
        }
        
        propertyBlock = new MaterialPropertyBlock();
        currentColor = Color.red;
        targetColor = Color.red;
    }

    private void Update()
    {
        switch (mode)
        {
            case ColorMode.Rainbow:
                UpdateRainbow();
                break;
            case ColorMode.Random:
                UpdateRandom();
                break;
        }
        
        ApplyColor(currentColor);
    }

    private void UpdateRainbow()
    {
        hue += changeSpeed * Time.deltaTime;
        if (hue > 1f) hue -= 1f;
        
        currentColor = Color.HSVToRGB(hue, 1f, 1f);
    }

    private void UpdateRandom()
    {
        randomTimer += Time.deltaTime;
        
        if (randomTimer >= randomChangeInterval)
        {
            randomTimer = 0f;
            targetColor = new Color(
                Random.Range(0f, 1f),
                Random.Range(0f, 1f),
                Random.Range(0f, 1f)
            );
        }
        
        currentColor = Color.Lerp(currentColor, targetColor, changeSpeed * Time.deltaTime);
    }

    private void ApplyColor(Color color)
    {
        // Renderer için
        if (targetRenderer != null)
        {
            targetRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor("_BaseColor", color);
            propertyBlock.SetColor("_Color", color);
            
            if (useEmission)
            {
                Color emissionColor = color * emissionIntensity;
                propertyBlock.SetColor("_EmissionColor", emissionColor);
            }
            
            targetRenderer.SetPropertyBlock(propertyBlock);
        }
        
        // Light için
        if (targetLight != null)
        {
            targetLight.color = color;
        }
    }
}
