using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>
/// AAA Kalitesinde El Feneri Sistemi
/// 
/// KURULUM:
/// 1. Bu scripti Player objesine ata
/// 2. Main Camera'yı playerCamera alanına sürükle
/// 3. El feneri modelini flashlightModel alanına sürükle (opsiyonel)
/// 4. Light component'ı otomatik oluşturulur veya lightSource alanına ekle
/// 
/// ÖZELLİKLER:
/// - Gerçekçi ışık kaynağı (light source'dan çıkar)
/// - Headbob ile hareket (yürürken sallanır)
/// - Kamera smooth takip (gecikmeli döner)
/// - Pil sistemi ve gerçekçi titreme
/// </summary>
public class AAA_Flashlight : MonoBehaviour
{
    #region Serialized Fields
    
    [Header("═══════════ TEMEL REFERANSLAR ═══════════")]
    [Tooltip("Player'ın Main Camera'sı")]
    [SerializeField] private Transform playerCamera;
    
    [Tooltip("El feneri modeli (opsiyonel - yoksa sadece ışık kullanılır)")]
    [SerializeField] private Transform flashlightModel;
    
    [Tooltip("Işık kaynağı (boş bırakılırsa otomatik oluşturulur)")]
    [SerializeField] private Transform lightSource;
    
    [Header("═══════════ IŞIK AYARLARI ═══════════")]
    [SerializeField] private Light mainSpotlight;
    [SerializeField] private Light fillLight;
    [SerializeField] private Light bulbGlow;
    
    [Space(5)]
    [SerializeField] private float baseIntensity = 3f;
    [SerializeField] private float baseRange = 30f;
    [SerializeField] private float baseSpotAngle = 50f;
    [SerializeField] private float baseInnerAngle = 25f;
    [SerializeField] private Color baseColor = new Color(1f, 0.95f, 0.85f);
    [Tooltip("El feneri ışık dokusu (Cookie)")]
    [SerializeField] private Texture flashlightCookie;
    
    [Header("═══════════ POZİSYON AYARLARI ═══════════")]
    [Tooltip("El fenerinin kameraya göre pozisyonu")]
    [SerializeField] private Vector3 holdPosition = new Vector3(0.3f, -0.25f, 0.4f);
    
    [Tooltip("Işığın fener modelinden ne kadar önde başlayacağı")]
    [SerializeField] private Vector3 lightOffset = new Vector3(0f, 0f, 0.15f);
    
    [Header("═══════════ SMOOTH TAKİP ═══════════")]
    [Tooltip("Kamerayı ne kadar hızlı takip etsin (düşük = daha gecikmeli)")]
    [SerializeField] private float rotationSmoothSpeed = 8f;
    
    [Tooltip("Pozisyon takip hızı")]
    [SerializeField] private float positionSmoothSpeed = 12f;
    
    [Tooltip("Maksimum rotasyon gecikmesi (derece)")]
    [SerializeField] private float maxRotationLag = 15f;
    
    [Header("═══════════ HEADBOB ETKİSİ ═══════════")]
    [Tooltip("Headbob'dan ne kadar etkilensin (0 = hiç, 1 = tam)")]
    [Range(0f, 1f)]
    [SerializeField] private float headbobInfluence = 0.6f;
    
    [Tooltip("Headbob yumuşatma hızı")]
    [SerializeField] private float headbobSmoothSpeed = 10f;
    
    [Tooltip("Ekstra sallanma miktarı (yürürken)")]
    [SerializeField] private float swayAmount = 0.02f;
    
    [Tooltip("Sallanma hızı")]
    [SerializeField] private float swaySpeed = 1.5f;
    
