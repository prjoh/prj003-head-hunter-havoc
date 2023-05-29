using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;


public class ScoreBoard : MonoBehaviour, ISaveable
{
    public GameObject scoreEntryPrefab;

    public Transform root;

    private List<GameObject> _scoreEntries = new ();

    // private void Update()
    // {
    //     if (Input.GetKeyDown(KeyCode.Return))
    //     {
    //        UpdateScoreboard(Random.Range(100, 9000), "00:00.0");
    //     }
    // }

    private void RemoveHighlight()
    {
        foreach (var scoreObj in _scoreEntries)
        {
            var image = scoreObj.GetComponent<Image>();
            image.enabled = false;

            var scoreEntry = scoreObj.GetComponent<ScoreEntry>();
            scoreEntry.name.readOnly = true;
            scoreEntry.name.onFocusSelectAll = false;
        }
    }

    public void UpdateScoreboard(int score, string timeText, string name = "")
    {
        RemoveHighlight();

        var ndx = GetScoreIndex(score);
        if (ndx == -1)
            return;

        var scoreObj = Instantiate(scoreEntryPrefab, root);
        var scoreTransform = scoreObj.GetComponent<RectTransform>();
        scoreTransform.SetSiblingIndex(ndx + 1);

        var scoreEntryScript = scoreObj.GetComponent<ScoreEntry>();

        scoreEntryScript.name.ActivateInputField();

        if (name.Length > 0)
            scoreEntryScript.name.text = name;
        else
            scoreEntryScript.name.text = "head hunter";
        scoreEntryScript.score.text = score.ToString();
        scoreEntryScript.time.text = timeText;

        StartCoroutine(SetCaret(scoreEntryScript.name));

        _scoreEntries.Insert(ndx, scoreObj);

        if (_scoreEntries.Count > 10)
        {
            Destroy(_scoreEntries[10]);
            _scoreEntries.RemoveAt(10);
        }

        for (var index = 0; index < _scoreEntries.Count; index++)
        {
            var scoreEntry = _scoreEntries[index].GetComponent<ScoreEntry>();
            scoreEntry.index.text = (index + 1).ToString();
        }
    }

    private int GetScoreIndex(int score)
    {
        for (var i = 0; i < _scoreEntries.Count; i++)
        {
            var scoreEntry = _scoreEntries[i].GetComponent<ScoreEntry>();
            if (int.Parse(scoreEntry.score.text) < score)
                return i;
        }

        if (_scoreEntries.Count < 10)
            return _scoreEntries.Count;

        return -1;
    }

    private IEnumerator SetCaret(TMP_InputField field)
    {
        yield return new WaitForEndOfFrame();
        field.MoveTextEnd(true);
    }

    public void PopulateSaveData(SaveData a_SaveData)
    {
        a_SaveData.scores.Clear();

        foreach (var scoreObj in _scoreEntries)
        {
            var scoreEntry = scoreObj.GetComponent<ScoreEntry>();
            var scoreData = new SaveData.ScoreData
            {
                index = scoreEntry.index.text,
                name = scoreEntry.name.text,
                score = scoreEntry.score.text,
                time = scoreEntry.time.text
            };
            a_SaveData.scores.Add(scoreData);
        }
    }

    public void LoadFromSaveData(SaveData a_SaveData)
    {
        foreach (var scoreData in a_SaveData.scores)
        {
            var scoreObj = Instantiate(scoreEntryPrefab, root);

            var scoreEntry = scoreObj.GetComponent<ScoreEntry>();
            scoreEntry.index.text = scoreData.index;
            scoreEntry.name.text = scoreData.name;
            scoreEntry.score.text = scoreData.score;
            scoreEntry.time.text = scoreData.time;

            _scoreEntries.Add(scoreObj);
        }

        RemoveHighlight();
    }
}
