using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;


public class AISystem : PooledObject.ObjectPool
{
    public ExclusiveColliderZone[] spawns;
    public float spawnInterval = 5.0f;
    public int maxEnemies = 5;
    public ExclusiveColliderZone[] aiDestinations;

    private List<PooledObject> _enemies;
    private CountdownTimer _spawnCountdown;

    protected override void Awake()
    {
        base.Awake();

        _enemies = new List<PooledObject>();
        _spawnCountdown = new CountdownTimer(spawnInterval);
    }

    private void Start()
    {
        SpawnEnemy(spawns[0]);

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

    private ExclusiveColliderZone GetNextSpawn()
    {
        var spawnZones = new List<ExclusiveColliderZone>();
        foreach (var spawn in spawns)
        {
            if (spawn.IsFree())
            {
                spawnZones.Add(spawn);
            }
        }

        var index = Random.Range(0, spawnZones.Count);
        return spawnZones[index];
    }

    public ExclusiveColliderZone GetDestination()
    {
        var destinationZones = new List<ExclusiveColliderZone>();
        foreach (var destination in aiDestinations)
        {
            if (destination.IsFree())
            {
                destinationZones.Add(destination);
            }
        }

        var index = Random.Range(0, destinationZones.Count);
        return destinationZones[index];
    }

    private void SpawnEnemy(ExclusiveColliderZone spawnZone)
    {
        var enemy = Create(spawnZone.transform.position, spawnZone.transform.rotation);
        _enemies.Add(enemy);

        var ai = enemy.gameObject.GetComponent<AIBehavior>();
        ai.system = this;

        spawnZone.Allocate(ai.collider);
    }

    private void OnSpawnCountdown()
    {
        // Debug.Log($"{GetType().Name}.OnSpawnCountdown: Countdown timeout.");
        if (LiveSize() >= maxEnemies)
            return;

        // Debug.Log($"{GetType().Name}.OnSpawnCountdown: Spawning Enemy!");
        var spawnZone = GetNextSpawn();
        SpawnEnemy(spawnZone);
    }
}
