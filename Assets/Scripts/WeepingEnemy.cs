using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// "Weeping Angel" tarzı düşman - Oyuncu baktığında donar, bakmadığında yaklaşır.
/// </summary>
public class WeepingEnemy : MonoBehaviour
{
    [Header("Hareket Ayarları")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 8f;
    [SerializeField] private bool useNavMesh = true;
    [Tooltip("Spawn yüksekliği offset")]
    [SerializeField] private float spawnHeightOffset = 0f;
    
    [Header("Görüş Kontrolü")]
    [Tooltip("Ekran kenarından margin (0.1 = %10)")]
    [SerializeField] [Range(0f, 0.3f)] private float screenMargin = 0.1f;
    [Tooltip("Görünürlük için maksimum mesafe (0 = sınırsız)")]
    [SerializeField] private float maxViewDistance = 0f;
    [Tooltip("Engel kontrolünü devre dışı bırak (test için)")]
    [SerializeField] private bool disableObstacleCheck = true;
    
    [Header("Temas Ayarları")]
    [SerializeField] private float damageRadius = 2f;
    
    [Header("Görsel/Ses")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip moveSound;
    [SerializeField] private AudioClip freezeSound;
    [SerializeField] private AudioClip catchSound;
    [SerializeField] private bool loopMoveSound = true;
    
    [Header("Animasyon (Opsiyonel)")]
    [SerializeField] private Animator animator;
    [SerializeField] private string moveAnimParam = "IsMoving";
    [SerializeField] private string speedAnimParam = "Speed";
    [SerializeField] private string frozenAnimParam = "IsFrozen";
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    
    // Private değişkenler
    private Transform player;
    private Camera playerCamera;
    private NavMeshAgent navAgent;
    private bool isVisible = false;
    private bool wasVisible = false;
    private bool hasCaughtPlayer = false;

    private void Start()
    {
        // Yükseklik düzeltmesi
        if (spawnHeightOffset != 0)
        {
            transform.position += Vector3.up * spawnHeightOffset;
        }
        
        FindPlayer();
        SetupNavMesh();
        
        Debug.Log($"[WeepingEnemy] Başlatıldı! Kamera: {playerCamera?.name}");
    }

    private void FindPlayer()
    {
        // Tag ile bul
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerCamera = playerObj.GetComponentInChildren<Camera>();
            
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }
            
            Debug.Log($"[WeepingEnemy] Player bulundu: {playerObj.name}");
            return;
        }
        
        // Component ile bul
        PlayerController pc = FindObjectOfType<PlayerController>();
        if (pc != null)
        {
            player = pc.transform;
            playerCamera = pc.GetComponentInChildren<Camera>();
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }
        }
        else
        {
            Debug.LogError("[WeepingEnemy] Player bulunamadı!");
        }
    }

    private void SetupNavMesh()
    {
        if (useNavMesh)
        {
            navAgent = GetComponent<NavMeshAgent>();
            if (navAgent == null)
            {
                Debug.LogWarning("[WeepingEnemy] NavMeshAgent bulunamadı, basit hareket kullanılacak.");
                useNavMesh = false;
            }
            else
            {
                navAgent.speed = moveSpeed;
                navAgent.angularSpeed = rotationSpeed * 100f;
                navAgent.isStopped = true;
            }
        }
    }

    private void Update()
    {
        if (player == null || playerCamera == null || hasCaughtPlayer) return;
        
        // Görünürlük kontrolü - BASİT VE GÜVENİLİR
        wasVisible = isVisible;
        isVisible = IsInCameraView();
        
        // Debug
        if (showDebugInfo)
        {
            Debug.Log($"[WeepingEnemy] isVisible={isVisible}, wasVisible={wasVisible}");
        }
        
        // Durum değişimi kontrolü
        if (isVisible != wasVisible)
        {
            OnVisibilityChanged(isVisible);
        }
        
        // Görünmüyorsa hareket et, görünüyorsa dur
        if (isVisible)
        {
            StopMovement();
        }
        else
        {
            MoveTowardsPlayer();
            CheckPlayerContact();
        }
        
        UpdateAnimation();
    }

    /// <summary>
    /// Düşman kameranın görüş alanında mı? (Basit ve güvenilir)
    /// </summary>
    private bool IsInCameraView()
    {
        if (playerCamera == null) return false;
        
        // Düşmanın dünya pozisyonunu viewport koordinatlarına çevir
        Vector3 viewportPos = playerCamera.WorldToViewportPoint(transform.position);
        
        // Viewport:
        // x: 0 = sol kenar, 1 = sağ kenar
        // y: 0 = alt kenar, 1 = üst kenar  
        // z: kameradan mesafe (z > 0 = kameranın önünde)
        
        // Kameranın arkasında mı?
        if (viewportPos.z <= 0)
        {
            if (showDebugInfo) Debug.Log("[WeepingEnemy] Kameranın arkasında - HAREKET");
            return false;
        }
        
        // Mesafe kontrolü
        if (maxViewDistance > 0 && viewportPos.z > maxViewDistance)
        {
            if (showDebugInfo) Debug.Log($"[WeepingEnemy] Çok uzakta: {viewportPos.z:F1}m - HAREKET");
            return false;
        }
        
        // Ekran içinde mi?
        bool inScreen = viewportPos.x > screenMargin && 
                        viewportPos.x < (1f - screenMargin) &&
                        viewportPos.y > screenMargin && 
                        viewportPos.y < (1f - screenMargin);
        
        if (!inScreen)
        {
            if (showDebugInfo) Debug.Log($"[WeepingEnemy] Ekran dışında ({viewportPos.x:F2}, {viewportPos.y:F2}) - HAREKET");
            return false;
        }
        
        // Engel kontrolü (opsiyonel - varsayılan kapalı)
        if (!disableObstacleCheck)
        {
            Vector3 direction = transform.position - playerCamera.transform.position;
            float distance = direction.magnitude;
            
            RaycastHit hit;
            if (Physics.Raycast(playerCamera.transform.position, direction.normalized, out hit, distance))
            {
                // Çarpan obje bu düşman değilse, engel var demek
                if (hit.transform != transform && !hit.transform.IsChildOf(transform))
                {
                    if (showDebugInfo) Debug.Log($"[WeepingEnemy] Engel: {hit.transform.name} - HAREKET");
                    return false;
                }
            }
        }
        
        if (showDebugInfo) Debug.Log("[WeepingEnemy] EKRANDA - DONDUR!");
        return true;
    }

    private void OnVisibilityChanged(bool nowVisible)
    {
        if (nowVisible)
        {
            Debug.Log("[WeepingEnemy] *** DONDU! ***");
            
            if (audioSource != null)
            {
                audioSource.Stop();
                if (freezeSound != null)
                    audioSource.PlayOneShot(freezeSound);
            }
        }
        else
        {
            Debug.Log("[WeepingEnemy] *** HAREKET EDİYOR! ***");
            
            if (audioSource != null && moveSound != null)
            {
                audioSource.clip = moveSound;
                audioSource.loop = loopMoveSound;
                audioSource.Play();
            }
        }
    }

    private void MoveTowardsPlayer()
    {
        if (useNavMesh && navAgent != null && navAgent.isOnNavMesh)
        {
            navAgent.isStopped = false;
            navAgent.SetDestination(player.position);
        }
        else
        {
            Vector3 direction = (player.position - transform.position).normalized;
            direction.y = 0;
            
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
            
            transform.position += direction * moveSpeed * Time.deltaTime;
        }
    }

    private void StopMovement()
    {
        if (useNavMesh && navAgent != null && navAgent.isOnNavMesh)
        {
            navAgent.isStopped = true;
            navAgent.velocity = Vector3.zero;
        }
    }

    private void CheckPlayerContact()
    {
        float distance = Vector3.Distance(transform.position, player.position);
        
        if (distance <= damageRadius)
        {
            CatchPlayer();
        }
    }

    private void CatchPlayer()
    {
        if (hasCaughtPlayer) return;
        hasCaughtPlayer = true;
        
        Debug.Log("[WeepingEnemy] OYUNCU YAKALANDI!");
        
        if (audioSource != null)
        {
            audioSource.Stop();
            if (catchSound != null)
                audioSource.PlayOneShot(catchSound);
        }
        
        StopMovement();
        
        BlinkTransition blink = BlinkTransition.Instance;
        LevelManager levelManager = LevelManager.Instance;
        
        if (blink != null && levelManager != null)
        {
            blink.BlinkError(() => levelManager.ResetGame());
        }
        else if (levelManager != null)
        {
            levelManager.ResetGame();
        }
    }

    private void UpdateAnimation()
    {
        if (animator == null) return;
        
        float speed = 0f;
        
        if (!isVisible)
        {
            speed = useNavMesh && navAgent != null ? navAgent.velocity.magnitude : moveSpeed;
        }
        
        animator.SetBool(moveAnimParam, speed > 0.1f);
        animator.SetFloat(speedAnimParam, speed);
        animator.SetBool(frozenAnimParam, isVisible);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasCaughtPlayer || isVisible) return;
        
        if (other.CompareTag("Player") || 
            other.GetComponent<PlayerController>() != null ||
            other.GetComponentInParent<PlayerController>() != null)
        {
            CatchPlayer();
        }
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        if (hasCaughtPlayer || isVisible) return;
        
        if (collision.gameObject.CompareTag("Player") || 
            collision.gameObject.GetComponent<PlayerController>() != null ||
            collision.gameObject.GetComponentInParent<PlayerController>() != null)
        {
            CatchPlayer();
        }
    }

    public bool IsFrozen() => isVisible;
    
    public void SetMoveSpeed(float speed)
    {
        moveSpeed = speed;
        if (navAgent != null) navAgent.speed = speed;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, damageRadius);
        
        if (playerCamera != null)
        {
            Gizmos.color = isVisible ? Color.green : Color.cyan;
            Gizmos.DrawLine(playerCamera.transform.position, transform.position);
        }
    }
    
    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            // Yeşil = donmuş, Kırmızı = hareket ediyor
            Gizmos.color = isVisible ? Color.green : Color.red;
            Gizmos.DrawSphere(transform.position + Vector3.up * 2.5f, 0.4f);
        }
    }
}
