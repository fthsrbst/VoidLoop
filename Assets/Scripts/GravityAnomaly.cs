using UnityEngine;
using System.Collections;

/// <summary>
/// Yerçekimi Anomalisi: Sahnedeki yerçekimini değiştirir.
/// Rigidbody'leri havaya kaldırır veya yerçekimini tamamen kapatır.
/// </summary>
public class GravityAnomaly : MonoBehaviour
{
    [Header("Yerçekimi Ayarları")]
    [Tooltip("Anomali sırasındaki yerçekimi vektörü (Örn: y = -1 zayıf yerçekimi, y = 1 ters yerçekimi)")]
    [SerializeField] private Vector3 anomalyGravity = new Vector3(0f, -1f, 0f); // Ay yerçekimi gibi
    
    [Tooltip("Anomali ne kadar sürede aktif olsun (geçiş süresi)")]
    [SerializeField] private float transitionDuration = 2f;
    
    [Tooltip("Otomatik başlasın mı?")]
    [SerializeField] private bool autoStart = true;
    
    [Header("Nesneleri Havaya Kaldırma")]
    [Tooltip("Etraftaki objelere rastgele kuvvet uygula")]
    [SerializeField] private bool floatObjects = true;
    
    [Tooltip("Uygulanacak kaldırma kuvveti")]
    [SerializeField] private float floatForce = 2f;
    
    private Vector3 originalGravity;
    private bool isAnomalyActive = false;

    private void Start()
    {
        // Orijinal yerçekimini kaydet
        originalGravity = Physics.gravity;
        
        if (autoStart)
        {
            StartCoroutine(ActivateAnomaly());
        }
    }

    private void OnDisable()
    {
        // Script kapanırsa yerçekimini düzelt
        Physics.gravity = originalGravity;
    }
    
    public void TriggerAnomaly()
    {
        if (!isAnomalyActive)
            StartCoroutine(ActivateAnomaly());
    }
    
    public void StopAnomaly()
    {
        if (isAnomalyActive)
            StartCoroutine(DeactivateAnomaly());
    }

    private IEnumerator ActivateAnomaly()
    {
        isAnomalyActive = true;
        float elapsed = 0f;
        
        // Yerçekimi geçişi
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            Physics.gravity = Vector3.Lerp(originalGravity, anomalyGravity, elapsed / transitionDuration);
            yield return null;
        }
        Physics.gravity = anomalyGravity;
        
        // Objelere hafif bir yukarı itme kuvveti ver (daha dramatik etki için)
        if (floatObjects)
        {
            Rigidbody[] allRbs = FindObjectsOfType<Rigidbody>();
            foreach (var rb in allRbs)
            {
                if (!rb.isKinematic)
                {
                    // Hafif yukarı ve rastgele dönüş
                    rb.AddForce(Vector3.up * floatForce, ForceMode.Impulse);
                    rb.AddTorque(Random.insideUnitSphere * floatForce, ForceMode.Impulse);
                }
            }
        }
    }

    private IEnumerator DeactivateAnomaly()
    {
        isAnomalyActive = false;
        float elapsed = 0f;
        
        Vector3 currentGrav = Physics.gravity;
        
        // Orijinale dön
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            Physics.gravity = Vector3.Lerp(currentGrav, originalGravity, elapsed / transitionDuration);
            yield return null;
        }
        Physics.gravity = originalGravity;
    }
}
