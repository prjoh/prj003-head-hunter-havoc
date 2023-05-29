using System.Collections;
using TMPro;
using UnityEngine;


public class LevelInfo : MonoBehaviour
{
    private RectTransform _transform;
    private CanvasGroup _canvasGroup;
    private TMP_Text _infoText;

    private void Awake()
    {
        _transform = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();
        _infoText = GetComponent<TMP_Text>();

        _transform.localScale = Vector3.one;
        _canvasGroup.alpha = 0.0f;
        _infoText.text = "Level 1";
    }

    public void UpdateLevelInfo(string infoText)
    {
        _transform.localScale = Vector3.one;
        _canvasGroup.alpha = 0.0f;
        _infoText.text = infoText;

        StartCoroutine(ShowLevelInfo());
    }

    private IEnumerator ShowLevelInfo()
    {
        var time = 2.0f;
        var elapsed = 0.0f;
        while (elapsed < time)
        {
            elapsed += Time.deltaTime;
            _canvasGroup.alpha = ExtensionMethods.Lerp(0.0f, 1.0f, elapsed / time);
            _transform.localScale = Vector3.Lerp(Vector3.one, new Vector3(1.2f, 1.2f, 1.2f), elapsed / time);
            yield return null;
        }

        time = 1.0f;
        elapsed = 0.0f;
        while (elapsed < time)
        {
            elapsed += Time.deltaTime;
            _canvasGroup.alpha = ExtensionMethods.Lerp(1.0f, 0.0f, elapsed / time);
            yield return null;
        }
    }
}
