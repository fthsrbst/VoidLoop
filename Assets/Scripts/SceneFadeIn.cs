using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Fades in from black when a scene loads.
/// Also fades in music if BgMusicPersistent is present.
/// Add this to a full-screen black panel that covers everything.
/// </summary>
public class SceneFadeIn : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Duration of the fade-in from black")]
    [SerializeField] private float fadeDuration = 1.0f;
    
    [Tooltip("Delay before starting fade")]
    [SerializeField] private float startDelay = 0.1f;
    
    [Header("Music Fade")]
    [Tooltip("Also fade in the music")]
    [SerializeField] private bool fadeInMusic = true;
    
    [Tooltip("Target volume for music after fade")]
    [SerializeField] private float targetMusicVolume = 0.5f;
    
    private Image fadeImage;
    private AudioSource musicSource;
    
    private void Awake()
    {
        fadeImage = GetComponent<Image>();
        
        // Start fully black
        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 1f;
            fadeImage.color = c;
        }
        
        // Find music source
        if (fadeInMusic)
        {
            var musicManager = FindObjectOfType<BgMusicPersistent>();
            if (musicManager != null)
            {
                musicSource = musicManager.GetComponent<AudioSource>();
                if (musicSource != null)
                {
                    musicSource.volume = 0f; // Start silent
                }
            }
        }
    }

    private void Start()
    {
        StartCoroutine(FadeIn());
    }

    private IEnumerator FadeIn()
    {
        yield return new WaitForSeconds(startDelay);
        
        if (fadeImage == null) yield break;
        
        float elapsed = 0f;
        Color fadeColor = fadeImage.color;
        
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            
            // Smooth step for professional feel
            t = t * t * (3f - 2f * t);
            
            // Fade visual
            fadeColor.a = Mathf.Lerp(1f, 0f, t);
            fadeImage.color = fadeColor;
            
            // Fade music
            if (musicSource != null && fadeInMusic)
            {
                musicSource.volume = Mathf.Lerp(0f, targetMusicVolume, t);
            }
            
            yield return null;
        }
        
        // Ensure fully transparent
        fadeColor.a = 0f;
        fadeImage.color = fadeColor;
        
        // Ensure music at target volume
        if (musicSource != null && fadeInMusic)
        {
            musicSource.volume = targetMusicVolume;
        }
        
        // Disable the overlay so it doesn't block input
        gameObject.SetActive(false);
    }
}

