using UnityEngine;

/// <summary>
/// Agresif ışık titreşim efekti.
/// Bu scripti herhangi bir Light objesine ekleyerek kullanın.
/// </summary>
public class LightFlicker : MonoBehaviour
{
    [Header("Titreşim Ayarları")]
    [Tooltip("Minimum titreşim süresi (saniye)")]
    [SerializeField] private float minFlickerTime = 0.02f;
    
    [Tooltip("Maksimum titreşim süresi (saniye)")]
    [SerializeField] private float maxFlickerTime = 0.15f;
    
    [Tooltip("Işığın açık kalma olasılığı (0-1)")]
    [Range(0f, 1f)]
    [SerializeField] private float onChance = 0.7f;
    
    [Tooltip("Intensity değişimi kullan")]
    [SerializeField] private bool useIntensityVariation = true;
    
    [Tooltip("Minimum intensity çarpanı")]
    [Range(0f, 1f)]
    [SerializeField] private float minIntensityMultiplier = 0.3f;
    
    private Light targetLight;
    private float baseIntensity;
    private float flickerTimer;
    private float nextFlickerTime;

    private void Awake()
    {
        targetLight = GetComponent<Light>();
        if (targetLight == null)
        {
            Debug.LogError("[LightFlicker] Bu script bir Light component'ı olan objeye eklenmelidir!");
            enabled = false;
            return;
        }
        baseIntensity = targetLight.intensity;
    }

    private void Update()
    {
        flickerTimer += Time.deltaTime;

        if (flickerTimer >= nextFlickerTime)
        {
            flickerTimer = 0f;
            nextFlickerTime = Random.Range(minFlickerTime, maxFlickerTime);
            
            // Rastgele açık/kapalı
            bool shouldBeOn = Random.value < onChance;
            targetLight.enabled = shouldBeOn;
            
            // Intensity değişimi
            if (shouldBeOn && useIntensityVariation)
            {
                float intensityMultiplier = Random.Range(minIntensityMultiplier, 1f);
                targetLight.intensity = baseIntensity * intensityMultiplier;
            }
        }
    }

    /// <summary>
    /// Runtime'da base intensity'yi güncelle
    /// </summary>
    public void SetBaseIntensity(float newIntensity)
    {
        baseIntensity = newIntensity;
    }
}
