using UnityEngine;

/// <summary>
/// First Person el feneri - Main Camera'nın child'ı olarak çalışır.
/// Headbob hareketini kompanse ederek stabil kalır.
/// KURULUM: Flashlight objesini Main Camera'nın child'ı yapın.
/// </summary>
public class Flashlight : MonoBehaviour
{
    [Header("Işık Ayarları")]
    [SerializeField] private Light flashlightLight;
    [SerializeField] private float intensity = 2f;
    [SerializeField] private float range = 25f;
    [SerializeField] private float spotAngle = 45f;
    [SerializeField] private float innerSpotAngle = 25f;
    [SerializeField] private Color lightColor = Color.white;
    
    [Header("Headbob Kompanzasyonu")]
    [Tooltip("Kameranın headbob hareketini ne kadar kompanse etsin (1 = tam kompanze/sabit, 0 = normal)")]
    [Range(0f, 1f)]
    [SerializeField] private float headbobCompensation = 0.7f;
    [Tooltip("Kompanzasyon yumuşatma hızı")]
    [SerializeField] private float compensationSmoothSpeed = 12f;
    
    [Header("Kontrol Ayarları")]
    [SerializeField] private KeyCode toggleKey = KeyCode.F;
    [SerializeField] private KeyCode gamepadToggle = KeyCode.JoystickButton3;
    [SerializeField] private bool startsOn = true;
    
    [Header("Ses Efektleri")]
    [SerializeField] private AudioClip toggleOnSound;
    [SerializeField] private AudioClip toggleOffSound;
    
    [Header("Pil Sistemi (Opsiyonel)")]
    [SerializeField] private bool useBattery = false;
    [SerializeField] private float maxBattery = 100f;
    [SerializeField] private float batteryDrainRate = 1f;
    [SerializeField] private float batteryRechargeRate = 0.5f;
    
    private AudioSource audioSource;
    private bool isOn;
    private float currentBattery;
    
    // Headbob kompanzasyonu için
    private Transform parentCamera;
    private float baseCameraLocalY;
    private float smoothedCompensationY;
    private Vector3 startLocalPosition;

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
        // Parent'ın kamera olduğunu varsay
        parentCamera = transform.parent;
        
        if (parentCamera != null)
        {
            baseCameraLocalY = parentCamera.localPosition.y;
        }
        
        // Başlangıç local pozisyonunu kaydet
        startLocalPosition = transform.localPosition;
        smoothedCompensationY = 0f;
        
        SetupLight();
        
        currentBattery = maxBattery;
        isOn = startsOn;
        UpdateLight();
    }

    private void SetupLight()
    {
        if (flashlightLight == null)
        {
            flashlightLight = GetComponentInChildren<Light>();
            
            if (flashlightLight == null)
            {
                GameObject lightObj = new GameObject("FlashlightSpotlight");
                lightObj.transform.SetParent(transform);
                lightObj.transform.localPosition = Vector3.zero;
                lightObj.transform.localRotation = Quaternion.identity;
                flashlightLight = lightObj.AddComponent<Light>();
            }
        }
        
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
        if (Input.GetKeyDown(toggleKey) || Input.GetKeyDown(gamepadToggle))
        {
            Toggle();
        }
        
        if (useBattery)
        {
            HandleBattery();
        }
    }
    
    private void LateUpdate()
    {
        // Sadece headbob kompanzasyonu yap - kendi local Y pozisyonunu ayarla
        if (parentCamera == null) return;
        
        // Kameranın şu anki Y pozisyonu ile başlangıç pozisyonu arasındaki fark = headbob
        float currentCameraY = parentCamera.localPosition.y;
        float headbobOffset = currentCameraY - baseCameraLocalY;
        
        // Hedef kompanzasyon (ters yönde, local space'de)
        float targetCompensationY = -headbobOffset * headbobCompensation;
        
        // Yumuşak geçiş
        smoothedCompensationY = Mathf.Lerp(smoothedCompensationY, targetCompensationY, compensationSmoothSpeed * Time.deltaTime);
        
        // Sadece kendi local Y pozisyonunu değiştir
        Vector3 newLocalPos = startLocalPosition;
        newLocalPos.y += smoothedCompensationY;
        transform.localPosition = newLocalPos;
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
            float batteryPercent = currentBattery / maxBattery;
            flashlightLight.intensity = intensity * Mathf.Lerp(0.3f, 1f, batteryPercent);
        }
        else
        {
            currentBattery = Mathf.Min(maxBattery, currentBattery + batteryRechargeRate * Time.deltaTime);
        }
    }

    public void Toggle()
    {
        isOn = !isOn;
        UpdateLight();
        AudioClip clip = isOn ? toggleOnSound : toggleOffSound;
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);
    }

    public void TurnOn()
    {
        if (!isOn) { isOn = true; UpdateLight(); }
    }

    public void TurnOff()
    {
        if (isOn) { isOn = false; UpdateLight(); }
    }

    private void UpdateLight()
    {
        if (flashlightLight != null)
            flashlightLight.enabled = isOn;
    }

    public bool IsOn => isOn;
    public float BatteryPercent => useBattery ? (currentBattery / maxBattery) : 1f;
    public float CurrentBattery => currentBattery;
}
