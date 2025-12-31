using UnityEngine;

/// <summary>
/// Anomali efekti: Nesneyi rastgele döndürür ve titretir.
/// Bu scripti herhangi bir GameObject'e ekleyerek kullanın.
/// </summary>
public class AnomalyShake : MonoBehaviour
{
    [Header("Titreşim Ayarları")]
    [Tooltip("Pozisyon titreşim şiddeti")]
    [SerializeField] private float shakeIntensity = 0.05f;
    
    [Tooltip("Titreşim hızı")]
    [SerializeField] private float shakeSpeed = 25f;
    
    [Header("Döndürme Ayarları")]
    [Tooltip("Döndürme hızı (derece/saniye)")]
    [SerializeField] private float rotationSpeed = 50f;
    
    [Tooltip("Rastgele döndürme yönü değiştirme aralığı")]
    [SerializeField] private float directionChangeInterval = 0.5f;
    
    [Tooltip("Hangi eksenlerde dönsün")]
    [SerializeField] private bool rotateX = true;
    [SerializeField] private bool rotateY = true;
    [SerializeField] private bool rotateZ = false;
    
    [Header("Gelişmiş")]
    [Tooltip("Perlin noise kullan (daha organik hareket)")]
    [SerializeField] private bool usePerlinNoise = true;
    
    private Vector3 originalPosition;
    private Vector3 rotationDirection;
    private float directionTimer;
    private float noiseOffsetX;
    private float noiseOffsetY;
    private float noiseOffsetZ;

    private void Start()
    {
        originalPosition = transform.localPosition;
        RandomizeRotationDirection();
        
        // Perlin noise için rastgele offset
        noiseOffsetX = Random.Range(0f, 1000f);
        noiseOffsetY = Random.Range(0f, 1000f);
        noiseOffsetZ = Random.Range(0f, 1000f);
    }

    private void Update()
    {
        // Titreşim
        ApplyShake();
        
        // Döndürme
        ApplyRotation();
        
        // Yön değiştirme
        directionTimer += Time.deltaTime;
        if (directionTimer >= directionChangeInterval)
        {
            directionTimer = 0f;
            RandomizeRotationDirection();
        }
    }

    private void ApplyShake()
    {
        Vector3 shakeOffset;
        
        if (usePerlinNoise)
        {
            float time = Time.time * shakeSpeed;
            shakeOffset = new Vector3(
                (Mathf.PerlinNoise(time, noiseOffsetX) - 0.5f) * 2f,
                (Mathf.PerlinNoise(time, noiseOffsetY) - 0.5f) * 2f,
                (Mathf.PerlinNoise(time, noiseOffsetZ) - 0.5f) * 2f
            ) * shakeIntensity;
        }
        else
        {
            shakeOffset = new Vector3(
                Mathf.Sin(Time.time * shakeSpeed) * shakeIntensity,
                Mathf.Cos(Time.time * shakeSpeed * 1.3f) * shakeIntensity,
                Mathf.Sin(Time.time * shakeSpeed * 0.7f) * shakeIntensity
            );
        }
        
        transform.localPosition = originalPosition + shakeOffset;
    }

    private void ApplyRotation()
    {
        Vector3 rotation = rotationDirection * rotationSpeed * Time.deltaTime;
        transform.Rotate(rotation, Space.Self);
    }

    private void RandomizeRotationDirection()
    {
        rotationDirection = new Vector3(
            rotateX ? Random.Range(-1f, 1f) : 0f,
            rotateY ? Random.Range(-1f, 1f) : 0f,
            rotateZ ? Random.Range(-1f, 1f) : 0f
        ).normalized;
    }

    private void OnDisable()
    {
        // Devre dışı kalınca orijinal pozisyona dön
        transform.localPosition = originalPosition;
    }
}
