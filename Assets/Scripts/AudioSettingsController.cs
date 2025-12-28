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

    // SFX AudioSource'larýn orijinal (base) volume deðerlerini sakla
    private readonly Dictionary<int, float> sfxBaseVolumes = new();

    private void Awake()
    {
        // Kayýtlý deðerleri al
        float musicVol = PlayerPrefs.GetFloat(MusicKey, 1f);
        float sfxVol = PlayerPrefs.GetFloat(SfxKey, 1f);

        // Sliderlara bas
        if (musicSlider != null) musicSlider.value = musicVol;
        if (sfxSlider != null) sfxSlider.value = sfxVol;

        // Sahnedeki isimli kaynaklarý bul
        BindNamedSources();

        // SFX kaynaklarýný cachele + volume uygula
        CacheSfxSourcesInScene();
        ApplyMusicVolume(musicVol);
        ApplySfxVolume(sfxVol);

        // Slider eventleri
        if (musicSlider != null) musicSlider.onValueChanged.AddListener(OnMusicChanged);
        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(OnSfxChanged);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Yeni sahnede BgSound/ButtonClick yeniden bulunabilir
        BindNamedSources();

        // Yeni sahnedeki SFX kaynaklarýný yakala ve volume uygula
        CacheSfxSourcesInScene();
        ApplyMusicVolume(GetMusicVolume());
        ApplySfxVolume(GetSfxVolume());
    }

    private void BindNamedSources()
    {
        // BgSound bul
        var bgObj = GameObject.Find(bgSoundObjectName);
        bgSound = bgObj ? bgObj.GetComponent<AudioSource>() : null;

        // ButtonClick bul (SFX sayýlacak)
        var clickObj = GameObject.Find(buttonClickObjectName);
        buttonClickSource = clickObj ? clickObj.GetComponent<AudioSource>() : null;

        if (bgSound == null)
            Debug.LogWarning($"AudioSettingsController: '{bgSoundObjectName}' objesinde AudioSource bulunamadý.");
        if (buttonClickSource == null)
            Debug.LogWarning($"AudioSettingsController: '{buttonClickObjectName}' objesinde AudioSource bulunamadý.");
    }

    private void CacheSfxSourcesInScene()
    {
        var allSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);

        foreach (var src in allSources)
        {
            if (src == null) continue;

            // SADECE BgSound music; onun dýþýndaki her þey SFX
            if (bgSound != null && src == bgSound) continue;

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

        // Yeni kaynaklar eklenmiþ olabilir
        CacheSfxSourcesInScene();
        ApplySfxVolume(v);
    }

    private void ApplyMusicVolume(float v)
    {
        if (bgSound != null)
            bgSound.volume = Mathf.Clamp01(v);
    }

    private void ApplySfxVolume(float v)
    {
        v = Mathf.Clamp01(v);
        var allSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);

        foreach (var src in allSources)
        {
            if (src == null) continue;
            if (bgSound != null && src == bgSound) continue; // BgSound hariç hepsi SFX

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
    }

    private void SaveSfxVolume(float v)
    {
        PlayerPrefs.SetFloat(SfxKey, v);
        PlayerPrefs.Save();
    }

    private float GetMusicVolume() => PlayerPrefs.GetFloat(MusicKey, 1f);
    private float GetSfxVolume() => PlayerPrefs.GetFloat(SfxKey, 1f);
}
