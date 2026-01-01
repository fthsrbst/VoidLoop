using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Displays a story introduction sequence when Level_0_Tutorial is entered for the first time.
/// Only shows once per game session - resets when game is restarted.
/// Attach this to an empty GameObject in Level_0_Tutorial scene.
/// </summary>
public class StoryIntroduction : MonoBehaviour
{
    [Header("Story Settings")]
    [SerializeField] private string[] storyLines = new string[]
    {
        "Where... Where is this place?",
        "Where did I fall?",
        "What is this dark void?",
        "I need to find a way out of here..."
    };
    
    [Header("Font Settings")]
    [Tooltip("Font to use for story text (leave empty for default)")]
    [SerializeField] private TMP_FontAsset customFont;
    
    [Header("Timing")]
    [SerializeField] private float typewriterSpeed = 0.05f;
    [SerializeField] private float delayBetweenLines = 2f;
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 1f;
    [SerializeField] private float startDelay = 1f;
    
    [Header("Visual Settings")]
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private int fontSize = 36;
    [SerializeField] private float bottomOffset = 80f;
    
    [Header("Text Shadow/Outline")]
    [SerializeField] private bool enableOutline = true;
    [SerializeField] private Color outlineColor = Color.black;
    [SerializeField] private float outlineWidth = 0.3f;
    
    [Header("Debug")]
    [SerializeField] private bool forceShowStory = false;
    [SerializeField] private bool showDebugLogs = true;
    
    // Static variable - resets when game restarts, persists during game session
    private static bool hasStoryPlayed = false;
    
    // UI References (created at runtime)
    private Canvas storyCanvas;
    private TextMeshProUGUI storyText;
    private CanvasGroup canvasGroup;
    
    // State
    private bool isPlaying = false;
    
    private void Start()
    {
        if (showDebugLogs) Debug.Log("[StoryIntro] Start called");
        
        // Check if this is the first time entering Level_0_Tutorial
        if (!forceShowStory && !ShouldShowStory())
        {
            if (showDebugLogs) Debug.Log("[StoryIntro] Skipping story - not first time this session");
            Destroy(gameObject);
            return;
        }
        
        if (showDebugLogs) Debug.Log("[StoryIntro] Showing story!");
        
        // Create UI and start story
        CreateUI();
        StartCoroutine(PlayStorySequence());
    }
    
    private bool ShouldShowStory()
    {
        // Check if story was already played this session
        if (showDebugLogs) Debug.Log($"[StoryIntro] hasStoryPlayed = {hasStoryPlayed}");
        
        if (hasStoryPlayed)
        {
            if (showDebugLogs) Debug.Log("[StoryIntro] Story already played this session");
            return false;
        }
        
        // Check if LevelManager exists and if this is a restart (not first time)
        if (LevelManager.Instance != null)
        {
            if (showDebugLogs) Debug.Log($"[StoryIntro] LevelManager - WrongChoices: {LevelManager.Instance.WrongChoices}, CorrectChoices: {LevelManager.Instance.CorrectChoices}, CurrentRound: {LevelManager.Instance.CurrentRound}");
            
            // If player has any wrong choices or correct choices, they've played before
            if (LevelManager.Instance.WrongChoices > 0 || LevelManager.Instance.CorrectChoices > 0)
            {
                if (showDebugLogs) Debug.Log("[StoryIntro] Skipping - player has choices recorded");
                return false;
            }
            
            // If current round is greater than 0, they've been through the loop
            if (LevelManager.Instance.CurrentRound > 0)
            {
                if (showDebugLogs) Debug.Log("[StoryIntro] Skipping - current round > 0");
                return false;
            }
        }
        else
        {
            if (showDebugLogs) Debug.Log("[StoryIntro] LevelManager not found");
        }
        
        return true;
    }
    
