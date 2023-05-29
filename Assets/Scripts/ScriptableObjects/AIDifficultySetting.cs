using UnityEngine;


[CreateAssetMenu()]
public class AIDifficultySetting:  ScriptableObject
{
    [Header("System Settings")]
    public float timeSeconds;
    public float spawnIntervalSeconds;
    public int maxEnemies;
    public int spawnsPerCycle;
    [Header("Behavior Settings")]
    public float shootCooldown;
    public int shotsFired;
    public int health;
}
