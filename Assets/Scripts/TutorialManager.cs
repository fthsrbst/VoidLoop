using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class TutorialManager : MonoBehaviour
{
    public TextMeshProUGUI tutorialText;
    private int currentStep = 0;

    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 1.5f;
    
    private Image fadeImage;
    private bool isTransitioning = false;

    void Start()
    {
        CreateFadeOverlay();
        ShowStep();
    }

    void CreateFadeOverlay()
    {
        // Fade için Canvas oluştur
        GameObject canvasObj = new GameObject("TutorialFadeCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999; // En üstte
        canvasObj.AddComponent<CanvasScaler>();
        
        // Fade Image oluştur
        GameObject imageObj = new GameObject("FadeImage");
        imageObj.transform.SetParent(canvasObj.transform);
        
        fadeImage = imageObj.AddComponent<Image>();
        fadeImage.color = new Color(0, 0, 0, 0); // Başlangıçta şeffaf
        fadeImage.raycastTarget = false;
        
        // Tam ekran kaplasın
        RectTransform rect = fadeImage.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    void Update()
    {
        if (isTransitioning) return;
        
        // 0. ADIM: Fener (F)
        if (currentStep == 0 && Input.GetKeyDown(KeyCode.F)) NextStep();

        // 1. ADIM: Yürüme (WASD)
        else if (currentStep == 1 && (Input.GetAxis("Horizontal") != 0 || Input.GetAxis("Vertical") != 0)) NextStep();

        // 2. ADIM: Düşman Bilgisi (Enter)
        else if (currentStep == 2 && Input.GetKeyDown(KeyCode.Return)) NextStep();

        // 3. ADIM: Eğilme (Sol Ctrl)
        else if (currentStep == 3 && Input.GetKeyDown(KeyCode.LeftControl)) NextStep();

        // 4. ADIM: Koşma (Sol Shift)
        else if (currentStep == 4 && (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKey(KeyCode.LeftShift))) NextStep();

        // 5. ADIM: Zıplama (Space)
        else if (currentStep == 5 && Input.GetKeyDown(KeyCode.Space)) NextStep();

        // 6. ADIM: Kırmızı Kapı (Enter)
        else if (currentStep == 6 && Input.GetKeyDown(KeyCode.Return)) NextStep();

        // 7. ADIM: Mavi Kapı (Enter)
        else if (currentStep == 7 && Input.GetKeyDown(KeyCode.Return)) NextStep();

        else if (currentStep == 8 && Input.GetKeyDown(KeyCode.Return)) NextStep();

        else if (currentStep == 9 && Input.GetKeyDown(KeyCode.Return)) NextStep();
    }

    void ShowStep()
    {
        switch (currentStep)
        {
            case 0:
                tutorialText.text = "You can use the flashlight with the [F] key.";
                break;
            case 1:
                tutorialText.text = "Great! Now start walking with [WASD].";
                break;
            case 2:
                tutorialText.text = "Don't forget to escape from enemies! (Press [Enter] to continue)";
                break;
            case 3:
                tutorialText.text = "Use [Left Ctrl] to crouch.";
                break;
            case 4:
                tutorialText.text = "Hold [Left Shift] to sprint.";
                break;
            case 5:
                tutorialText.text = "Press [Space] to jump.";
                break;
            case 6:
                tutorialText.text = "The <color=red>Red Door</color> is for anomalies. Enter it if you notice something is wrong.";
                break;
            case 7:
                tutorialText.text = "If there is no anomaly, you should go to the <color=blue>Blue Door</color>.";
                break;
            case 8:
                tutorialText.text = "Remember, you must enter the Blue Door in the first level!";
                break;
            case 9:
                tutorialText.text = "The flashlight recharges while it is turned off.";
                break;
            default:
                tutorialText.text = "Basic training completed!";
                // 3 saniye sonra fade ile sahne geçişi
                Invoke("StartFadeTransition", 3f);
                break;
        }
    }

    public void NextStep()
    {
        currentStep++;
        ShowStep();
    }

    void StartFadeTransition()
    {
        if (!isTransitioning)
        {
            isTransitioning = true;
            
            // Önce BlinkTransition'ı dene - o zaten fade in/out yapıyor
            BlinkTransition blink = BlinkTransition.Instance;
            if (blink != null)
            {
                blink.Blink(() => {
                    SceneManager.LoadScene("Level_0_Tutorial");
                });
            }
            else
            {
                // BlinkTransition yoksa kendi fade'imizi kullan
                StartCoroutine(FadeAndLoadScene());
            }
        }
    }

    IEnumerator FadeAndLoadScene()
    {
        // Siyaha fade out
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
            fadeImage.color = new Color(0, 0, 0, alpha);
            yield return null;
        }
        
        fadeImage.color = new Color(0, 0, 0, 1f);
        
        // Kısa bekleme
        yield return new WaitForSeconds(0.3f);
        
        // Sahneyi yükle (SceneFadeIn component varsa o fade in yapacak)
        SceneManager.LoadScene("Level_0_Tutorial");
    }
}