    private void CreateUI()
    {
        // Create Canvas
        GameObject canvasObj = new GameObject("StoryIntroCanvas");
        storyCanvas = canvasObj.AddComponent<Canvas>();
        storyCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        storyCanvas.sortingOrder = 100; // Above game UI but below pause menu
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        // NO GraphicRaycaster - allows clicks to pass through
        
        // Add CanvasGroup for fading
        canvasGroup = canvasObj.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false; // Don't block input
        
        // Create text at bottom of screen (no overlay)
        GameObject textObj = new GameObject("StoryText");
        textObj.transform.SetParent(canvasObj.transform, false);
        
        storyText = textObj.AddComponent<TextMeshProUGUI>();
        storyText.text = "";
        storyText.color = textColor;
        storyText.fontSize = fontSize;
        storyText.alignment = TextAlignmentOptions.Center;
        storyText.fontStyle = FontStyles.Italic;
        storyText.raycastTarget = false; // Don't block input
        
        // Apply custom font if set
        if (customFont != null)
        {
            storyText.font = customFont;
        }
        
        // Apply outline for readability without dark background
        if (enableOutline)
        {
            storyText.outlineWidth = outlineWidth;
            storyText.outlineColor = outlineColor;
        }
        
        // Position at bottom of screen
        RectTransform textRect = storyText.rectTransform;
        textRect.anchorMin = new Vector2(0, 0);
        textRect.anchorMax = new Vector2(1, 0);
        textRect.pivot = new Vector2(0.5f, 0);
        textRect.offsetMin = new Vector2(50, bottomOffset); // Left, Bottom
        textRect.offsetMax = new Vector2(-50, bottomOffset + 150); // Right, Top
        
        if (showDebugLogs) Debug.Log("[StoryIntro] UI Created");
    }
    
    private IEnumerator PlayStorySequence()
    {
        isPlaying = true;
        
        // Mark as played immediately so it won't play again if scene reloads
        hasStoryPlayed = true;
        
        if (showDebugLogs) Debug.Log("[StoryIntro] Starting story sequence");
        
        // Wait for scene to settle
        yield return new WaitForSeconds(startDelay);
        
        // Fade in text
        yield return StartCoroutine(FadeCanvasGroup(0f, 1f, fadeInDuration));
        
        if (showDebugLogs) Debug.Log("[StoryIntro] Fade in complete, starting lines");
        
        // Play each story line
        for (int i = 0; i < storyLines.Length; i++)
        {
            string line = storyLines[i];
            if (showDebugLogs) Debug.Log($"[StoryIntro] Playing line {i + 1}: {line}");
            
            // Clear text
            storyText.text = "";
            
            // Typewriter effect
            yield return StartCoroutine(TypewriterEffect(line));
            
            // Wait before next line
            yield return new WaitForSeconds(delayBetweenLines);
        }
        
        // Fade out
        yield return StartCoroutine(FadeCanvasGroup(1f, 0f, fadeOutDuration));
        
        if (showDebugLogs) Debug.Log("[StoryIntro] Story complete");
        
        isPlaying = false;
        
        // Cleanup
        if (storyCanvas != null)
        {
            Destroy(storyCanvas.gameObject);
        }
        Destroy(gameObject);
    }
    
    private IEnumerator TypewriterEffect(string text)
    {
        storyText.text = "";
        
        foreach (char c in text)
        {
            storyText.text += c;
            yield return new WaitForSeconds(typewriterSpeed);
        }
    }
    
    private IEnumerator FadeCanvasGroup(float from, float to, float duration)
    {
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            t = t * t * (3f - 2f * t); // Smooth step
            canvasGroup.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }
        
        canvasGroup.alpha = to;
    }
    
    /// <summary>
    /// Call this to reset the story flag (e.g., when returning to main menu)
    /// </summary>
    public static void ResetStory()
    {
        hasStoryPlayed = false;
        Debug.Log("[StoryIntro] Story reset - will play again on next Level_0_Tutorial entry");
    }
    
    // Context menu for easy reset in editor
    [ContextMenu("Reset Story Flag")]
    private void ResetStoryFromMenu()
    {
        ResetStory();
    }
}
