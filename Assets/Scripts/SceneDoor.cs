using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Sahne bazlı sistem için kapı kontrolcüsü.
/// Oyuncu içinden geçince otomatik olarak seçim yapılır.
/// </summary>
public class SceneDoor : MonoBehaviour
{
    public enum DoorType
    {
        Normal,     // İleri git (anomali yoksa doğru)
        Anomaly     // Geri dön (anomali varsa doğru)
    }

    [Header("Kapı Ayarları")]
    [SerializeField] private DoorType doorType = DoorType.Normal;

    [Header("Görsel/Ses (Opsiyonel)")]
    [SerializeField] private Animator doorAnimator;
    [SerializeField] private string openTrigger = "Open";
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip enterSound;
    [SerializeField] private AudioClip wrongChoiceSound;

    [Header("Events")]
    public UnityEvent OnDoorEntered;
    public UnityEvent OnCorrectChoice;
    public UnityEvent OnWrongChoice;

    private bool hasBeenUsed = false;

    public DoorType GetDoorType() => doorType;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[SceneDoor] Trigger: {other.name}, Tag: {other.tag}, hasBeenUsed: {hasBeenUsed}");
        
        // Player tespiti - tag veya component ile
        bool isPlayer = other.CompareTag("Player") || 
                       other.GetComponent<PlayerController>() != null ||
                       other.GetComponentInParent<PlayerController>() != null;
        
        if (isPlayer && !hasBeenUsed)
        {
            Debug.Log("[SceneDoor] Player tespit edildi, kapı kullanılıyor...");
            UseDoor();
        }
    }

    private void UseDoor()
    {
        if (hasBeenUsed) return;
        hasBeenUsed = true;

        // Animasyon
        if (doorAnimator != null)
        {
            doorAnimator.SetTrigger(openTrigger);
        }

        // Ses
        PlaySound(enterSound);
        
        OnDoorEntered?.Invoke();

        // LevelManager'a bildir
        LevelManager levelManager = LevelManager.Instance;
        if (levelManager != null)
        {
            bool choseAnomalyDoor = doorType == DoorType.Anomaly;
            
            // Seçimin doğru olup olmadığını kontrol et
            bool isCorrect = (levelManager.IsAnomalyScene && choseAnomalyDoor) || 
                            (!levelManager.IsAnomalyScene && !choseAnomalyDoor);
            
            if (isCorrect)
            {
                OnCorrectChoice?.Invoke();
            }
            else
            {
                OnWrongChoice?.Invoke();
                PlaySound(wrongChoiceSound);
            }
            
            // Kısa bir gecikme sonra sahne geçişi
            Invoke(nameof(NotifyLevelManager), 0.3f);
        }
        else
        {
            Debug.LogError("SceneDoor: LevelManager bulunamadı! LevelManager'ın ilk sahnede olduğundan emin olun.");
        }
    }

    private void NotifyLevelManager()
    {
        LevelManager.Instance?.ProcessDoorChoice(doorType == DoorType.Anomaly);
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    // Sahne yüklendiğinde sıfırla
    private void OnEnable()
    {
        hasBeenUsed = false;
    }
}

