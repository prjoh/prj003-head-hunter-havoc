using System;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public class SaveData
{
    [Serializable]
    public struct ScoreData
    {
        public string index;
        public string name;
        public string score;
        public string time;
    }

    public List<ScoreData> scores = new ();

    public string ToJson()
    {
        return JsonUtility.ToJson(this);
    }

    public void LoadFromJson(string a_Json)
    {
        JsonUtility.FromJsonOverwrite(a_Json, this);
    }
}

public interface ISaveable
{
    void PopulateSaveData(SaveData a_SaveData);
    void LoadFromSaveData(SaveData a_SaveData);
}
