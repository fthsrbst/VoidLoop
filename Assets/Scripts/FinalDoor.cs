using UnityEngine;

/// <summary>
/// Final sahnesindeki kapı.
/// Oyuncu bu kapıdan geçince Level 0'a döner ama skor korunur.
/// </summary>
public class FinalDoor : MonoBehaviour
{
    [Header("Görsel/Ses (Opsiyonel)")]
    [SerializeField] private Animator doorAnimator;
    [SerializeField] private string openTrigger = "Open";
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip enterSound;

    private bool hasBeenUsed = false;

    private void OnTriggerEnter(Collider other)
    {
        // Player tespiti
        bool isPlayer = other.CompareTag("Player") || 
                       other.GetComponent<PlayerController>() != null ||
                       other.GetComponentInParent<PlayerController>() != null;
        
        if (isPlayer && !hasBeenUsed)
        {
            Debug.Log("[FinalDoor] Player final kapısına girdi - Level 0'a dönülüyor (skor korunuyor)");
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
        if (audioSource != null && enterSound != null)
        {
            audioSource.PlayOneShot(enterSound);
        }

        // LevelManager'a bildir - skoru koruyarak Level 0'a dön
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.ContinueFromFinal();
        }
        else
        {
            Debug.LogError("[FinalDoor] LevelManager bulunamadı!");
        }
    }

    private void OnEnable()
    {
        hasBeenUsed = false;
    }
}
