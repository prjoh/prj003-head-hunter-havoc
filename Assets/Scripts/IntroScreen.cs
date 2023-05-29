using System;
using System.Collections;
using UnityEngine;


public class IntroScreen : MonoBehaviour
{
    [Serializable]
    public enum ScreenType
    {
        PARENT,
        INTRO,
        WAIT,
        FAIL,
    }

    public GameObject parentScreen;
    public GameObject introScreen;
    public GameObject waitScreen;
    public GameObject failScreen;

    private ScreenType _currentType = ScreenType.INTRO;
    private EyeDetectorThreaded _eyeDetectorThreaded;

    [HideInInspector] public bool initSuccessful;

    private void OnEnable()
    {
        EyeDetectorThreaded.WebcamInit += OnWebcamInit;
        EyeDetectorThreaded.WebcamFailed += OnWebcamFailed;
    }

    private void OnDisable()
    {
        EyeDetectorThreaded.WebcamInit -= OnWebcamInit;
        EyeDetectorThreaded.WebcamFailed -= OnWebcamFailed;
    }

    private void Awake()
    {
        initSuccessful = false;

        _eyeDetectorThreaded = FindObjectOfType<EyeDetectorThreaded>();
    }

    private void Start()
    {
        parentScreen.GetComponent<CanvasGroup>().alpha = 1.0f;
        introScreen.GetComponent<CanvasGroup>().alpha = 0.0f;
        waitScreen.GetComponent<CanvasGroup>().alpha = 0.0f;
        failScreen.GetComponent<CanvasGroup>().alpha = 0.0f;
    }

    public void OnLetsGo()
    {
        StartCoroutine(InitCoro());
    }

    private void OnWebcamInit()
    {
        initSuccessful = true;

        StartCoroutine(FadeScreenCoro(ScreenType.PARENT, false));
    }

    private void OnWebcamFailed()
    {
        StartCoroutine(FadeFromTo(ScreenType.WAIT, ScreenType.FAIL));
    }

    public void FadeScreen(ScreenType type, bool fadeIn)
    {
        StartCoroutine(FadeScreenCoro(type, fadeIn));
    }

    private IEnumerator InitCoro()
    {
        yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);

        if (Application.HasUserAuthorization(UserAuthorization.WebCam))
        {
            _eyeDetectorThreaded.Initialize();

            StartCoroutine(FadeFromTo(ScreenType.INTRO, ScreenType.WAIT));
        }
        else
        {
            OnWebcamFailed();
        }
    }

    private IEnumerator FadeFromTo(ScreenType from, ScreenType to)
    {
        yield return StartCoroutine(FadeScreenCoro(from, false));
        yield return StartCoroutine(FadeScreenCoro(to, true));
    }

    private IEnumerator FadeScreenCoro(ScreenType type, bool fadeIn)
    {
        introScreen.SetActive(false);
        waitScreen.SetActive(false);
        failScreen.SetActive(false);
    
        var screen = type switch
        {
            ScreenType.INTRO => introScreen,
            ScreenType.WAIT => waitScreen,
            ScreenType.FAIL => failScreen,
            ScreenType.PARENT => parentScreen,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
        screen.SetActive(true);
        var screenCanvas = screen.GetComponent<CanvasGroup>();

        float fadeTo, fadeFrom;
        if (fadeIn)
        {
            fadeFrom = 0.0f;
            fadeTo = 1.0f;
        }
        else
        {
            fadeFrom = 1.0f;
            fadeTo = 0.0f;
        }

        const float time = 1.0f;
        var elapsed = 0.0f;
        while (elapsed < time)
        {
            elapsed += Time.deltaTime;
            screenCanvas.alpha = ExtensionMethods.Lerp(fadeFrom, fadeTo, elapsed / time);
            yield return null;
        }

        screenCanvas.alpha = fadeTo;

        if (type == ScreenType.PARENT)
            gameObject.SetActive(false);
    }
}
