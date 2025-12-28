using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class LoadingAsyncLoader : MonoBehaviour
{
    [Header("Target Scene")]
    [SerializeField] private string targetSceneName = "Level_0_Tutorial";

    [Header("UI (Optional)")]
    [SerializeField] private TMP_Text loadingText;
    [SerializeField] private bool showPercent = false;

    [Header("Optional")]
    [SerializeField] private float minimumLoadingScreenTime = 1.0f;

    private void Start()
    {
        // Pause vs kalmasýn
        Time.timeScale = 1f;
        StartCoroutine(LoadRoutine());
    }

    private IEnumerator LoadRoutine()
    {
        float start = Time.unscaledTime;

        AsyncOperation op = SceneManager.LoadSceneAsync(targetSceneName);
        op.allowSceneActivation = false;

        while (!op.isDone)
        {
            float normalized = Mathf.Clamp01(op.progress / 0.9f);

            if (loadingText != null)
            {
                if (showPercent)
                    loadingText.text = $"Loading {Mathf.RoundToInt(normalized * 100f)}%";
                else
                    loadingText.text = "Loading...";
            }

            bool ready = op.progress >= 0.9f;
            bool minTimePassed = (Time.unscaledTime - start) >= minimumLoadingScreenTime;

            if (ready && minTimePassed)
            {
                op.allowSceneActivation = true; // direkt Level_0_Tutorial'a geç
            }

            yield return null;
        }
    }
}
