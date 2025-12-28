using UnityEngine;

public class BgMusicPersistent : MonoBehaviour
{
    private static BgMusicPersistent instance;

    private void Awake()
    {
        // Ayný müzik objesi ikinci kez gelirse onu sil
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        // AudioSource yoksa ekle
        var a = GetComponent<AudioSource>();
        if (a == null) a = gameObject.AddComponent<AudioSource>();

        // Güvenli ayarlar
        a.loop = true;
        if (!a.isPlaying) a.Play();
    }
}
