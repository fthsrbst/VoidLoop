using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement; // Sahne geçişi için gerekli kütüphane

public class TutorialManager : MonoBehaviour
{
    public TextMeshProUGUI tutorialText;
    private int currentStep = 0;

    void Start()
    {
        ShowStep();
    }

    void Update()
    {
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
    }

    void ShowStep()
    {
        switch (currentStep)
        {
            case 0:
                tutorialText.text = "F tuşu ile feneri kullanabilirsin";
                break;
            case 1:
                tutorialText.text = "Harika! Şimdi yürümeye başla (WASD)";
                break;
            case 2:
                tutorialText.text = "Düşmanlardan kaçmayı unutma! (Devam etmek için Enter)";
                break;
            case 3:
                tutorialText.text = "Eğilmek için Sol Ctrl tuşunu kullan";
                break;
            case 4:
                tutorialText.text = "Hızlı koşmak için Sol Shift tuşuna basılı tut";
                break;
            case 5:
                tutorialText.text = "Zıplamak için Space tuşuna bas";
                break;
            case 6:
                tutorialText.text = "<color=red>Kırmızı kapı</color> anomali kapısıdır. Eğer anomali olduğunu düşünüyorsan o kapıdan girmelisin.";
                break;
            case 7:
                tutorialText.text = "Eğer anomali yoksa <color=blue>mavi kapıya</color> gitmelisin.";
                break;
                            case 8:
                tutorialText.text = "Unutma ilk bölümde mavi kapıdan girmelisin.";
                break;
            default:
                tutorialText.text = "Temel eğitim tamamlandı!";
                // 3 saniye sonra sahne değiştirme fonksiyonunu çağırır
                Invoke("LoadNextScene", 3f);
                break;
        }
    }

    public void NextStep()
    {
        currentStep++;
        ShowStep();
    }

    void LoadNextScene()
    {
        // Level_0_Tutorial isimli sahneye geçiş yapar
        SceneManager.LoadScene("Level_0_Tutorial");
    }
}