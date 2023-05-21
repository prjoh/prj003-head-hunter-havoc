using UnityEngine;


[CreateAssetMenu()]
public class AIDifficultySetting:  ScriptableObject
{
    public float timeSeconds;
    public float spawnIntervalSeconds;
    public int maxEnemies;        
}