    [Header("═══════════ PİL SİSTEMİ ═══════════")]
    [SerializeField] private bool useBattery = true;
    [SerializeField] private float maxBattery = 100f;
    [SerializeField] private float batteryDrainRate = 2f;
    [Tooltip("Bu yüzdenin altında kritik mod aktif olur (0-1 arası)")]
    [Range(0f, 1f)]
    [SerializeField] private float criticalBatteryThreshold = 0.2f;
    [Tooltip("Bu yüzdenin altında düşük pil modu aktif olur (0-1 arası)")]
    [Range(0f, 1f)]
    [SerializeField] private float lowBatteryThreshold = 0.4f;
    [Tooltip("Saniye başına pil şarj olma miktarı")]
    [SerializeField] private float rechargeRate = 5f;
    
    [Header("═══════════ TİTREME (FLICKER) ═══════════")]
    [Tooltip("Pil düşükken titreme aktif olsun mu?")]
    [SerializeField] private bool enableFlicker = true;
    [Tooltip("Titreme sırasında minimum ışık yoğunluğu")]
    [Range(0f, 1f)]
    [SerializeField] private float flickerMinIntensity = 0.3f;
    [Tooltip("Kritik durumda minimum ışık yoğunluğu")]
    [Range(0f, 1f)]
    [SerializeField] private float criticalFlickerIntensity = 0.1f;
    
    [Header("═══════════ GEÇİŞ ANİMASYONLARI ═══════════")]
    [SerializeField] private float turnOnSpeed = 8f;
    [SerializeField] private float turnOffSpeed = 12f;
    [SerializeField] private AnimationCurve turnOnCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve turnOffCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    
    [Header("═══════════ LENS FLARE & EFEKTLER ═══════════")]
    [SerializeField] private LensFlareComponentSRP lensFlare;
    [SerializeField] private float baseLensFlareIntensity = 1f;
    [SerializeField] private MeshRenderer bulbMeshRenderer;
    [SerializeField] private string emissionPropertyName = "_EmissionColor";
    [SerializeField] private float emissionIntensity = 5f;
    
    [Header("═══════════ KONTROL AYARLARI ═══════════")]
    [Tooltip("Kendi input handling'ini kullan (PlayerController kullanıyorsan kapat)")]
    [SerializeField] private bool handleInputInternally = false;
    [SerializeField] private KeyCode toggleKey = KeyCode.F;
    [SerializeField] private KeyCode gamepadToggle = KeyCode.JoystickButton3;
    [SerializeField] private bool startsOn = false;
    
    [Header("═══════════ SES EFEKTLERİ ═══════════")]
    [SerializeField] private AudioClip turnOnSound;
    [SerializeField] private AudioClip turnOffSound;
    [SerializeField] private AudioClip flickerSound;
    [SerializeField] private AudioClip lowBatteryBeep;
    [SerializeField] private float flickerSoundVolume = 0.3f;

    [Header("═══════════ UI SİSTEMİ ═══════════")]
    [Tooltip("Pil seviyesini gösterecek obje (Image veya 3D Object olabilir)")]
    [SerializeField] private Transform batteryFillObject;
    
    #endregion
    
    #region Private Variables
    
    private AudioSource audioSource;
    private bool isOn;
    private float currentBattery;
    private float currentIntensityMultiplier = 1f;
    private float targetIntensityMultiplier = 1f;
    private float transitionProgress = 0f;
    private bool isTransitioning = false;
    private bool transitionDirection;
    
    // Smooth takip
    private Vector3 currentPosition;
    private Quaternion currentRotation;
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private Vector3 smoothVelocity;
    
    // Headbob tracking
    private Vector3 lastCameraPosition;
    private Vector3 cameraVelocity;
    private float headbobOffsetY;
    private float smoothedHeadbobY;
    
    // Sway (sallanma)
    private float swayTimer;
    private Vector3 swayOffset;
    
    // Flicker sistemi
    private float flickerTimer = 0f;
    private float flickerValue = 1f;
    private float nextFlickerTime = 0f;
    private bool isFlickering = false;
    private float flickerDuration = 0f;
    private float flickerTargetIntensity = 1f;
    
    // Perlin noise
    private float noiseOffsetX;
    private float noiseOffsetY;
    
    // Material emission
    private Material bulbMaterial;
    private bool hasEmission = false;
    
