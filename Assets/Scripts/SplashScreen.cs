using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Professional splash screen controller with fade-in/fade-out animation.
/// Displays the studio logo after Unity splash, then transitions to main menu.
/// </summary>
public class SplashScreen : MonoBehaviour
{
    [Header("Splash Settings")]
    [Tooltip("Duration of the fade-in animation in seconds")]
    [SerializeField] private float fadeInDuration = 1.5f;
    
    [Tooltip("Duration to hold the logo on screen in seconds")]
    [SerializeField] private float holdDuration = 2.0f;
    
    [Tooltip("Duration of the fade-out animation in seconds")]
    [SerializeField] private float fadeOutDuration = 1.5f;
    
    [Tooltip("Name of the scene to load after splash")]
    [SerializeField] private string nextSceneName = "UIMenu";
    
    [Header("References")]
    [Tooltip("The logo image to fade")]
    [SerializeField] private Image logoImage;

    private void Start()
    {
        // Ensure we start with the logo invisible
        if (logoImage != null)
        {
            Color c = logoImage.color;
            c.a = 0f;
            logoImage.color = c;
        }
        
        // Start the splash sequence immediately
        StartCoroutine(SplashSequence());
    }

    /// <summary>
    /// Main splash screen sequence: fade in -> hold -> fade out -> load next scene
    /// </summary>
    private IEnumerator SplashSequence()
    {
        // Fade in logo
        yield return StartCoroutine(FadeLogo(0f, 1f, fadeInDuration));
        
        // Hold (only if duration > 0)
        if (holdDuration > 0f)
        {
            yield return new WaitForSeconds(holdDuration);
        }
        
        // Fade out logo
        yield return StartCoroutine(FadeLogo(1f, 0f, fadeOutDuration));
        
        // Load the main menu scene immediately after fade out
        SceneManager.LoadScene(nextSceneName);
    }

    /// <summary>
    /// Smoothly fades the logo image alpha from start to end value.
    /// </summary>
    private IEnumerator FadeLogo(float startAlpha, float endAlpha, float duration)
    {
        if (logoImage == null)
        {
            Debug.LogWarning("SplashScreen: Logo Image is not assigned!");
            yield break;
        }

        // Skip if duration is zero or negative
        if (duration <= 0f)
        {
            Color c = logoImage.color;
            c.a = endAlpha;
            logoImage.color = c;
            yield break;
        }

        float elapsed = 0f;
        Color logoColor = logoImage.color;
        logoColor.a = startAlpha;
        logoImage.color = logoColor;
        
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; // Use unscaled time for consistency
            float t = Mathf.Clamp01(elapsed / duration);
            
            // Smooth step for professional feel
            float smoothT = t * t * (3f - 2f * t);
            logoColor.a = Mathf.Lerp(startAlpha, endAlpha, smoothT);
            logoImage.color = logoColor;
            
            yield return null;
        }
        
        // Ensure we reach the exact end value
        logoColor.a = endAlpha;
        logoImage.color = logoColor;
    }
}

