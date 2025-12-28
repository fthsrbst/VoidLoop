using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// WeepingEnemy için trigger tabanlı spawner.
/// Oyuncu trigger'a girince belirlenen spawn noktasında düşman oluşturur.
/// </summary>
public class WeepingEnemySpawner : MonoBehaviour
{
    [Header("Prefab")]
    [Tooltip("WeepingEnemy prefab'ı")]
    [SerializeField] private GameObject weepingEnemyPrefab;
    
    [Header("Spawn Noktaları")]
    [Tooltip("Spawn noktaları - Birden fazla olabilir")]
    [SerializeField] private Transform[] spawnPoints;
    
    [Tooltip("Spawn noktası seçim modu")]
    [SerializeField] private SpawnMode spawnMode = SpawnMode.Random;
    
    [Tooltip("Spawn yüksekliği offset (yerde gömülü kalmaması için)")]
    [SerializeField] private float spawnHeightOffset = 0.5f;
    
    [Header("Spawn Davranışı")]
    [SerializeField] private bool spawnOnce = true;
    [SerializeField] private float spawnDelay = 0f;
    [Tooltip("Birden fazla düşman spawn edilsin mi?")]
    [SerializeField] private bool spawnMultiple = false;
    [Tooltip("Spawn edilecek düşman sayısı (spawnMultiple açıksa)")]
    [SerializeField] private int spawnCount = 1;
    
    [Header("Oyuncuya Bak")]
    [Tooltip("Spawn olunca oyuncuya doğru dönsün mü?")]
    [SerializeField] private bool lookAtPlayerOnSpawn = true;
    
    [Header("Görsel Efekt (Opsiyonel)")]
    [SerializeField] private GameObject spawnEffectPrefab;
    [SerializeField] private float effectDuration = 2f;
    
    [Header("Ses (Opsiyonel)")]
    [SerializeField] private AudioClip spawnSound;
    [SerializeField] private AudioSource audioSource;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
    
    private bool hasTriggered = false;
    private List<GameObject> spawnedEnemies = new List<GameObject>();
    private Transform player;
    private int currentSpawnIndex = 0;

    public enum SpawnMode
    {
        Random,         // Rastgele spawn noktası
        Sequential,     // Sıralı spawn noktası
        All,            // Tüm noktalarda spawn
        Closest,        // Oyuncuya en yakın nokta
        Farthest        // Oyuncuya en uzak nokta
    }

    private void Start()
    {
        FindPlayer();
    }

