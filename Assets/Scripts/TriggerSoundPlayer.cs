using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Trigger'a girildiğinde ses çalan script
/// 
/// KULLANIM:
/// 1. Bu scripti Collider olan objeye ekle
/// 2. Collider'ın "Is Trigger" seçeneğini aç
/// 3. Inspector'dan audioClip alanına istediğin sesi ata
/// 4. Player objesine Rigidbody ekli olduğundan emin ol
/// </summary>
public class TriggerSoundPlayer : MonoBehaviour
{
    [Header("═══════════ SES AYARLARI ═══════════")]
    [Tooltip("Çalınacak ses dosyası")]
    [SerializeField] private AudioClip audioClip;
    
    [Tooltip("Ses seviyesi (0-1 arası)")]
    [Range(0f, 1f)]
    [SerializeField] private float volume = 1f;
    
    [Tooltip("Sesin 3D mi yoksa 2D mi olacağı (0 = 2D, 1 = 3D)")]
    [Range(0f, 1f)]
    [SerializeField] private float spatialBlend = 0f;
    
    [Header("═══════════ TETİKLEME AYARLARI ═══════════")]
    [Tooltip("Sadece belirli tag'e sahip objeler tetikleyebilir")]
    [SerializeField] private bool useTagFilter = true;
    
    [Tooltip("Tetikleme yapabilecek objenin tag'i")]
    [SerializeField] private string triggerTag = "Player";
    
    [Tooltip("Trigger'a girildiğinde mi yoksa çıkıldığında mı çalacak?")]
    [SerializeField] private TriggerType triggerType = TriggerType.OnEnter;
    
    [Tooltip("Sadece bir kez çalsın mı?")]
    [SerializeField] private bool playOnlyOnce = true;
    
    [Tooltip("Tekrar çalmadan önce bekleme süresi (saniye)")]
    [SerializeField] private float cooldown = 1f;
    
    [Tooltip("Ses çalmadan önce bekleme süresi (saniye)")]
    [SerializeField] private float delay = 0f;
    
    [Header("═══════════ EVENTS ═══════════")]
    [Tooltip("Ses çaldığında tetiklenecek event")]
    public UnityEvent onSoundPlayed;
    
    public enum TriggerType
    {
        OnEnter,    // Trigger'a girildiğinde
        OnExit,     // Trigger'dan çıkıldığında
        OnStay      // Trigger içindeyken (cooldown ile)
    }
    
    private AudioSource audioSource;
    private bool hasPlayed = false;
    private float lastPlayTime = -100f;
    private bool isInsideTrigger = false;
    
    private void Awake()
    {
        SetupAudioSource();
        ValidateCollider();
    }
    
    private void SetupAudioSource()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = spatialBlend;
        audioSource.volume = volume;
    }
    
    private void ValidateCollider()
    {
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogWarning($"[TriggerSoundPlayer] {gameObject.name} objesinde Collider bulunamadı!");
            return;
        }
        
        if (!col.isTrigger)
        {
            Debug.LogWarning($"[TriggerSoundPlayer] {gameObject.name} Collider'ı trigger değil! 'Is Trigger' açılmalı.");
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (triggerType != TriggerType.OnEnter) return;
        if (!IsValidTarget(other)) return;
        
        TryPlaySound();
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (triggerType == TriggerType.OnExit)
        {
            if (!IsValidTarget(other)) return;
            TryPlaySound();
        }
        
        if (other.CompareTag(triggerTag))
        {
            isInsideTrigger = false;
        }
    }
    
    private void OnTriggerStay(Collider other)
    {
        if (triggerType != TriggerType.OnStay) return;
        if (!IsValidTarget(other)) return;
        
        isInsideTrigger = true;
        TryPlaySound();
    }
    
    // 2D Trigger desteği
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggerType != TriggerType.OnEnter) return;
        if (!IsValidTarget2D(other)) return;
        
        TryPlaySound();
    }
    
    private void OnTriggerExit2D(Collider2D other)
    {
        if (triggerType == TriggerType.OnExit)
        {
            if (!IsValidTarget2D(other)) return;
            TryPlaySound();
        }
        
        if (other.CompareTag(triggerTag))
        {
            isInsideTrigger = false;
        }
    }
    
    private void OnTriggerStay2D(Collider2D other)
    {
        if (triggerType != TriggerType.OnStay) return;
        if (!IsValidTarget2D(other)) return;
        
        isInsideTrigger = true;
        TryPlaySound();
    }
    
    private bool IsValidTarget(Collider other)
    {
        if (useTagFilter && !other.CompareTag(triggerTag))
            return false;
        
        return true;
    }
    
    private bool IsValidTarget2D(Collider2D other)
    {
        if (useTagFilter && !other.CompareTag(triggerTag))
            return false;
        
        return true;
    }
    
    private void TryPlaySound()
    {
        // Sadece bir kez çalma kontrolü
        if (playOnlyOnce && hasPlayed) return;
        
        // Cooldown kontrolü
        if (Time.time - lastPlayTime < cooldown) return;
        
        if (audioClip == null)
        {
            Debug.LogWarning("[TriggerSoundPlayer] AudioClip atanmamış!");
            return;
        }
        
        if (delay > 0)
        {
            StartCoroutine(PlayWithDelay());
        }
        else
        {
            PlaySound();
        }
    }
    
    private System.Collections.IEnumerator PlayWithDelay()
    {
        yield return new WaitForSeconds(delay);
        PlaySound();
    }
    
    private void PlaySound()
    {
        audioSource.PlayOneShot(audioClip, volume);
        hasPlayed = true;
        lastPlayTime = Time.time;
        
        Debug.Log($"[TriggerSoundPlayer] Ses çalındı: {audioClip.name}");
        
        onSoundPlayed?.Invoke();
    }
    
    /// <summary>
    /// Tekrar çalabilmesi için durumu sıfırla
    /// </summary>
    public void ResetTrigger()
    {
        hasPlayed = false;
        lastPlayTime = -100f;
    }
    
    /// <summary>
    /// Manuel olarak sesi çal
    /// </summary>
    public void PlayManually()
    {
        if (audioClip != null && audioSource != null)
        {
            audioSource.PlayOneShot(audioClip, volume);
            onSoundPlayed?.Invoke();
        }
    }
    
    /// <summary>
    /// Farklı bir ses dosyasını çal
    /// </summary>
    public void PlayClip(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip, volume);
        }
    }
    
    #region Properties
    
    public AudioClip AudioClip
    {
        get => audioClip;
        set => audioClip = value;
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
    
    public bool HasPlayed => hasPlayed;
    
    public bool IsInsideTrigger => isInsideTrigger;
    
    #endregion
}
