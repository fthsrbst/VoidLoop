using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Oyuncuyu kovalayan düşman NPC.
/// Oyuncuya değdiğinde kırmızı blink ile oyunu sıfırlar.
/// NavMeshAgent veya basit transform hareketi kullanır.
/// </summary>
public class ChasingEnemy : MonoBehaviour
{
    [Header("Hareket Ayarları")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private bool useNavMesh = true;
    
    [Header("Algılama")]
    [Tooltip("Oyuncuyu algılama mesafesi (0 = sınırsız)")]
    [SerializeField] private float detectionRange = 0f;
    [SerializeField] private bool alwaysChase = true;
    
    [Header("Temas Ayarları")]
    [SerializeField] private float damageRadius = 2f;
    [SerializeField] private float checkInterval = 0.05f;
    
    [Header("Görsel/Ses")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip chaseSound;
    [SerializeField] private AudioClip catchSound;
    [SerializeField] private bool loopChaseSound = true;
    
    [Header("Animasyon (Opsiyonel)")]
    [SerializeField] private Animator animator;
    [SerializeField] private string moveAnimParam = "IsMoving";
    [SerializeField] private string speedAnimParam = "Speed";
    
    private Transform player;
    private NavMeshAgent navAgent;
    private CharacterController playerController;
    private bool isChasing = false;
    private bool hasCaughtPlayer = false;
    private float nextCheckTime;

    private void Start()
    {
        // Player'ı bul
        FindPlayer();
        
        // NavMeshAgent kontrolü
        if (useNavMesh)
        {
            navAgent = GetComponent<NavMeshAgent>();
            if (navAgent == null)
            {
                Debug.LogWarning("[ChasingEnemy] NavMeshAgent bulunamadı, basit hareket kullanılacak.");
                useNavMesh = false;
            }
            else
            {
                navAgent.speed = moveSpeed;
                navAgent.angularSpeed = rotationSpeed * 100f;
            }
        }
        
        // Kovalamayı başlat
        if (alwaysChase)
        {
            StartChasing();
        }
    }

    private void FindPlayer()
    {
        // Tag ile bul
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerController = playerObj.GetComponent<CharacterController>();
            Debug.Log($"[ChasingEnemy] Player bulundu (Tag): {playerObj.name}");
            return;
        }
        
        // Component ile bul
        PlayerController pc = FindObjectOfType<PlayerController>();
        if (pc != null)
        {
            player = pc.transform;
            playerController = pc.GetComponent<CharacterController>();
            Debug.Log($"[ChasingEnemy] Player bulundu (Component): {pc.name}");
        }
        else
        {
            Debug.LogError("[ChasingEnemy] Player bulunamadı!");
        }
    }

    private void Update()
    {
        if (player == null || hasCaughtPlayer) return;
        
        // Mesafe kontrolü
        if (detectionRange > 0 && !isChasing)
        {
            float distance = Vector3.Distance(transform.position, player.position);
            if (distance <= detectionRange)
            {
                StartChasing();
            }
        }
        
        if (isChasing)
        {
            ChasePlayer();
            CheckPlayerContact();
        }
        
        UpdateAnimation();
    }

    private void StartChasing()
    {
        isChasing = true;
        
        // Kovalama sesi
        if (audioSource != null && chaseSound != null)
        {
            audioSource.clip = chaseSound;
            audioSource.loop = loopChaseSound;
            audioSource.Play();
        }
        
        Debug.Log("[ChasingEnemy] Kovalama başladı!");
    }

    private void ChasePlayer()
    {
        if (useNavMesh && navAgent != null && navAgent.isOnNavMesh)
        {
            // NavMesh ile hareket
            navAgent.SetDestination(player.position);
        }
        else
        {
            // Basit hareket
            Vector3 direction = (player.position - transform.position).normalized;
            direction.y = 0; // Y ekseninde hareket etme
            
            // Dön
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
            
            // Hareket et
            transform.position += direction * moveSpeed * Time.deltaTime;
        }
    }

    private void CheckPlayerContact()
    {
        // Her frame kontrol yap (interval çok kısa)
        float distance = Vector3.Distance(transform.position, player.position);
        
        // Debug - mesafeyi göster
        if (Time.time >= nextCheckTime)
        {
            nextCheckTime = Time.time + 0.5f;
            Debug.Log($"[ChasingEnemy] Oyuncuya mesafe: {distance:F2}m (Yakalama: {damageRadius}m)");
        }
        
        if (distance <= damageRadius)
        {
            Debug.Log($"[ChasingEnemy] YAKALANDI! Mesafe: {distance:F2}m");
            CatchPlayer();
        }
    }

    private void CatchPlayer()
    {
        if (hasCaughtPlayer) return;
        hasCaughtPlayer = true;
        
        Debug.Log("[ChasingEnemy] Oyuncu yakalandı!");
        
        // Kovalama sesini durdur
        if (audioSource != null)
        {
            audioSource.Stop();
            
            // Yakalama sesi
            if (catchSound != null)
            {
                audioSource.PlayOneShot(catchSound);
            }
        }
        
        // Hareketi durdur
        if (navAgent != null)
        {
            navAgent.isStopped = true;
        }
        
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
            Debug.LogError("[ChasingEnemy] LevelManager bulunamadı!");
        }
    }

    private void UpdateAnimation()
    {
        if (animator == null) return;
        
        float speed = 0f;
        
        if (useNavMesh && navAgent != null)
        {
            speed = navAgent.velocity.magnitude;
        }
        else if (isChasing)
        {
            speed = moveSpeed;
        }
        
        animator.SetBool(moveAnimParam, speed > 0.1f);
        animator.SetFloat(speedAnimParam, speed);
    }

    // Trigger ile de temas kontrolü
    private void OnTriggerEnter(Collider other)
    {
        if (hasCaughtPlayer) return;
        
        bool isPlayer = other.CompareTag("Player") || 
                       other.GetComponent<PlayerController>() != null ||
                       other.GetComponentInParent<PlayerController>() != null;
        
        if (isPlayer)
        {
            CatchPlayer();
        }
    }
    
    // Collision ile de temas kontrolü
    private void OnCollisionEnter(Collision collision)
    {
        if (hasCaughtPlayer) return;
        
        bool isPlayer = collision.gameObject.CompareTag("Player") || 
                       collision.gameObject.GetComponent<PlayerController>() != null ||
                       collision.gameObject.GetComponentInParent<PlayerController>() != null;
        
        if (isPlayer)
        {
            CatchPlayer();
        }
    }

    // Debug görselleştirme
    private void OnDrawGizmosSelected()
    {
        // Hasar yarıçapı
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, damageRadius);
        
        // Algılama mesafesi
        if (detectionRange > 0)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
        }
    }
}
