using System;
using TMPro;
using UnityEngine;


public class GameState : State
{
    private GameManager _gameManager;

    private float _timePlayed;
    private int _score;
    private int _displayedScore;

    public GameState(string name, GameManager gameManager) : base(name)
    {
        _gameManager = gameManager;
        _timePlayed = 0.0f;
    }

    public override void Enter()
    {
        base.Enter();

        _gameManager.gameOverMenu.SetActive(false);

        _gameManager.player.Init();
        
        _gameManager.aiSystem.Clear();
        _gameManager.aiSystem.Init();

        _gameManager.projection.enableDebugMode = false;

        _timePlayed = 0.0f;
        _gameManager.timePlayedUI.text = "Time: 00:00.0";

        _score = 0;
        _displayedScore = 0;
        _gameManager.scoreUI.text = "Score: 0";

        _gameManager.aiSystem.EnemyDied += OnEnemyDied;

        Time.timeScale = 1.0f;
    }

    public override void Update()
    {
        base.Update();

        _timePlayed += Time.deltaTime;

        var timeSpan = TimeSpan.FromSeconds(_timePlayed);
        var timeString = $"{timeSpan.Minutes:00}:{timeSpan.Seconds:00}.{timeSpan.Milliseconds / 100:0}";
        _gameManager.timePlayedUI.text = $"Time: {timeString}";

        if (_score != _displayedScore)
        {
            _displayedScore += 1;
            _gameManager.scoreUI.text = $"Score: {_displayedScore}";
        }

        if (!_gameManager.player.IsAlive())
            _gameManager.fsm.SwitchState("GameOverState");
    }

    private void OnEnemyDied()
    {
        _score += 100;
    }

    public override void Exit()
    {
        base.Exit();
        
        _gameManager.aiSystem.EnemyDied -= OnEnemyDied;
    }
}


public class GameOverState : State
{
    private GameManager _gameManager;

    public GameOverState(string name, GameManager gameManager) : base(name)
    {
        _gameManager = gameManager;
    }

    public override void Enter()
    {
        base.Enter();

        Time.timeScale = 0.0f;

        _gameManager.projection.enableDebugMode = true;

        _gameManager.gameOverMenu.SetActive(true);
    }

    public override void Update()
    {
        base.Update();
    }

    public override void Exit()
    {
        base.Exit();
    }
}


public class GameManager : MonoBehaviour
{
    public Player player;
    public AISystem aiSystem;
    public OffAxisPerspectiveProjection projection;

    public GameObject gameOverMenu;
    public TMP_Text timePlayedUI;
    public TMP_Text scoreUI;

    public readonly FiniteStateMachine fsm = new ();

    private void Awake()
    {
        fsm.AddState(new GameState("GameState", this));
        fsm.AddState(new GameOverState("GameOverState", this));
    }

    private void Start()
    {
        StartGame();
    }

    public void StartGame()
    {
        fsm.SwitchState("GameState");
    }

    private void Update()
    {
        fsm.Update();
    }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
         Application.Quit();
#endif
    }
}
