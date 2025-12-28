using UnityEngine;
using UnityEngine.UI;

public class UIButtonClickSFX : MonoBehaviour
{
    [SerializeField] private string buttonClickObjectName = "ButtonClick";
    private static AudioSource cachedClickSource;

    private void Awake()
    {
        // AudioSource'u bir kez bul ve cache'le
        if (cachedClickSource == null)
        {
            var go = GameObject.Find(buttonClickObjectName);
            cachedClickSource = go ? go.GetComponent<AudioSource>() : null;

            if (cachedClickSource == null)
                Debug.LogWarning($"UIButtonClickSFX: '{buttonClickObjectName}' objesinde AudioSource bulunamadý.");
        }

        // Bu objede Button varsa otomatik baðla
        var btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(PlayClick);
        }
    }

    private void PlayClick()
    {
        if (cachedClickSource != null)
            cachedClickSource.PlayOneShot(cachedClickSource.clip);
    }
}
