using System;
using UnityEngine;


public class ScoreMenu : Menu3D
{
    public enum ScoreMenuState
    {
        EDITABLE,
        VIEW,
    }

    public GameObject playButton;
    public GameObject quitButton;
    public GameObject backButton;

    public ScoreBoard scoreBoard;

    public void SetScoreMenuState(ScoreMenuState state)
    {
        switch (state)
        {
            case ScoreMenuState.VIEW:
                playButton.SetActive(false);
                quitButton.SetActive(false);
                backButton.SetActive(true);
                break;
            case ScoreMenuState.EDITABLE:
                playButton.SetActive(true);
                quitButton.SetActive(true);
                backButton.SetActive(false);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }
    }
}
