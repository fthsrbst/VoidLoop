using UnityEngine;
using TMPro; 
using System.Collections;

public class FinalTextManager : MonoBehaviour
{
    [Header("Bileşen Ayarları")]
    [SerializeField] private TextMeshProUGUI targetText; // Sahnedeki Text(TMP) objesi
    
    [Header("Zamanlama Ayarları")]
    [SerializeField] private float initialDelay = 2f; // Sahne açıldıktan kaç sn sonra başlasın?
    [SerializeField] private float timePerPhrase = 3f; // Her bir yazının ekranda kalma süresi
    
    [Header("İçerik Ayarları")]
    [TextArea(3, 10)] 
    [SerializeField] private string[] phrases; // Ekranda sırayla görünecek yazılar

    void Start()
    {
        if (targetText != null && phrases.Length > 0)
        {
            // Yazı kutusu başta boş görünsün
            targetText.text = "";
            // Coroutine'i başlat
            StartCoroutine(ShowPhrasesRoutine());
        }
    }

    IEnumerator ShowPhrasesRoutine()
    {
        // Sahne açıldıktan sonra belirlenen süre kadar bekle (2 saniye)
        yield return new WaitForSeconds(initialDelay);

        // Listedeki tüm yazıları sırayla dön
        foreach (string textContent in phrases)
        {
            targetText.text = textContent; // Yazıyı değiştir
            yield return new WaitForSeconds(timePerPhrase); // 3 saniye bekle
        }

        // Tüm yazılar bittiğinde metin kutusunu gizle
        targetText.gameObject.SetActive(false);
    }
}