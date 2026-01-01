using UnityEngine;
using UnityEngine.SceneManagement;

public class BgMusicPersistent : MonoBehaviour
{
    private static BgMusicPersistent instance;

    public static BgMusicPersistent Instance => instance;

    [Header("Scene Configuration")]
    [Tooltip("Main Menu sahnesinin tam adı")]
    [SerializeField] private string menuSceneName = "MainMenu";
    
    [Tooltip("Tutorial sahnesinin tam adı")]
    [SerializeField] private string tutorialSceneName = "Level_0_Tutorial";

    [Header("Music Settings")]
    [Tooltip("Menüde çalacak müzik")]
    [SerializeField] private AudioClip menuMusic;
    
    [Tooltip("Oyun içinde çalacak müzik")]
    [SerializeField] private AudioClip gameMusic;
    
    [Tooltip("Tutorial sahnesinde müzik çalsın mı?")]
    [SerializeField] private bool playMusicInTutorial = false;
    
    [Header("Audio Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float volume = 0.5f;

    private AudioSource audioSource;
    
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        
        SetupAudioSource();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    private void Start()
    {
        // İlk açılışta sahneye göre müzik çal
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }
    
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void SetupAudioSource()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) 
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.volume = volume;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string sceneName = scene.name;
        AudioClip targetClip = null;

        // Sahneye göre hangi müziğin çalması gerektiğini belirle
        if (sceneName == menuSceneName)
        {
            targetClip = menuMusic;
        }
        else if (sceneName == tutorialSceneName)
        {
            if (playMusicInTutorial)
            {
                targetClip = gameMusic;
            }
            else
            {
                targetClip = null; // Tutorial'da müzik istenmiyorsa
            }
        }
        else
        {
            // Diğer oyun sahneleri
            targetClip = gameMusic;
        }

        PlayMusic(targetClip);
    }
    
    public void PlayMusic(AudioClip clip)
    {
        if (audioSource == null) return;

        // Eğer istenen müzik zaten çalıyorsa bir şey yapma
        if (audioSource.clip == clip && audioSource.isPlaying) return;

        // Müzik kapatılacaksa
        if (clip == null)
        {
            audioSource.Stop();
            audioSource.clip = null;
            return;
        }

        // Yeni müzik çal
        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.Play();
    }

    public void SetVolume(float newVolume)
    {
        volume = newVolume;
        if (audioSource != null)
        {
            audioSource.volume = volume;
        }
    }
}
