using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class TutorialManager : MonoBehaviour
{
    public TextMeshProUGUI tutorialText;
    private int currentStep = 0;

    [Header("Daktilo Ayarları")]
    [SerializeField] private float textSpeed = 0.05f;       // Harf harf yazılma hızı
    [SerializeField] private float waitBetweenSteps = 2.5f; // Yazı bittikten sonraki bekleme süresi
    [SerializeField] private float startDelay = 2.0f;       // Sahne açıldıktan sonraki ilk bekleme süresi

    // Enter tuşu ile geçiş için değişkenler
    private bool isTyping = false;           // Yazı yazılıyor mu?
    private bool skipTyping = false;         // Yazıyı atla
    private bool skipWaiting = false;        // Beklemeyi atla
    private string currentFullText = "";     // Mevcut tam metin

    void Start()
    {
        // Yazı başlamadan önce metni temizle
        tutorialText.text = "";
        // Otomatik ilerleme döngüsünü başlat
        StartCoroutine(AutoAdvanceWithTypewriter());
    }

    void Update()
    {
        // Enter veya Return tuşuna basıldığında
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (isTyping)
            {
                // Yazı yazılıyorsa, yazıyı anında tamamla
                skipTyping = true;
            }
            else
            {
                // Yazı tamamlandıysa, beklemeyi atla ve sonraki adıma geç
                skipWaiting = true;
            }
        }
    }

    IEnumerator AutoAdvanceWithTypewriter()
    {
        // 1. ADIM: Sahne açıldıktan sonra belirlenen süre kadar (2 saniye) bekle
        yield return new WaitForSeconds(startDelay);

        // 2. ADIM: Toplam 11 case (0'dan 10'a kadar) döngüye gir
        while (currentStep <= 10)
        {
            currentFullText = GetStepText(currentStep);
            yield return StartCoroutine(TypeText(currentFullText));
            
            // Bekleme süresi - Enter ile atlanabilir
            skipWaiting = false;
            float elapsedTime = 0f;
            while (elapsedTime < waitBetweenSteps && !skipWaiting)
            {
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            currentStep++;
        }

        // 3. ADIM: Final mesajı ve sahne geçişi
        tutorialText.text = "Basic training completed!";
        yield return new WaitForSeconds(2f);
        SceneManager.LoadScene("Level_0_Tutorial");
    }

    IEnumerator TypeText(string textToType)
    {
        isTyping = true;
        skipTyping = false;
        tutorialText.text = ""; 
        
        foreach (char letter in textToType.ToCharArray())
        {
            if (skipTyping)
            {
                // Enter'a basıldıysa yazıyı anında tamamla
                tutorialText.text = textToType;
                break;
            }
            tutorialText.text += letter;
            yield return new WaitForSeconds(textSpeed);
        }
        
        isTyping = false;
    }

    string GetStepText(int step)
    {
        switch (step)
        {
            case 0: return "Welcome to the Tutorial!";
            case 1: return "You can use the flashlight with the [F] key.";
            case 2: return "Great! Now start walking with [WASD].";
            case 3: return "Don't forget to escape from enemies!";
            case 4: return "Use [Left Ctrl] to crouch.";
            case 5: return "Hold [Left Shift] to sprint.";
            case 6: return "Press [Space] to jump.";
            case 7: return "The 'Red Door' is for anomalies. Enter it if you notice something is wrong.";
            case 8: return "If there is no anomaly, you should go to the 'Blue Door'.";
            case 9: return "Remember, you must enter the Blue Door in the first level!";
            case 10: return "The flashlight recharges while it is turned off.";
            default: return "";
        }
    }
}