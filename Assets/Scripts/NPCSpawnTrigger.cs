using UnityEngine;

/// <summary>
/// Oyuncu bu trigger'dan geçince belirtilen noktada NPC spawn olur.
/// </summary>
public class NPCSpawnTrigger : MonoBehaviour
{
    [Header("Spawn Ayarları")]
    [Tooltip("NPC prefab'ı")]
    [SerializeField] private GameObject npcPrefab;
    
    [Tooltip("Spawn noktası (Empty GameObject)")]
    [SerializeField] private Transform spawnPoint;
    
    [Header("Davranış")]
    [SerializeField] private bool spawnOnce = true;
    [SerializeField] private float spawnDelay = 0f;
    
    [Header("Ses (Opsiyonel)")]
    [SerializeField] private AudioClip spawnSound;
    [SerializeField] private AudioSource audioSource;
    
    private bool hasTriggered = false;
    private GameObject spawnedNPC;

    private void OnTriggerEnter(Collider other)
    {
        // Player kontrolü
        bool isPlayer = other.CompareTag("Player") || 
                       other.GetComponent<PlayerController>() != null ||
                       other.GetComponentInParent<PlayerController>() != null;
        
        if (!isPlayer) return;
        
        // Zaten tetiklendiyse ve tek seferlikse çık
        if (hasTriggered && spawnOnce) return;
        
        hasTriggered = true;
        
        if (spawnDelay > 0)
        {
            Invoke(nameof(SpawnNPC), spawnDelay);
        }
        else
        {
            SpawnNPC();
        }
    }

    private void SpawnNPC()
    {
        if (npcPrefab == null)
        {
            Debug.LogError("[NPCSpawnTrigger] NPC Prefab atanmamış!");
            return;
        }
        
        if (spawnPoint == null)
        {
            Debug.LogError("[NPCSpawnTrigger] Spawn Point atanmamış!");
            return;
        }
        
        // Önceki NPC'yi yok et (varsa)
        if (spawnedNPC != null)
        {
            Destroy(spawnedNPC);
        }
        
        // NPC'yi spawn et
        spawnedNPC = Instantiate(npcPrefab, spawnPoint.position, spawnPoint.rotation);
        
        Debug.Log($"[NPCSpawnTrigger] NPC spawn edildi: {spawnPoint.position}");
        
        // Ses çal
        if (spawnSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(spawnSound);
        }
    }
    
    // Sahne değiştiğinde sıfırla
    private void OnEnable()
    {
        hasTriggered = false;
    }
    
    // Debug görselleştirme
    private void OnDrawGizmos()
    {
        // Trigger zone
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            if (col is BoxCollider box)
            {
                Gizmos.DrawCube(box.center, box.size);
            }
            else if (col is SphereCollider sphere)
            {
                Gizmos.DrawSphere(sphere.center, sphere.radius);
            }
        }
        
        // Spawn point
        if (spawnPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(spawnPoint.position, 0.5f);
            Gizmos.DrawLine(transform.position, spawnPoint.position);
        }
    }
}