    // Low battery timer
    private float lowBatteryBeepTimer = 0f;
    private float lowBatteryBeepInterval = 3f;
    
    // Light source container
    private GameObject lightContainer;

    // UI Cache
    private Image batteryFillImage;
    private Vector3 initialFillScale;
    
    #endregion
    
    #region Unity Lifecycle
    
    private void Awake()
    {
        noiseOffsetX = Random.Range(0f, 1000f);
        noiseOffsetY = Random.Range(0f, 1000f);
        
        SetupAudioSource();
        SetupBulbMaterial();
    }

    private void Start()
    {
        // Kamera referansını bul
        if (playerCamera == null)
        {
            playerCamera = Camera.main?.transform;
            if (playerCamera == null)
            {
                Debug.LogError("[AAA_Flashlight] Player Camera bulunamadı! Lütfen Inspector'dan atayın.");
                enabled = false;
                return;
            }
        }
        
        // Light container oluştur
        SetupLightContainer();
        SetupLights();
        
        // Başlangıç pozisyonu
        currentPosition = CalculateTargetPosition();
        currentRotation = playerCamera.rotation;
        lastCameraPosition = playerCamera.position;
        
        if (lightContainer != null)
        {
            lightContainer.transform.position = currentPosition;
            lightContainer.transform.rotation = currentRotation;
        }
        
        currentBattery = maxBattery;
        isOn = startsOn;
        
        transitionProgress = startsOn ? 1f : 0f;
        currentIntensityMultiplier = startsOn ? 1f : 0f;
        
        UpdateAllLights();
        
        // UI Setup
        if (batteryFillObject != null)
        {
            batteryFillImage = batteryFillObject.GetComponent<Image>();
            initialFillScale = batteryFillObject.localScale;
        }
    }

    private void Update()
    {
        if (playerCamera == null) return;
        
        HandleInput();
        
        // Pil sistemi
        if (useBattery)
        {
            HandleBattery();
        }
        
        if (isTransitioning)
        {
            HandleTransition();
        }
        
        // Işık yoğunluğu güncelleme
        if (isOn)
        {
            if (enableFlicker && useBattery)
            {
                HandleFlicker();
            }
            else
            {
                // Flicker kapalıysa sadece pil durumuna göre güncelle
                UpdateBatteryIntensity();
            }
        }
        
        UpdateAllLights();
        UpdateBatteryUI();
    }
    
    private void LateUpdate()
    {
        if (playerCamera == null) return;
        
        CalculateHeadbob();
        CalculateSway();
        UpdateFlashlightTransform();
    }
    
    #endregion
    
    #region Setup Methods
    
