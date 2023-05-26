using UnityEngine;


public class PauseMenu : MonoBehaviour
{
    private bool _isPaused;

    public bool Paused()
    {
        return _isPaused;
    }

    public void TogglePause()
    {
        _isPaused = !_isPaused;
        if (_isPaused)
        {
            Time.timeScale = 0.0f;
            gameObject.SetActive(true);
        }
        else
        {
            Time.timeScale = 1.0f;
            gameObject.SetActive(false);
        }
    }
}
