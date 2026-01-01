using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AudioSettingsController : MonoBehaviour
{
    [Header("Named AudioSources in Scene")]
    [SerializeField] private string bgSoundObjectName = "BgSound";
    [SerializeField] private string buttonClickObjectName = "ButtonClick";

    [Header("UI Sliders")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    private AudioSource bgSound;          // Music
    private AudioSource buttonClickSource; // SFX (click)

    private const string MusicKey = "VOL_MUSIC";
    private const string SfxKey = "VOL_SFX";

    // SFX AudioSources original base volume values
    private static readonly Dictionary<int, float> sfxBaseVolumes = new();
    
    // Static reference to allow multiple instances to sync
    private static float cachedMusicVolume = -1f;
    private static float cachedSfxVolume = -1f;

    private void Awake()
    {
        // Auto-find references if missing
        FindUIReferences();

        // Get saved values
        float musicVol = PlayerPrefs.GetFloat(MusicKey, 1f);
        float sfxVol = PlayerPrefs.GetFloat(SfxKey, 1f);
        
        cachedMusicVolume = musicVol;
        cachedSfxVolume = sfxVol;

        // Set sliders
        if (musicSlider != null) musicSlider.value = musicVol;
        if (sfxSlider != null) sfxSlider.value = sfxVol;

        // Find named sources
        BindNamedSources();

        // Cache SFX + apply volume
        CacheSfxSourcesInScene();
        ApplyMusicVolume(musicVol);
        ApplySfxVolume(sfxVol);

        // Slider events
        if (musicSlider != null) musicSlider.onValueChanged.AddListener(OnMusicChanged);
        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(OnSfxChanged);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    private void OnEnable()
    {
        // Try finding again in case this was enabled later
        FindUIReferences();

        // Sync sliders with current values when panel becomes active
        float musicVol = PlayerPrefs.GetFloat(MusicKey, 1f);
        float sfxVol = PlayerPrefs.GetFloat(SfxKey, 1f);
        
        if (musicSlider != null) 
        {
            musicSlider.SetValueWithoutNotify(musicVol);
            // Re-bind listener just in case
            musicSlider.onValueChanged.RemoveListener(OnMusicChanged);
            musicSlider.onValueChanged.AddListener(OnMusicChanged);
        }
        
        if (sfxSlider != null) 
        {
            sfxSlider.SetValueWithoutNotify(sfxVol);
            sfxSlider.onValueChanged.RemoveListener(OnSfxChanged);
            sfxSlider.onValueChanged.AddListener(OnSfxChanged);
        }
    }

    private void FindUIReferences()
    {
        if (musicSlider != null && sfxSlider != null) return;

        // Search all sliders including inactive ones
        var allSliders = Resources.FindObjectsOfTypeAll<Slider>();
        foreach (var s in allSliders)
        {
            // Skip assets (prefabs not in scene)
            if (s.gameObject.scene.rootCount == 0) continue;

            // Find by slider name directly
            if (musicSlider == null && s.name == "MusicSlider")
            {
                musicSlider = s;
            }
            if (sfxSlider == null && s.name == "SFXSlider")
            {
                sfxSlider = s;
            }
        }

        // Fallback: Find by parent name (Row_Music / Row_SFX)
        if (musicSlider == null || sfxSlider == null)
        {
            foreach (var s in allSliders)
            {
                if (s.gameObject.scene.rootCount == 0) continue;

                if (musicSlider == null && s.transform.parent != null && s.transform.parent.name == "Row_Music")
                {
                    musicSlider = s;
                }
                if (sfxSlider == null && s.transform.parent != null && s.transform.parent.name == "Row_SFX")
                {
                    sfxSlider = s;
                }
            }
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Find sources again in new scene
        BindNamedSources();

        // Catch new SFX and apply volume
        CacheSfxSourcesInScene();
        ApplyMusicVolume(GetMusicVolume());
        ApplySfxVolume(GetSfxVolume());
    }

    private void BindNamedSources()
    {
        // Try to find via Singleton first
        if (BgMusicPersistent.Instance != null)
        {
            bgSound = BgMusicPersistent.Instance.GetComponent<AudioSource>();
        }

        // Fallback: Find by name
        if (bgSound == null)
        {
            var bgObj = GameObject.Find(bgSoundObjectName);
            bgSound = bgObj ? bgObj.GetComponent<AudioSource>() : null;
        }

        // Find ButtonClick (SFX)
        var clickObj = GameObject.Find(buttonClickObjectName);
        buttonClickSource = clickObj ? clickObj.GetComponent<AudioSource>() : null;
    }

    private void CacheSfxSourcesInScene()
    {
        var allSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);

        foreach (var src in allSources)
        {
            if (src == null) continue;

            // ONLY BgSound is music; everything else is SFX
            if (bgSound != null && src == bgSound) continue;
            
            // Also exclude BgMusicPersistent's AudioSource
            if (BgMusicPersistent.Instance != null)
            {
                var persistentAudio = BgMusicPersistent.Instance.GetComponent<AudioSource>();
                if (persistentAudio != null && src == persistentAudio) continue;
            }

            int id = src.GetInstanceID();
            if (!sfxBaseVolumes.ContainsKey(id))
                sfxBaseVolumes[id] = src.volume;
        }
    }

    private void OnMusicChanged(float v)
    {
        v = Mathf.Clamp01(v);
        SaveMusicVolume(v);
        ApplyMusicVolume(v);
    }

    private void OnSfxChanged(float v)
    {
        v = Mathf.Clamp01(v);
        SaveSfxVolume(v);

        // New sources might have been added
        CacheSfxSourcesInScene();
        ApplySfxVolume(v);
    }

    private void ApplyMusicVolume(float v)
    {
        // Apply to BgMusicPersistent (this controls both menu and game music)
        if (BgMusicPersistent.Instance != null)
        {
            BgMusicPersistent.Instance.SetVolume(Mathf.Clamp01(v));
        }

        // Also apply to local bgSound if different
        if (bgSound != null)
        {
            var persistentAudio = BgMusicPersistent.Instance?.GetComponent<AudioSource>();
            if (persistentAudio == null || bgSound != persistentAudio)
            {
                bgSound.volume = Mathf.Clamp01(v);
            }
        }
    }

    private void ApplySfxVolume(float v)
    {
        v = Mathf.Clamp01(v);
        var allSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);

        foreach (var src in allSources)
        {
            if (src == null) continue;
            if (bgSound != null && src == bgSound) continue; // Skip BgSound
            
            // Also skip BgMusicPersistent's AudioSource
            if (BgMusicPersistent.Instance != null)
            {
                var persistentAudio = BgMusicPersistent.Instance.GetComponent<AudioSource>();
                if (persistentAudio != null && src == persistentAudio) continue;
            }

            int id = src.GetInstanceID();
            if (sfxBaseVolumes.TryGetValue(id, out float baseVol))
                src.volume = baseVol * v;
            else
                src.volume = v;
        }
    }

    private void SaveMusicVolume(float v)
    {
        PlayerPrefs.SetFloat(MusicKey, v);
        PlayerPrefs.Save();
        cachedMusicVolume = v;
    }

    private void SaveSfxVolume(float v)
    {
        PlayerPrefs.SetFloat(SfxKey, v);
        PlayerPrefs.Save();
        cachedSfxVolume = v;
    }

    private float GetMusicVolume() => PlayerPrefs.GetFloat(MusicKey, 1f);
    private float GetSfxVolume() => PlayerPrefs.GetFloat(SfxKey, 1f);
}