    private void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            PlayerController pc = FindObjectOfType<PlayerController>();
            if (pc != null)
            {
                player = pc.transform;
            }
        }
    }

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
        
        if (showDebugInfo)
            Debug.Log("[WeepingEnemySpawner] Trigger tetiklendi!");
        
        if (spawnDelay > 0)
        {
            Invoke(nameof(SpawnEnemies), spawnDelay);
        }
        else
        {
            SpawnEnemies();
        }
    }

    private void SpawnEnemies()
    {
        if (weepingEnemyPrefab == null)
        {
            Debug.LogError("[WeepingEnemySpawner] WeepingEnemy Prefab atanmamış!");
            return;
        }
        
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("[WeepingEnemySpawner] Spawn noktası atanmamış!");
            return;
        }
        
        // Önceki düşmanları temizle
        ClearSpawnedEnemies();
        
        // Spawn moduna göre spawn et
        switch (spawnMode)
        {
            case SpawnMode.All:
                SpawnAtAllPoints();
                break;
            case SpawnMode.Random:
            case SpawnMode.Sequential:
            case SpawnMode.Closest:
            case SpawnMode.Farthest:
            default:
                if (spawnMultiple)
                {
                    for (int i = 0; i < spawnCount; i++)
                    {
                        SpawnAtPoint(GetSpawnPoint());
                    }
                }
                else
                {
                    SpawnAtPoint(GetSpawnPoint());
                }
                break;
        }
        
        // Ses çal
        if (spawnSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(spawnSound);
        }
    }

    private Transform GetSpawnPoint()
    {
        if (spawnPoints.Length == 1)
            return spawnPoints[0];
        
        switch (spawnMode)
        {
            case SpawnMode.Random:
                return spawnPoints[Random.Range(0, spawnPoints.Length)];
                
            case SpawnMode.Sequential:
                Transform point = spawnPoints[currentSpawnIndex];
                currentSpawnIndex = (currentSpawnIndex + 1) % spawnPoints.Length;
                return point;
                
            case SpawnMode.Closest:
                return GetClosestSpawnPoint();
                
            case SpawnMode.Farthest:
                return GetFarthestSpawnPoint();
                
            default:
                return spawnPoints[0];
        }
    }

    private Transform GetClosestSpawnPoint()
    {
        if (player == null) return spawnPoints[0];
        
        Transform closest = spawnPoints[0];
        float minDistance = float.MaxValue;
        
        foreach (Transform point in spawnPoints)
        {
            float distance = Vector3.Distance(player.position, point.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                closest = point;
            }
        }
        
        return closest;
    }

    private Transform GetFarthestSpawnPoint()
    {
        if (player == null) return spawnPoints[0];
        
        Transform farthest = spawnPoints[0];
        float maxDistance = 0f;
        
        foreach (Transform point in spawnPoints)
        {
            float distance = Vector3.Distance(player.position, point.position);
            if (distance > maxDistance)
            {
                maxDistance = distance;
                farthest = point;
            }
        }
        
        return farthest;
    }

    private void SpawnAtAllPoints()
    {
        foreach (Transform point in spawnPoints)
        {
            if (point != null)
            {
                SpawnAtPoint(point);
            }
        }
    }

    private void SpawnAtPoint(Transform point)
    {
        if (point == null) return;
        
        // Rotasyonu hesapla
        Quaternion rotation = point.rotation;
        if (lookAtPlayerOnSpawn && player != null)
        {
            Vector3 lookDirection = player.position - point.position;
            lookDirection.y = 0;
            if (lookDirection != Vector3.zero)
            {
                rotation = Quaternion.LookRotation(lookDirection);
            }
        }
        
        // Spawn pozisyonu (yükseklik offset'i ile)
        Vector3 spawnPosition = point.position + Vector3.up * spawnHeightOffset;
        
        // Düşmanı spawn et
        GameObject enemy = Instantiate(weepingEnemyPrefab, spawnPosition, rotation);
        spawnedEnemies.Add(enemy);
        
        if (showDebugInfo)
            Debug.Log($"[WeepingEnemySpawner] Düşman spawn edildi: {point.position}");
        
        // Efekt spawn et
        if (spawnEffectPrefab != null)
        {
            GameObject effect = Instantiate(spawnEffectPrefab, point.position, Quaternion.identity);
            Destroy(effect, effectDuration);
        }
    }

    private void ClearSpawnedEnemies()
    {
        foreach (GameObject enemy in spawnedEnemies)
        {
            if (enemy != null)
            {
                Destroy(enemy);
            }
        }
        spawnedEnemies.Clear();
    }

    // Sahne yüklendiğinde sıfırla
    private void OnEnable()
    {
        hasTriggered = false;
        currentSpawnIndex = 0;
    }
    
    // Public metodlar
    
    /// <summary>
    /// Spawner'ı manuel olarak tetikle
    /// </summary>
    public void TriggerSpawn()
    {
        if (hasTriggered && spawnOnce) return;
        hasTriggered = true;
        SpawnEnemies();
    }
    
    /// <summary>
    /// Spawn edilen tüm düşmanları temizle
    /// </summary>
    public void ClearEnemies()
    {
        ClearSpawnedEnemies();
    }
    
    /// <summary>
    /// Spawner'ı sıfırla (tekrar tetiklenebilir hale getir)
    /// </summary>
    public void ResetSpawner()
    {
        hasTriggered = false;
        ClearSpawnedEnemies();
    }

    // Debug görselleştirme
    private void OnDrawGizmos()
    {
        // Trigger zone - turuncu
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
            Gizmos.matrix = Matrix4x4.identity;
        }
        
        // Spawn noktaları - kırmızı
        if (spawnPoints != null)
        {
            int index = 0;
            foreach (Transform point in spawnPoints)
            {
                if (point != null)
                {
                    // Nokta
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireSphere(point.position, 0.5f);
                    
                    // Yön oku
                    Gizmos.color = Color.blue;
                    Gizmos.DrawRay(point.position, point.forward * 1.5f);
                    
                    // Bağlantı çizgisi
                    Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
                    Gizmos.DrawLine(transform.position, point.position);
                    
                    index++;
                }
            }
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        // Seçiliyken daha detaylı göster
        if (spawnPoints != null)
        {
            foreach (Transform point in spawnPoints)
            {
                if (point != null)
                {
                    // İç küre
                    Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
                    Gizmos.DrawSphere(point.position, 0.3f);
                }
            }
        }
    }
}
