using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;


public class AISystem : PooledObject.ObjectPool
{
    [Serializable]
    public class SpawnZone
    {
        public string name;
        public ExclusiveColliderZone[] spawns;
        public ExclusiveColliderZone[] destinations;

        public bool SpawnAvailable()
        {
            return spawns.All(spawn => spawn.IsFree());
        }

        public bool DestinationAvailable()
        {
            return destinations.All(dest => dest.IsFree());
        }

        public ExclusiveColliderZone GetAvailableSpawn()
        {
            if (!SpawnAvailable())
                return null;

            var colliderZone = new List<ExclusiveColliderZone>();
            foreach (var spawn in spawns)
            {
                if (spawn.IsFree())
                {
                    colliderZone.Add(spawn);
                }
            }
            
            var index = Random.Range(0, colliderZone.Count);
            return colliderZone[index];
        }

        public ExclusiveColliderZone GetClosestDestination(ExclusiveColliderZone spawn)
        {
            if (!DestinationAvailable())
                return null;

            var from = spawn.transform.position;
            ExclusiveColliderZone result = null;
            foreach (var dest in destinations)
            {
                if (!dest.IsFree())
                    continue;

                if (result is null)
                {
                    result = dest;
                    continue;                    
                }

                if (Vector3.Distance(result.transform.position, from) > Vector3.Distance(dest.transform.position, from))
                    result = dest;
            }

            return result;
        }
    }

    [Header("Spawning")]
    public ExclusiveColliderZone[] spawns;
    public float spawnInterval = 5.0f;
    public int maxEnemies = 5;
    public ExclusiveColliderZone[] aiDestinations;

    public SpawnZone[] spawnZones;

    private List<PooledObject> _enemies;
    private CountdownTimer _spawnCountdown;

    private List<Vector3> debugDraw = new List<Vector3>();

    protected override void Awake()
    {
        base.Awake();

        _enemies = new List<PooledObject>();
        _spawnCountdown = new CountdownTimer(spawnInterval);
    }

    private void Start()
    {
        SpawnEnemy(spawnZones[0]);  // TODO: Fix this

        _spawnCountdown.Start();
    }

    private void OnEnable()
    {
        _spawnCountdown.Timeout += OnSpawnCountdown;
        // Projectile.EnvironmentHit += OnEnvironmentHit;
    }

    private void OnDisable()
    {
        _spawnCountdown.Timeout -= OnSpawnCountdown;
        // Projectile.EnvironmentHit -= OnEnvironmentHit;
    }

    private void Update()
    {
        _spawnCountdown.Update(Time.deltaTime);
        // if (LiveSize() < spawns.Length && _spawnEnemy)
        // {
        //     
        // }
    }

    private SpawnZone GetNextSpawn()
    {
        var zones = new List<SpawnZone>();
        foreach (var spawnZone in spawnZones)
        {
            if (!spawnZone.SpawnAvailable() || !spawnZone.DestinationAvailable())
                continue;

            zones.Add(spawnZone);
        }

        if (zones.Count == 0)
            return null;

        var index = Random.Range(0, zones.Count);
        return zones[index];
    }

    // public ExclusiveColliderZone GetDestination()
    // {
    //     var destinationZones = new List<ExclusiveColliderZone>();
    //     foreach (var destination in aiDestinations)
    //     {
    //         if (destination.IsFree())
    //         {
    //             destinationZones.Add(destination);
    //         }
    //     }
    //
    //     var index = Random.Range(0, destinationZones.Count);
    //     return destinationZones[index];
    // }

    private void SpawnEnemy(SpawnZone spawnZone)
    {
        var spawn = spawnZone.GetAvailableSpawn();
        if (spawn is null)
        {
            Debug.LogError("Unable to get valid ExclusiveColliderZone spawn!");
            return;
        }

        var enemy = Create(spawn.transform.position, spawn.transform.rotation);
        _enemies.Add(enemy);

        var ai = enemy.gameObject.GetComponent<AIBehavior>();
        ai.system = this;

        // TODO: Instead, give reference of SpawnZone to AIBehavior
        //   An AIBehavior allocates a spawn ExclusiveColliderZone, either it is freed when exiting or we call free on death
        // spawn.Allocate(ai.collider);

        ai.SetSpawn(spawnZone, spawn);
    }

    private void OnSpawnCountdown()
    {
        // Debug.Log($"{GetType().Name}.OnSpawnCountdown: Countdown timeout.");
        if (LiveSize() >= maxEnemies)
        {
            // _spawnCountdown.Stop(); // TODO: This is only temporary
            return;
        }

        // Debug.Log($"{GetType().Name}.OnSpawnCountdown: Spawning Enemy!");
        var spawnZone = GetNextSpawn();
        if (spawnZone is null)
        {
            Debug.LogError("Unable to get valid SpawnZone!");
            return;
        }

        SpawnEnemy(spawnZone);  // TODO: Fix this
    }

    private void OnEnvironmentHit(Vector3 position)
    {
        debugDraw.Add(position);

        const float hitRadius = 3.0f;  // TODO: Put this somewhere else?
        foreach (var enemy in _enemies)
        {
            var aiPos = enemy.transform.position;
            var aiHitDelta = hitRadius - Vector3.Distance(position, aiPos);
            if (aiHitDelta <= 0.0f)
                continue;

            var dmgFactor = Mathf.Max(0.0f, aiHitDelta) / hitRadius;
            var dmgPercentage = 0.5f * dmgFactor;

            var ai = enemy.GetComponent<AIBehavior>();
            ai.health.TakeDamage(dmgPercentage);

            Debug.Log($"-------- Enemy-{enemy.gameObject.name} --------");
            Debug.Log($"aiHitDelta: {aiHitDelta}");
            Debug.Log($"dmgFactor: {dmgFactor}");
            Debug.Log($"dmgPercentage: {dmgPercentage}");
        }
    }

    private void OnDrawGizmos()
    {
        for (var i = debugDraw.Count - 1; i >= 0; --i)
        {
            Gizmos.DrawSphere(debugDraw[i], 3.0f);
        }
    }
}
