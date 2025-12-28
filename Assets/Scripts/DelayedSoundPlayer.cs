using UnityEngine;

/// <summary>
/// Belirli bir süre sonra ses çalan basit script
/// 
/// KULLANIM:
/// 1. Bu scripti herhangi bir objeye ekle
/// 2. Inspector'dan audioClip alanına istediğin sesi ata
/// 3. delay değerini ayarla (saniye cinsinden)
/// 4. playOnStart açıksa oyun başladığında otomatik çalar
/// 5. Veya kod ile PlayDelayed() metodunu çağır
/// </summary>
public class DelayedSoundPlayer : MonoBehaviour
{
    [Header("═══════════ SES AYARLARI ═══════════")]
    [Tooltip("Çalınacak ses dosyası")]
    [SerializeField] private AudioClip audioClip;
    
    [Tooltip("Sesin çalınması için beklenecek süre (saniye)")]
    [SerializeField] private float delay = 3f;
    
    [Tooltip("Ses seviyesi (0-1 arası)")]
    [Range(0f, 1f)]
    [SerializeField] private float volume = 1f;
    
    [Tooltip("Oyun başladığında otomatik çalsın mı?")]
    [SerializeField] private bool playOnStart = true;
    
    [Tooltip("Sesin 3D mi yoksa 2D mi olacağı (0 = 2D, 1 = 3D)")]
    [Range(0f, 1f)]
    [SerializeField] private float spatialBlend = 0f;
    
    [Header("═══════════ DÖNGÜ AYARLARI ═══════════")]
    [Tooltip("Ses çaldıktan sonra tekrar çalsın mı?")]
    [SerializeField] private bool loop = false;
    
    [Tooltip("Her döngü arasındaki süre (saniye)")]
    [SerializeField] private float loopInterval = 5f;
    
    [Header("═══════════ DEBUG ═══════════")]
    [SerializeField] private bool showDebugLogs = true;
    
    private AudioSource audioSource;
    private bool isWaiting = false;
    private Coroutine currentCoroutine;
    
    private void Awake()
    {
        SetupAudioSource();
        DebugLog("Awake çağrıldı");
    }
    
    private void Start()
    {
        DebugLog($"Start çağrıldı. playOnStart: {playOnStart}, audioClip: {(audioClip != null ? audioClip.name : "NULL")}");
        
        if (playOnStart)
        {
            PlayDelayed();
        }
    }
    
    private void SetupAudioSource()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            DebugLog("AudioSource oluşturuldu");
        }
        
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = spatialBlend;
        audioSource.volume = volume;
    }
    
    /// <summary>
    /// Belirlenen süre sonra sesi çalar
    /// </summary>
    public void PlayDelayed()
    {
        DebugLog($"PlayDelayed() çağrıldı. delay: {delay}");
        
        if (audioClip == null)
        {
            Debug.LogError("[DelayedSoundPlayer] AudioClip atanmamış! Inspector'dan ses dosyası ekleyin.");
            return;
        }
        
        if (audioSource == null)
        {
            Debug.LogError("[DelayedSoundPlayer] AudioSource bulunamadı!");
            SetupAudioSource();
        }
        
        // Eğer zaten bekliyor veya çalıyorsa, önce durdur
        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
        }
        
        isWaiting = true;
        currentCoroutine = StartCoroutine(PlayAfterDelay());
    }
    
    /// <summary>
    /// Özel bir süre belirterek sesi çalar
    /// </summary>
    public void PlayDelayed(float customDelay)
    {
        DebugLog($"PlayDelayed({customDelay}) çağrıldı");
        
        if (audioClip == null)
        {
            Debug.LogError("[DelayedSoundPlayer] AudioClip atanmamış!");
            return;
        }
        
        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
        }
        
        isWaiting = true;
        currentCoroutine = StartCoroutine(PlayAfterDelay(customDelay));
    }
    
    /// <summary>
    /// Farklı bir ses dosyasını belirli süre sonra çalar
    /// </summary>
    public void PlayDelayed(AudioClip clip, float customDelay)
    {
        DebugLog($"PlayDelayed(clip, {customDelay}) çağrıldı");
        
        if (clip == null)
        {
            Debug.LogError("[DelayedSoundPlayer] AudioClip null!");
            return;
        }
        
        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
        }
        
        isWaiting = true;
        currentCoroutine = StartCoroutine(PlayAfterDelay(clip, customDelay));
    }
    
    /// <summary>
    /// Hemen çal (gecikme olmadan)
    /// </summary>
    public void PlayNow()
    {
        DebugLog("PlayNow() çağrıldı");
        PlaySound();
    }
    
    /// <summary>
    /// Çalmayı durdur
    /// </summary>
    public void Stop()
    {
        DebugLog("Stop() çağrıldı");
        
        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
            currentCoroutine = null;
        }
        
        isWaiting = false;
        
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }
    
    private System.Collections.IEnumerator PlayAfterDelay()
    {
        DebugLog($"{delay} saniye bekleniyor...");
        
        yield return new WaitForSeconds(delay);
        
        DebugLog("Bekleme bitti, ses çalınıyor...");
        PlaySound();
        
        if (loop)
        {
            while (true)
            {
                DebugLog($"Loop: {loopInterval} saniye bekleniyor...");
                yield return new WaitForSeconds(loopInterval);
                PlaySound();
            }
        }
        else
        {
            isWaiting = false;
            currentCoroutine = null;
        }
    }
    
    private System.Collections.IEnumerator PlayAfterDelay(float customDelay)
    {
        DebugLog($"{customDelay} saniye bekleniyor...");
        
        yield return new WaitForSeconds(customDelay);
        
        PlaySound();
        
        if (loop)
        {
            while (true)
            {
                yield return new WaitForSeconds(loopInterval);
                PlaySound();
            }
        }
        else
        {
            isWaiting = false;
            currentCoroutine = null;
        }
    }
    
    private System.Collections.IEnumerator PlayAfterDelay(AudioClip clip, float customDelay)
    {
        yield return new WaitForSeconds(customDelay);
        
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip, volume);
            DebugLog($"Ses çalındı: {clip.name}");
        }
        
        isWaiting = false;
        currentCoroutine = null;
    }
    
    private void PlaySound()
    {
        if (audioClip == null)
        {
            Debug.LogError("[DelayedSoundPlayer] AudioClip NULL! Ses çalınamadı.");
            return;
        }
        
        if (audioSource == null)
        {
            Debug.LogError("[DelayedSoundPlayer] AudioSource NULL! Ses çalınamadı.");
            return;
        }
        
        audioSource.PlayOneShot(audioClip, volume);
        DebugLog($"✓ Ses çalındı: {audioClip.name} (volume: {volume})");
    }
    
    private void DebugLog(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[DelayedSoundPlayer - {gameObject.name}] {message}");
        }
    }
    
    #region Properties
    
    public float Delay
    {
        get => delay;
        set => delay = Mathf.Max(0f, value);
    }
    
    public float Volume
    {
        get => volume;
        set
        {
            volume = Mathf.Clamp01(value);
            if (audioSource != null)
                audioSource.volume = volume;
        }
    }
    
    public AudioClip AudioClip
    {
        get => audioClip;
        set => audioClip = value;
    }
    
    public bool IsWaiting => isWaiting;
    
    #endregion
    
    // Inspector'dan test butonu
    [ContextMenu("Test - Şimdi Çal")]
    private void TestPlayNow()
    {
        PlayNow();
    }
    
    [ContextMenu("Test - Gecikmeli Çal")]
    private void TestPlayDelayed()
    {
        PlayDelayed();
    }
}
