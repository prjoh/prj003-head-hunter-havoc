using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


public class MainMenuState : State
{
    private GameManager _gameManager;

    private Vector3 _dirLightMenuRotation = new(30f, 342f, 180f);
    private Vector3 _dirLightGameRotation = new(30f, 312.1f, 180f);

    public MainMenuState(string name, GameManager gameManager) : base(name)
    {
        _gameManager = gameManager;
    }

    public override void Enter()
    {
        base.Enter();

        _gameManager.projection.enableDebugMode = false;

        _gameManager.scoreMenu.SetScoreMenuState(ScoreMenu.ScoreMenuState.VIEW);

        _gameManager.StartCoroutine(SetDirLight(_dirLightMenuRotation));
    }

    public override void Update()
    {
        base.Update();
    }

    public override void Exit()
    {
        base.Exit();

        _gameManager.StartCoroutine(SetDirLight(_dirLightGameRotation));
    }

    private IEnumerator SetDirLight(Vector3 goalAngles)
    {
        while (_gameManager.directionalLight.transform.eulerAngles != goalAngles)
        {
            var angles = Vector3.Lerp(_gameManager.directionalLight.transform.eulerAngles, goalAngles, 0.5f * Time.deltaTime);
            _gameManager.directionalLight.transform.rotation = Quaternion.Euler(angles);

            yield return null;
        }
    }
}

public class GameState : State
{
    private GameManager _gameManager;

    private float _timePlayed;
    private int _score;
    private int _displayedScore;

    private CanvasGroup _playerUICanvasGroup;

    public GameState(string name, GameManager gameManager) : base(name)
    {
        _gameManager = gameManager;
        _timePlayed = 0.0f;
        _playerUICanvasGroup = _gameManager.playerUI.GetComponent<CanvasGroup>();
    }

    public override void Enter()
    {
        base.Enter();

        _gameManager.player.Init();
        
        _gameManager.aiSystem.Init();

        _timePlayed = 0.0f;
        _gameManager.timePlayedUI.text = "Time: 00:00.0";

        _score = 0;
        _displayedScore = 0;
        _gameManager.scoreUI.text = "Score: 0";

        _gameManager.aiSystem.EnemyDied += OnEnemyDied;

        // Time.timeScale = 1.0f;

        _gameManager.StartCoroutine(SetPlayerUIVisibility(1.0f));
    }

    public override void Update()
    {
        base.Update();

        _timePlayed += Time.deltaTime;

        var timeSpan = TimeSpan.FromSeconds(_timePlayed);
        var timeString = $"{timeSpan.Minutes:00}:{timeSpan.Seconds:00}.{timeSpan.Milliseconds / 100:0}";
        _gameManager.timePlayedUI.text = $"Time: {timeString}";

        if (Input.GetKeyDown(KeyCode.Escape))
            _gameManager.pauseMenu.TogglePause();

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

        if (_score > 0)
            _gameManager.scoreBoard.UpdateScoreboard(_score, _gameManager.timePlayedUI.text);

        _gameManager.StartCoroutine(SetPlayerUIVisibility(0.0f));
        _gameManager.aiSystem.EnemyDied -= OnEnemyDied;
    }

    IEnumerator SetPlayerUIVisibility(float visibility)
    {
        while (!Mathf.Approximately(_playerUICanvasGroup.alpha, visibility))
        {
            _playerUICanvasGroup.alpha = ExtensionMethods.Lerp(_playerUICanvasGroup.alpha, visibility, 1.5f * Time.deltaTime);
            yield return null;
        }
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

        // Time.timeScale = 0.0f;
        _gameManager.aiSystem.Clear();

        // _gameManager.projection.enableDebugMode = true;

        _gameManager.scoreMenu.SetScoreMenuState(ScoreMenu.ScoreMenuState.EDITABLE);
        _gameManager.scoreMenu.Show();

        _gameManager.SaveJsonData();
    }

    public override void Update()
    {
        base.Update();
    }

    public override void Exit()
    {
        base.Exit();

        _gameManager.scoreMenu.Hide();
    }
}


public class GameManager : MonoBehaviour
{
    public Player player;
    public AISystem aiSystem;
    public OffAxisPerspectiveProjection projection;

    public GameObject directionalLight;
    public GameObject playerUI;
    public ScoreMenu scoreMenu;
    public PauseMenu pauseMenu;
    public ScoreBoard scoreBoard;

    public TMP_Text timePlayedUI;
    public TMP_Text scoreUI;

    public readonly FiniteStateMachine fsm = new ();

    private List<ISaveable> saveables = new List<ISaveable>();

    private void Awake()
    {
        fsm.AddState(new MainMenuState("MainMenuState", this));
        fsm.AddState(new GameState("GameState", this));
        fsm.AddState(new GameOverState("GameOverState", this));

        saveables.Add(scoreBoard);
    }

    private void Start()
    {
        LoadJsonData();

        ShowMenu();
    }

    public void StartGame()
    {
        fsm.SwitchState("GameState");
    }

    public void ShowMenu()
    {
        fsm.SwitchState("MainMenuState");
    }

    private void Update()
    {
        fsm.Update();
    }

    public void Quit()
    {
        SaveJsonData();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void SaveJsonData()
    {
        SaveDataManager.SaveJsonData(saveables);
    }

    public void LoadJsonData()
    {
        SaveDataManager.LoadJsonData(saveables);
    }
}
