using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;


public class AISystem : PooledObject.ObjectPool
{
    public SpawnPoint[] spawns;
    public int maxEnemies = 5;
    public float spawnInterval = 5.0f;

    private List<PooledObject> _enemies;
    private CountdownTimer _spawnCountdown;
    // private bool _spawnEnemy = true;

    protected override void Awake()
    {
        base.Awake();

        _enemies = new List<PooledObject>();
        _spawnCountdown = new CountdownTimer(spawnInterval);
    }

    private void Start()
    {
        var enemy = Create(spawns[0].transform.position, spawns[0].transform.rotation);
        _enemies.Add(enemy);

        _spawnCountdown.Start();
    }

    private void OnEnable()
    {
        _spawnCountdown.Timeout += OnSpawnCountdown;
    }

    private void OnDisable()
    {
        _spawnCountdown.Timeout -= OnSpawnCountdown;
    }

    private void Update()
    {
        _spawnCountdown.Update(Time.deltaTime);
        // if (LiveSize() < spawns.Length && _spawnEnemy)
        // {
        //     
        // }
    }

    private Transform GetNextSpawn()
    {
        var liveColliders = new List<Collider>();
        foreach (var enemy in _enemies)
        {
            var ai = enemy.gameObject.GetComponent<AIBehavior>();
            liveColliders.Add(ai.collider);
        }

        var spawnPoints = new List<SpawnPoint>();
        foreach (var spawn in spawns)
        {
            if (spawn.IsFree(liveColliders))
            {
                spawnPoints.Add(spawn);
            }
        }

        var index = Random.Range(0, spawnPoints.Count);
        return spawnPoints[index].transform;
    }
    
    private void OnSpawnCountdown()
    {
        // Debug.Log($"{GetType().Name}.OnSpawnCountdown: Countdown timeout.");
        if (LiveSize() >= maxEnemies)
            return;

        // Debug.Log($"{GetType().Name}.OnSpawnCountdown: Spawning Enemy!");
        var t = GetNextSpawn();
        var enemy = Create(t.position, t.rotation);
        _enemies.Add(enemy);
    }
}
