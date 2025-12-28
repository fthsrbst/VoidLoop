using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// GlobalUI'daki EventSystem'in her zaman aktif ve tek olmasını sağlar.
/// Sahnelerdeki duplicate EventSystem'leri otomatik olarak siler.
/// </summary>
public class EnsureEventSystem : MonoBehaviour
{
    private EventSystem myEventSystem;

    private void Awake()
    {
        myEventSystem = GetComponentInChildren<EventSystem>();
        
        if (myEventSystem == null)
        {
            // EventSystem yoksa oluştur
            GameObject esObj = new GameObject("EventSystem");
            esObj.transform.SetParent(transform);
            myEventSystem = esObj.AddComponent<EventSystem>();
            esObj.AddComponent<StandaloneInputModule>();
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Sahnedeki diğer EventSystem'leri bul ve sil
        EventSystem[] allEventSystems = FindObjectsOfType<EventSystem>();
        
        foreach (EventSystem es in allEventSystems)
        {
            if (es != myEventSystem)
            {
                Debug.Log($"[EnsureEventSystem] Duplicate EventSystem silindi: {es.gameObject.name}");
                Destroy(es.gameObject);
            }
        }

        // Kendi EventSystem'imizi aktif yap
        if (myEventSystem != null)
        {
            myEventSystem.enabled = true;
        }
    }
}
