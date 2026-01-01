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

    void Start()
    {
        // Yazı başlamadan önce metni temizle
        tutorialText.text = "";
        // Otomatik ilerleme döngüsünü başlat
        StartCoroutine(AutoAdvanceWithTypewriter());
    }

    IEnumerator AutoAdvanceWithTypewriter()
    {
        // 1. ADIM: Sahne açıldıktan sonra belirlenen süre kadar (2 saniye) bekle
        yield return new WaitForSeconds(startDelay);

        // 2. ADIM: Toplam 11 case (0'dan 10'a kadar) döngüye gir
        while (currentStep <= 10)
        {
            string fullText = GetStepText(currentStep);
            yield return StartCoroutine(TypeText(fullText));
            yield return new WaitForSeconds(waitBetweenSteps);
            currentStep++;
        }

        // 3. ADIM: Final mesajı ve sahne geçişi
        tutorialText.text = "Basic training completed!";
        yield return new WaitForSeconds(2f);
        SceneManager.LoadScene("Level_0_Tutorial");
    }

    IEnumerator TypeText(string textToType)
    {
        tutorialText.text = ""; 
        foreach (char letter in textToType.ToCharArray())
        {
            tutorialText.text += letter;
            yield return new WaitForSeconds(textSpeed);
        }
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