    private void SetupAudioSource()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f;
            audioSource.playOnAwake = false;
        }
    }
    
    private void SetupBulbMaterial()
    {
        if (bulbMeshRenderer != null)
        {
            bulbMaterial = bulbMeshRenderer.material;
            if (bulbMaterial.HasProperty(emissionPropertyName))
            {
                hasEmission = true;
            }
        }
    }
    
    private void SetupLightContainer()
    {
        // Eğer lightSource atanmışsa onu kullan
        if (lightSource != null)
        {
            lightContainer = lightSource.gameObject;
            return;
        }
        
        // Yoksa yeni oluştur
        lightContainer = new GameObject("FlashlightLightSource");
        lightSource = lightContainer.transform;
        
        // El feneri modeli varsa ona göre pozisyonla
        if (flashlightModel != null)
        {
            lightContainer.transform.SetParent(flashlightModel);
            lightContainer.transform.localPosition = lightOffset;
            lightContainer.transform.localRotation = Quaternion.identity;
        }
    }
    
    private void SetupLights()
    {
        // Ana spotlight
        if (mainSpotlight == null)
        {
            if (lightContainer != null)
                mainSpotlight = lightContainer.GetComponentInChildren<Light>();
            
            if (mainSpotlight == null)
            {
                GameObject lightObj = new GameObject("MainSpotlight");
                lightObj.transform.SetParent(lightContainer != null ? lightContainer.transform : transform);
                lightObj.transform.localPosition = Vector3.zero;
                lightObj.transform.localRotation = Quaternion.identity;
                mainSpotlight = lightObj.AddComponent<Light>();
            }
        }
        
        mainSpotlight.type = LightType.Spot;
        mainSpotlight.intensity = baseIntensity;
        mainSpotlight.range = baseRange;
        mainSpotlight.spotAngle = baseSpotAngle;
        mainSpotlight.innerSpotAngle = baseInnerAngle;
        mainSpotlight.color = baseColor;
        mainSpotlight.shadows = LightShadows.Soft;
        mainSpotlight.shadowStrength = 0.8f;
        mainSpotlight.shadowResolution = LightShadowResolution.High;
        if (flashlightCookie != null)
        {
            mainSpotlight.cookie = flashlightCookie;
        }
        
        // Dolgu ışığı
        if (fillLight != null)
        {
            fillLight.type = LightType.Spot;
            fillLight.intensity = baseIntensity * 0.15f;
            fillLight.range = baseRange * 0.6f;
            fillLight.spotAngle = baseSpotAngle * 1.5f;
            fillLight.color = baseColor;
            fillLight.shadows = LightShadows.None;
        }
        
        // Ampul glow
        if (bulbGlow != null)
        {
            bulbGlow.type = LightType.Point;
            bulbGlow.intensity = baseIntensity * 0.2f;
            bulbGlow.range = 0.5f;
            bulbGlow.color = Color.Lerp(baseColor, Color.yellow, 0.3f);
            bulbGlow.shadows = LightShadows.None;
        }
    }
    
    #endregion
    
    #region Transform Updates
    
    private Vector3 CalculateTargetPosition()
    {
        // Kameranın local space'inde hedef pozisyon
        Vector3 worldPos = playerCamera.position;
        worldPos += playerCamera.right * holdPosition.x;
        worldPos += playerCamera.up * holdPosition.y;
        worldPos += playerCamera.forward * holdPosition.z;
        
        return worldPos;
    }
    
    private void CalculateHeadbob()
    {
        // Prevent Division by Zero when paused (Time.deltaTime is 0)
        if (Time.deltaTime < 0.0001f)
        {
            cameraVelocity = Vector3.zero;
            return;
        }

        // Kamera hareket hızını hesapla
        Vector3 currentCamPos = playerCamera.position;
        cameraVelocity = (currentCamPos - lastCameraPosition) / Time.deltaTime;
        lastCameraPosition = currentCamPos;
        
        // Y ekseni hareketini (headbob) takip et
        float rawHeadbobY = cameraVelocity.y * headbobInfluence * 0.1f;
        smoothedHeadbobY = Mathf.Lerp(smoothedHeadbobY, rawHeadbobY, headbobSmoothSpeed * Time.deltaTime);
        
        headbobOffsetY = smoothedHeadbobY;
    }
    
    private void CalculateSway()
    {
        // Yürürken sallanma efekti
        float horizontalSpeed = new Vector2(cameraVelocity.x, cameraVelocity.z).magnitude;
        
        if (horizontalSpeed > 0.1f)
        {
            swayTimer += Time.deltaTime * swaySpeed * (horizontalSpeed * 0.5f);
            
            swayOffset.x = Mathf.Sin(swayTimer) * swayAmount;
            swayOffset.y = Mathf.Cos(swayTimer * 2f) * swayAmount * 0.5f;
        }
        else
        {
            // Durgunken sway azalsın
            swayOffset = Vector3.Lerp(swayOffset, Vector3.zero, Time.deltaTime * 5f);
        }
    }
    
    private void UpdateFlashlightTransform()
    {
        // Hedef pozisyon ve rotasyon
        targetPosition = CalculateTargetPosition();
        targetPosition += playerCamera.up * headbobOffsetY;
        targetPosition += playerCamera.right * swayOffset.x;
        targetPosition += playerCamera.up * swayOffset.y;
        
        targetRotation = playerCamera.rotation;
        
        // Smooth pozisyon takibi
        float smoothTime = Mathf.Max(0.01f, 1f / Mathf.Max(0.1f, positionSmoothSpeed));
        currentPosition = Vector3.SmoothDamp(currentPosition, targetPosition, ref smoothVelocity, smoothTime);
        
        // Smooth rotasyon takibi (gecikmeli)
        float rotationStep = rotationSmoothSpeed * Time.deltaTime;
        currentRotation = Quaternion.Slerp(currentRotation, targetRotation, rotationStep);
        
        // Maksimum lag sınırı
        float angleDiff = Quaternion.Angle(currentRotation, targetRotation);
        if (angleDiff > maxRotationLag)
        {
            float t = (angleDiff - maxRotationLag) / angleDiff;
            currentRotation = Quaternion.Slerp(currentRotation, targetRotation, t);
        }
        
        // Light source'u güncelle
        if (lightContainer != null)
        {
            lightContainer.transform.position = currentPosition;
            lightContainer.transform.rotation = currentRotation;
        }
        
        // Fener modelini güncelle (varsa)
        if (flashlightModel != null)
        {
            flashlightModel.position = currentPosition;
            flashlightModel.rotation = currentRotation;
        }
    }
    
    #endregion
    
    #region Input Handling
    
    private void HandleInput()
    {
        if (!handleInputInternally) return;
        
        if (Input.GetKeyDown(toggleKey) || Input.GetKeyDown(gamepadToggle))
        {
            Toggle();
        }
    }
    
    #endregion
    
    #region Battery System
    
    private void HandleBattery()
    {
        if (isOn)
        {
            // Pil tüketimi
            currentBattery -= batteryDrainRate * Time.deltaTime;
            
            if (currentBattery <= 0)
            {
                currentBattery = 0;
                ForceTurnOff();
                return;
            }
            
            // Düşük pil uyarısı
            if (BatteryPercent <= lowBatteryThreshold && lowBatteryBeep != null)
            {
                lowBatteryBeepTimer += Time.deltaTime;
                if (lowBatteryBeepTimer >= lowBatteryBeepInterval)
                {
                    audioSource.PlayOneShot(lowBatteryBeep, 0.5f);
                    lowBatteryBeepTimer = 0f;
                    
                    if (BatteryPercent <= criticalBatteryThreshold)
                        lowBatteryBeepInterval = 1.5f;
                }
            }
        }
        else
        {
            // Pil şarjı
            if (currentBattery < maxBattery)
            {
                currentBattery += rechargeRate * Time.deltaTime;
                if (currentBattery > maxBattery) currentBattery = maxBattery;
            }
        }
    }
    
    private void UpdateBatteryIntensity()
    {
        float batteryIntensityFactor;
        
        if (useBattery)
        {
            batteryIntensityFactor = CalculateBatteryIntensityFactor(BatteryPercent);
        }
        else
        {
            batteryIntensityFactor = 1f; // Pil sistemi kapalıysa tam parlaklık
        }
        
        currentIntensityMultiplier = Mathf.Lerp(currentIntensityMultiplier, batteryIntensityFactor, Time.deltaTime * 5f);
    }
    
    #endregion
    
    #region Flicker System
    
    private void HandleFlicker()
    {
        float batteryPercent = BatteryPercent;
        
        // Titreme şansı hesapla - sadece pil düşükse titrer
        float flickerChance = 0f;
        
        if (batteryPercent <= criticalBatteryThreshold)
        {
            // Kritik seviyede ÇOK SIK titreme (Artırıldı)
            flickerChance = Mathf.Lerp(0.4f, 0.9f, 1f - (batteryPercent / criticalBatteryThreshold));
        }
        else if (batteryPercent <= lowBatteryThreshold)
        {
            // Düşük seviyede belirgin titreme (Artırıldı)
            flickerChance = Mathf.Lerp(0.1f, 0.4f, 1f - ((batteryPercent - criticalBatteryThreshold) / (lowBatteryThreshold - criticalBatteryThreshold)));
        }
        // Pil normalse flickerChance = 0, titreme olmaz
        
        flickerTimer += Time.deltaTime;
        
        if (!isFlickering)
        {
            if (flickerTimer >= nextFlickerTime && Random.value < flickerChance)
            {
                StartFlicker(batteryPercent);
            }
        }
        else
        {
            ProcessFlicker();
        }
        
        // Perlin noise ile doğal değişim
        float noiseValue = Mathf.PerlinNoise(Time.time * 0.5f + noiseOffsetX, noiseOffsetY);
        float subtleVariation = Mathf.Lerp(0.97f, 1.03f, noiseValue);
        
        float batteryIntensityFactor = useBattery ? CalculateBatteryIntensityFactor(batteryPercent) : 1f;
        targetIntensityMultiplier = flickerValue * subtleVariation * batteryIntensityFactor;
        
        currentIntensityMultiplier = Mathf.Lerp(currentIntensityMultiplier, targetIntensityMultiplier, Time.deltaTime * 20f);
    }
    
    private void StartFlicker(float batteryPercent)
    {
        isFlickering = true;
        flickerTimer = 0f;
        
        if (batteryPercent <= criticalBatteryThreshold)
        {
            flickerDuration = Random.Range(0.05f, 0.3f);
            flickerTargetIntensity = Random.Range(criticalFlickerIntensity, flickerMinIntensity);
            
            // Bazen tamamen söner gibi olsun
            if (Random.value < 0.2f)
            {
                flickerTargetIntensity = Random.Range(0f, 0.05f);
            }
        }
        else
        {
            flickerDuration = Random.Range(0.02f, 0.1f);
            flickerTargetIntensity = Random.Range(flickerMinIntensity, 0.8f);
        }
        
        if (flickerSound != null && Random.value < 0.3f)
        {
            audioSource.PlayOneShot(flickerSound, flickerSoundVolume);
        }
    }
    
    private void ProcessFlicker()
    {
        float t = flickerTimer / flickerDuration;
        
        if (t < 0.3f)
        {
            flickerValue = Mathf.Lerp(1f, flickerTargetIntensity, t / 0.3f);
        }
        else
        {
            flickerValue = Mathf.Lerp(flickerTargetIntensity, 1f, (t - 0.3f) / 0.7f);
        }
        
        if (flickerTimer >= flickerDuration)
        {
            isFlickering = false;
            flickerValue = 1f;
            
            float batteryPercent = BatteryPercent;
            if (batteryPercent <= criticalBatteryThreshold)
            {
                nextFlickerTime = Random.Range(0.1f, 0.5f);
            }
            else
            {
                nextFlickerTime = Random.Range(0.5f, 2f);
            }
            flickerTimer = 0f;
        }
    }
    
    private float CalculateBatteryIntensityFactor(float batteryPercent)
    {
        if (batteryPercent >= lowBatteryThreshold)
        {
            // Normal durumda tam parlaklık
            return Mathf.Lerp(0.95f, 1f, (batteryPercent - lowBatteryThreshold) / (1f - lowBatteryThreshold));
        }
        else if (batteryPercent >= criticalBatteryThreshold)
        {
            // Düşük pilde parlaklık azalır
            return Mathf.Lerp(0.6f, 0.95f, (batteryPercent - criticalBatteryThreshold) / (lowBatteryThreshold - criticalBatteryThreshold));
        }
        else
        {
            // Kritik seviyede ciddi parlaklık kaybı
            return Mathf.Lerp(0.2f, 0.6f, batteryPercent / criticalBatteryThreshold);
        }
    }
    
    #endregion
    
    #region Transition System
    
    private void HandleTransition()
    {
        float speed = transitionDirection ? turnOnSpeed : turnOffSpeed;
        AnimationCurve curve = transitionDirection ? turnOnCurve : turnOffCurve;
        
        if (transitionDirection)
        {
            transitionProgress += Time.deltaTime * speed;
            if (transitionProgress >= 1f)
            {
                transitionProgress = 1f;
                isTransitioning = false;
            }
        }
        else
        {
            transitionProgress -= Time.deltaTime * speed;
            if (transitionProgress <= 0f)
            {
                transitionProgress = 0f;
                isTransitioning = false;
            }
        }
        
        // Transition sırasında intensity'yi curve'dan al
        if (!isOn || !useBattery)
        {
            currentIntensityMultiplier = curve.Evaluate(transitionProgress);
        }
    }
    
    #endregion
    
    #region Light Updates
    
    private void UpdateAllLights()
    {
        float finalMultiplier = currentIntensityMultiplier * transitionProgress;
        
        // Pil kapalıysa tam parlaklık, açıksa pil durumuna göre renk
        Color currentColor = useBattery ? GetBatteryAdjustedColor() : baseColor;
        
        // Ana spotlight
        if (mainSpotlight != null)
        {
            mainSpotlight.enabled = finalMultiplier > 0.01f;
            mainSpotlight.intensity = baseIntensity * finalMultiplier;
            mainSpotlight.range = baseRange * Mathf.Lerp(0.7f, 1f, finalMultiplier);
            mainSpotlight.spotAngle = baseSpotAngle * Mathf.Lerp(0.85f, 1f, finalMultiplier);
            mainSpotlight.innerSpotAngle = baseInnerAngle * Mathf.Lerp(0.8f, 1f, finalMultiplier);
            mainSpotlight.color = currentColor;
        }
        
        // Dolgu ışığı
        if (fillLight != null)
        {
            fillLight.enabled = finalMultiplier > 0.05f;
            fillLight.intensity = baseIntensity * 0.15f * finalMultiplier;
            fillLight.color = currentColor;
        }
        
        // Ampul glow
        if (bulbGlow != null)
        {
            bulbGlow.enabled = finalMultiplier > 0.01f;
            bulbGlow.intensity = baseIntensity * 0.2f * finalMultiplier;
            bulbGlow.color = currentColor; // Sarı renk karışımını kaldırdım
        }
        
        // Lens flare
        if (lensFlare != null)
        {
            lensFlare.intensity = baseLensFlareIntensity * finalMultiplier;
        }
        
        // Ampul emission
        if (hasEmission && bulbMaterial != null)
        {
            Color emissionColor = currentColor * emissionIntensity * finalMultiplier;
            bulbMaterial.SetColor(emissionPropertyName, emissionColor);
        }
    }
    
    private Color GetBatteryAdjustedColor()
    {
        // Kullanıcı isteği üzerine renk değişimi kapatıldı veya çok aza indirildi
        // Sadece çok kritik seviyede hafif bir sıcaklık ekleyebiliriz veya tamamen iptal edebiliriz.
        // Şimdilik tamamen baseColor dönüyoruz ki "renk değişmesin" isteği tam karşılansın.
        return baseColor;
    }

    private void UpdateBatteryUI()
    {
        if (batteryFillObject == null) return;

        float pct = BatteryPercent;

        if (batteryFillImage != null && batteryFillImage.type == Image.Type.Filled)
        {
            batteryFillImage.fillAmount = pct;
        }
        else
        {
            // Image yoksa veya filled değilse scale kullan (X ekseni)
            Vector3 targetScale = initialFillScale;
            targetScale.x = initialFillScale.x * pct;
            batteryFillObject.localScale = Vector3.Lerp(batteryFillObject.localScale, targetScale, Time.deltaTime * 10f);
        }
    }
    
    #endregion
    
    #region Public Methods
    
    public void Toggle()
    {
        if (isOn) TurnOff();
        else TurnOn();
    }
    
    public void TurnOn()
    {
        if (isOn) return;
        if (useBattery && currentBattery <= 0) return;
        
        isOn = true;
        isTransitioning = true;
        transitionDirection = true;
        
        // Timer'ları sıfırla
        lowBatteryBeepTimer = 0f;
        lowBatteryBeepInterval = 3f;
        
        if (turnOnSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(turnOnSound, 1f);
            Debug.Log("[AAA_Flashlight] Açma sesi çalındı: " + turnOnSound.name);
        }
        else
        {
            Debug.LogWarning("[AAA_Flashlight] Açma sesi çalınamadı! turnOnSound: " + 
                (turnOnSound != null ? turnOnSound.name : "NULL") + 
                ", audioSource: " + (audioSource != null ? "OK" : "NULL"));
        }
    }
    
    public void TurnOff()
    {
        if (!isOn) return;
        
        isOn = false;
        isTransitioning = true;
        transitionDirection = false;
        
        if (turnOffSound != null)
            audioSource.PlayOneShot(turnOffSound);
    }
    
    private void ForceTurnOff()
    {
        isOn = false;
        isTransitioning = true;
        transitionDirection = false;
        
        if (turnOffSound != null)
            audioSource.PlayOneShot(turnOffSound, 0.5f);
    }
    
    public void RechargeBattery(float amount)
    {
        currentBattery = Mathf.Min(maxBattery, currentBattery + amount);
    }
    
    public void FullRecharge()
    {
        currentBattery = maxBattery;
    }
    
    /// <summary>
    /// Işık kaynağının Transform'unu döndürür
    /// </summary>
    public Transform GetLightSourceTransform()
    {
        return lightSource;
    }
    
    #endregion
    
    #region Public Properties
    
    public bool IsOn => isOn;
    public float BatteryPercent => useBattery ? Mathf.Clamp01(currentBattery / maxBattery) : 1f;
    public float CurrentBattery => currentBattery;
    public float MaxBattery => maxBattery;
    public bool IsCriticalBattery => useBattery && BatteryPercent <= criticalBatteryThreshold;
    public bool IsLowBattery => useBattery && BatteryPercent <= lowBatteryThreshold;
    public float CurrentIntensity => currentIntensityMultiplier * baseIntensity;
    
    #endregion
    
    #region Editor Helpers
    
