using UnityEngine;

/// <summary>
/// First Person el feneri - kameranın baktığı yöne doğru ışık tutar.
/// Kullanım: Bu scripti Player'ın child objesi olarak oluşturun veya 
/// kameraya ekleyin. Spotlight otomatik oluşturulur veya referans atayın.
/// </summary>
public class Flashlight : MonoBehaviour
{
    [Header("Işık Ayarları")]
    [Tooltip("Spotlight referansı (boş bırakılırsa otomatik oluşturulur)")]
    [SerializeField] private Light flashlightLight;
    
    [SerializeField] private float intensity = 2f;
    [SerializeField] private float range = 25f;
    [SerializeField] private float spotAngle = 45f;
    [SerializeField] private float innerSpotAngle = 25f;
    [SerializeField] private Color lightColor = Color.white;
    
    [Header("Pozisyon Ayarları")]
    [Tooltip("Kamera referansı (boş bırakılırsa Main Camera kullanılır)")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Vector3 positionOffset = new Vector3(0.3f, -0.2f, 0.5f);
    
    [Header("Kontrol Ayarları")]
    [SerializeField] private KeyCode toggleKey = KeyCode.F;
    [SerializeField] private KeyCode gamepadToggle = KeyCode.JoystickButton3; // Y butonu
    [SerializeField] private bool startsOn = true;
    
    [Header("Ses Efektleri")]
    [SerializeField] private AudioClip toggleOnSound;
    [SerializeField] private AudioClip toggleOffSound;
    
    [Header("Pil Sistemi (Opsiyonel)")]
    [SerializeField] private bool useBattery = false;
    [SerializeField] private float maxBattery = 100f;
    [SerializeField] private float batteryDrainRate = 1f; // saniyede
    [SerializeField] private float batteryRechargeRate = 0.5f; // kapalıyken saniyede
    
    private AudioSource audioSource;
    private bool isOn;
    private float currentBattery;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f;
            audioSource.playOnAwake = false;
        }
    }

    private void Start()
    {
        // Kamera referansını bul
        if (cameraTransform == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                cameraTransform = mainCam.transform;
            }
            else
            {
                Debug.LogError("[Flashlight] Kamera bulunamadı!");
                return;
            }
        }

        // Spotlight oluştur veya ayarla
        SetupLight();
        
        // Başlangıç durumu
        currentBattery = maxBattery;
        isOn = startsOn;
        UpdateLight();
    }

    private void SetupLight()
    {
        if (flashlightLight == null)
        {
            // Yeni Spotlight oluştur
            GameObject lightObj = new GameObject("FlashlightSpotlight");
            lightObj.transform.SetParent(cameraTransform);
            lightObj.transform.localPosition = positionOffset;
            lightObj.transform.localRotation = Quaternion.identity;
            
            flashlightLight = lightObj.AddComponent<Light>();
        }
        
        // Işık ayarlarını uygula
        flashlightLight.type = LightType.Spot;
        flashlightLight.intensity = intensity;
        flashlightLight.range = range;
        flashlightLight.spotAngle = spotAngle;
        flashlightLight.innerSpotAngle = innerSpotAngle;
        flashlightLight.color = lightColor;
        flashlightLight.shadows = LightShadows.Soft;
    }

    private void Update()
    {
        // Toggle kontrolü
        if (Input.GetKeyDown(toggleKey) || Input.GetKeyDown(gamepadToggle))
        {
            Toggle();
        }
        
        // Pil sistemi
        if (useBattery)
        {
            HandleBattery();
        }
    }

    private void HandleBattery()
    {
        if (isOn)
        {
            currentBattery -= batteryDrainRate * Time.deltaTime;
            
            if (currentBattery <= 0)
            {
                currentBattery = 0;
                TurnOff();
            }
            
            // Pil azaldıkça ışık sönükleşsin
            float batteryPercent = currentBattery / maxBattery;
            flashlightLight.intensity = intensity * Mathf.Lerp(0.3f, 1f, batteryPercent);
        }
        else
        {
            // Kapalıyken yavaşça şarj ol
            currentBattery = Mathf.Min(maxBattery, currentBattery + batteryRechargeRate * Time.deltaTime);
        }
    }

    public void Toggle()
    {
        isOn = !isOn;
        UpdateLight();
        
        // Ses çal
        AudioClip clip = isOn ? toggleOnSound : toggleOffSound;
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    public void TurnOn()
    {
        if (!isOn)
        {
            isOn = true;
            UpdateLight();
            if (toggleOnSound != null && audioSource != null)
                audioSource.PlayOneShot(toggleOnSound);
        }
    }

    public void TurnOff()
    {
        if (isOn)
        {
            isOn = false;
            UpdateLight();
            if (toggleOffSound != null && audioSource != null)
                audioSource.PlayOneShot(toggleOffSound);
        }
    }

    private void UpdateLight()
    {
        if (flashlightLight != null)
        {
            flashlightLight.enabled = isOn;
        }
    }

    // Public getters
    public bool IsOn => isOn;
    public float BatteryPercent => useBattery ? (currentBattery / maxBattery) : 1f;
    public float CurrentBattery => currentBattery;
    
    public void SetBattery(float amount)
    {
        currentBattery = Mathf.Clamp(amount, 0, maxBattery);
    }
    
    public void AddBattery(float amount)
    {
        currentBattery = Mathf.Min(maxBattery, currentBattery + amount);
    }
}
