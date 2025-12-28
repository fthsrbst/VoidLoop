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
    
    [Header("Görüş Kontrolü")]
    [Tooltip("Oyuncunun kamerasından bakış açısı kontrolü için")]
    [SerializeField] private float viewAngle = 60f;
    [Tooltip("Görünürlük için maksimum mesafe (0 = sınırsız)")]
    [SerializeField] private float maxViewDistance = 0f;
    [Tooltip("Görünürlük kontrolü için raycast kullan")]
    [SerializeField] private bool useLineOfSight = true;
    [Tooltip("Raycast için engel katmanları")]
    [SerializeField] private LayerMask obstacleLayer = ~0;
    
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
    [SerializeField] private bool showDebugInfo = false;
    
    // Private değişkenler
    private Transform player;
    private Camera playerCamera;
    private NavMeshAgent navAgent;
    private bool isVisible = false;
    private bool wasVisible = false;
    private bool hasCaughtPlayer = false;
    private float nextCheckTime;
    
    // Görünürlük kontrolü için referans noktaları
    private Vector3[] checkPoints;

    private void Start()
    {
        FindPlayer();
        SetupNavMesh();
        SetupCheckPoints();
    }

    private void FindPlayer()
    {
        // Tag ile bul
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            
            // Kamerayı bul - önce çocuk olarak ara
            playerCamera = playerObj.GetComponentInChildren<Camera>();
            
            // Bulamazsa ana kamerayı kullan
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }
            
            if (showDebugInfo)
                Debug.Log($"[WeepingEnemy] Player bulundu: {playerObj.name}, Kamera: {playerCamera?.name}");
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
            
            if (showDebugInfo)
                Debug.Log($"[WeepingEnemy] Player bulundu: {pc.name}");
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
                navAgent.isStopped = true; // Başlangıçta durgun
            }
        }
    }

    private void SetupCheckPoints()
    {
        // Düşmanın farklı noktalarından görünürlük kontrolü
        // (merkezden ve kenarlardan)
        checkPoints = new Vector3[]
        {
            Vector3.zero,                    // Merkez
            Vector3.up * 0.5f,               // Üst
            Vector3.up * -0.5f,              // Alt
            Vector3.right * 0.3f,            // Sağ
            Vector3.left * 0.3f              // Sol
        };
    }

    private void Update()
    {
        if (player == null || playerCamera == null || hasCaughtPlayer) return;
        
        // Görünürlük kontrolü
        wasVisible = isVisible;
        isVisible = CheckVisibility();
        
        // Durum değişimi kontrolü
        if (isVisible != wasVisible)
        {
            OnVisibilityChanged(isVisible);
        }
        
        // Görünmüyorsa hareket et
        if (!isVisible)
        {
            MoveTowardsPlayer();
            CheckPlayerContact();
        }
        else
        {
            StopMovement();
        }
        
        UpdateAnimation();
    }

    /// <summary>
    /// Düşmanın oyuncunun görüş alanında olup olmadığını kontrol eder
    /// </summary>
    private bool CheckVisibility()
    {
        if (playerCamera == null) return false;
        
        Vector3 directionToEnemy = transform.position - playerCamera.transform.position;
        float distanceToEnemy = directionToEnemy.magnitude;
        
        // Mesafe kontrolü
        if (maxViewDistance > 0 && distanceToEnemy > maxViewDistance)
        {
            return false;
        }
        
        // Açı kontrolü - kameranın baktığı yöne göre
        float angle = Vector3.Angle(playerCamera.transform.forward, directionToEnemy);
        
        if (angle > viewAngle)
        {
            if (showDebugInfo && Time.time >= nextCheckTime)
            {
                nextCheckTime = Time.time + 0.5f;
                Debug.Log($"[WeepingEnemy] Görüş açısı dışında: {angle:F1}° > {viewAngle}°");
            }
            return false;
        }
        
        // Frustum (kamera görüş alanı) kontrolü
        Vector3 viewportPoint = playerCamera.WorldToViewportPoint(transform.position);
        bool inFrustum = viewportPoint.x > 0 && viewportPoint.x < 1 &&
                         viewportPoint.y > 0 && viewportPoint.y < 1 &&
                         viewportPoint.z > 0;
        
        if (!inFrustum)
        {
            return false;
        }
        
        // Engel kontrolü (Line of Sight)
        if (useLineOfSight)
        {
            // Birden fazla noktadan kontrol et
            foreach (Vector3 offset in checkPoints)
            {
                Vector3 checkPosition = transform.position + transform.TransformDirection(offset);
                Vector3 rayDirection = (checkPosition - playerCamera.transform.position).normalized;
                float rayDistance = Vector3.Distance(playerCamera.transform.position, checkPosition);
                
                if (!Physics.Raycast(playerCamera.transform.position, rayDirection, rayDistance, obstacleLayer))
                {
                    // En az bir nokta görünüyor
                    if (showDebugInfo && Time.time >= nextCheckTime)
                    {
                        nextCheckTime = Time.time + 0.5f;
                        Debug.Log($"[WeepingEnemy] GÖRÜNÜR - Açı: {angle:F1}°, Mesafe: {distanceToEnemy:F1}m");
                    }
                    return true;
                }
            }
            
            // Tüm noktalar engellendi
            return false;
        }
        
        return true;
    }

    /// <summary>
    /// Görünürlük durumu değiştiğinde çağrılır
    /// </summary>
    private void OnVisibilityChanged(bool nowVisible)
    {
        if (nowVisible)
        {
            // Dondu!
            if (showDebugInfo)
                Debug.Log("[WeepingEnemy] DONDU - Oyuncu bakıyor!");
            
            // Donma sesi
            if (audioSource != null && freezeSound != null)
            {
                audioSource.Stop();
                audioSource.PlayOneShot(freezeSound);
            }
            else if (audioSource != null)
            {
                audioSource.Stop();
            }
        }
        else
        {
            // Hareket etmeye başladı!
            if (showDebugInfo)
                Debug.Log("[WeepingEnemy] HAREKET - Oyuncu bakmıyor!");
            
            // Hareket sesi
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
            // Basit hareket
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
        
        if (showDebugInfo && Time.time >= nextCheckTime)
        {
            nextCheckTime = Time.time + 0.5f;
            Debug.Log($"[WeepingEnemy] Oyuncuya mesafe: {distance:F2}m (Yakalama: {damageRadius}m)");
        }
        
        if (distance <= damageRadius)
        {
            CatchPlayer();
        }
    }

    private void CatchPlayer()
    {
        if (hasCaughtPlayer) return;
        hasCaughtPlayer = true;
        
        Debug.Log("[WeepingEnemy] Oyuncu yakalandı!");
        
        // Sesi durdur
        if (audioSource != null)
        {
            audioSource.Stop();
            if (catchSound != null)
            {
                audioSource.PlayOneShot(catchSound);
            }
        }
        
        // Hareketi durdur
        StopMovement();
        
        // Kırmızı blink ile başa dön
        BlinkTransition blink = BlinkTransition.Instance;
        LevelManager levelManager = LevelManager.Instance;
        
        if (blink != null && levelManager != null)
        {
            blink.BlinkError(() => {
                levelManager.ResetGame();
            });
        }
        else if (levelManager != null)
        {
            levelManager.ResetGame();
        }
        else
        {
            Debug.LogError("[WeepingEnemy] LevelManager bulunamadı!");
        }
    }

    private void UpdateAnimation()
    {
        if (animator == null) return;
        
        float speed = 0f;
        
        if (!isVisible)
        {
            if (useNavMesh && navAgent != null)
            {
                speed = navAgent.velocity.magnitude;
            }
            else
            {
                speed = moveSpeed;
            }
        }
        
        animator.SetBool(moveAnimParam, speed > 0.1f);
        animator.SetFloat(speedAnimParam, speed);
        animator.SetBool(frozenAnimParam, isVisible);
    }

    // Trigger ile temas kontrolü
    private void OnTriggerEnter(Collider other)
    {
        if (hasCaughtPlayer || isVisible) return;
        
        bool isPlayer = other.CompareTag("Player") || 
                       other.GetComponent<PlayerController>() != null ||
                       other.GetComponentInParent<PlayerController>() != null;
        
        if (isPlayer)
        {
            CatchPlayer();
        }
    }
    
    // Collision ile temas kontrolü
    private void OnCollisionEnter(Collision collision)
    {
        if (hasCaughtPlayer || isVisible) return;
        
        bool isPlayer = collision.gameObject.CompareTag("Player") || 
                       collision.gameObject.GetComponent<PlayerController>() != null ||
                       collision.gameObject.GetComponentInParent<PlayerController>() != null;
        
        if (isPlayer)
        {
            CatchPlayer();
        }
    }

    // Public metodlar
    
    /// <summary>
    /// Düşmanın şu anda donmuş olup olmadığını döndürür
    /// </summary>
    public bool IsFrozen()
    {
        return isVisible;
    }
    
    /// <summary>
    /// Düşmanın hareket hızını değiştirir
    /// </summary>
    public void SetMoveSpeed(float speed)
    {
        moveSpeed = speed;
        if (navAgent != null)
        {
            navAgent.speed = speed;
        }
    }

    // Debug görselleştirme
    private void OnDrawGizmosSelected()
    {
        // Hasar yarıçapı
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, damageRadius);
        
        // Görüş açısını çiz
        if (playerCamera != null)
        {
            Gizmos.color = isVisible ? Color.green : Color.cyan;
            Vector3 cameraPos = playerCamera.transform.position;
            Vector3 cameraForward = playerCamera.transform.forward;
            
            // Görüş çizgisi
            Gizmos.DrawLine(cameraPos, transform.position);
        }
        
        // Görüş mesafesi
        if (maxViewDistance > 0)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, maxViewDistance);
        }
    }
    
    private void OnDrawGizmos()
    {
        // Runtime'da durumu göster
        if (Application.isPlaying && showDebugInfo)
        {
            Gizmos.color = isVisible ? Color.green : Color.red;
            Gizmos.DrawSphere(transform.position + Vector3.up * 2f, 0.3f);
        }
    }
}
