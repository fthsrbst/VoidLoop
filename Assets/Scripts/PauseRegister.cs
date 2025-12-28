using UnityEngine;

public class PauseRegister : MonoBehaviour
{
    [SerializeField] private Behaviour[] disableWhenPaused;

    private void Start()
    {
        if (PauseManager.Instance == null) return;
        if (disableWhenPaused == null || disableWhenPaused.Length == 0) return;

        PauseManager.Instance.RegisterForPause(disableWhenPaused);
    }
}