#if UNITY_EDITOR
    private void OnValidate()
    {
        // Threshold değerlerinin mantıklı olduğundan emin ol
        if (criticalBatteryThreshold > lowBatteryThreshold)
        {
            criticalBatteryThreshold = lowBatteryThreshold;
        }
        
        if (Application.isPlaying && mainSpotlight != null)
        {
            mainSpotlight.intensity = baseIntensity;
            mainSpotlight.range = baseRange;
            mainSpotlight.spotAngle = baseSpotAngle;
            mainSpotlight.innerSpotAngle = baseInnerAngle;
            mainSpotlight.color = baseColor;
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        // Işık konisini göster
        Transform lightPos = lightSource != null ? lightSource : transform;
        
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.matrix = Matrix4x4.TRS(lightPos.position, lightPos.rotation, Vector3.one);
        
        float halfAngle = baseSpotAngle * 0.5f * Mathf.Deg2Rad;
        float endRadius = Mathf.Tan(halfAngle) * baseRange;
        
        Gizmos.DrawWireSphere(Vector3.forward * baseRange, endRadius);
        Gizmos.DrawLine(Vector3.zero, new Vector3(endRadius, 0, baseRange));
        Gizmos.DrawLine(Vector3.zero, new Vector3(-endRadius, 0, baseRange));
        Gizmos.DrawLine(Vector3.zero, new Vector3(0, endRadius, baseRange));
        Gizmos.DrawLine(Vector3.zero, new Vector3(0, -endRadius, baseRange));
        
        // Hold pozisyonunu göster
        if (playerCamera != null)
        {
            Gizmos.color = Color.cyan;
            Vector3 holdPos = playerCamera.position + 
                              playerCamera.right * holdPosition.x + 
                              playerCamera.up * holdPosition.y + 
                              playerCamera.forward * holdPosition.z;
            Gizmos.DrawWireSphere(holdPos, 0.05f);
        }
    }
#endif
    
    #endregion
}
