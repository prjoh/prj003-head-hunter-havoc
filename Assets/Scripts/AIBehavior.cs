using System.Collections;
using UnityEngine;
using UnityEngine.AI;


public class IdleState : State
{
    private AIBehavior _ai;

    public IdleState(string name, AIBehavior ai) : base(name)
    {
        _ai = ai;
    }

    public override void Enter()
    {
        _ai.animator.Play("Idle");
    }

    public override void Update()
    {
        if (!_ai.health.IsAlive()) 
            _ai.fsm.SwitchState("Death");

        // var destination = _ai.system.GetDestination();
        // if (!destination)
        if (_ai.zone is null || _ai.spawn is null)
            return;

        _ai.destination = _ai.zone.GetAvailableDestination();
        _ai.destination.Allocate(_ai.collider);

        _ai.agent.SetDestination(_ai.destination.transform.position);

        // _ai.SetDestination(destination.transform);
        _ai.fsm.SwitchState("Running");
    }

    public override void Exit()
    {
    }
}

public class RunningState : State
{
    private AIBehavior _ai;

    public RunningState(string name, AIBehavior ai) : base(name)
    {
        _ai = ai;
    }

    public override void Enter()
    {
        _ai.animator.Play("Running");
    }

    public override void Update()
    {
        if (!_ai.health.IsAlive()) 
            _ai.fsm.SwitchState("Death");

        if (_ai.agent.destination != _ai.agent.pathEndPosition)
            return;

        // var dist = _ai.agent.remainingDistance;
        // if (!float.IsPositiveInfinity(dist) && _ai.agent.pathStatus==NavMeshPathStatus.PathComplete && dist == 0)
            // _ai.fsm.SwitchState("Fighting");
        
        if (Mathf.Approximately(Vector3.Distance(_ai.agent.pathEndPosition, _ai.transform.position), 0.0f))
            _ai.fsm.SwitchState("Fighting");
    }

    public override void Exit()
    {
    }
}

public class FightingState : State
{
    private AIBehavior _ai;
    private Vector3 _targetDirection;
    private CountdownTimer _shootTimer;
    private Color _projectileColor;

    public FightingState(string name, AIBehavior ai) : base(name)
    {
        _ai = ai;
        _shootTimer = new CountdownTimer();
        _shootTimer.Stop();
        _projectileColor = new Color(191, 46, 0) * 0.0115f;
    }

    public override void Enter()
    {
        _ai.animator.Play("Idle");

        _shootTimer.Timeout += OnShootTimeout;
        _shootTimer.Start(_ai.shootCooldown + Random.Range(-0.25f, 0.25f));
    }

    public override void Update()
    {
        if (!_ai.health.IsAlive()) 
            _ai.fsm.SwitchState("Death");

        var targetDelta = _ai.player.transform.position - _ai.gameObject.transform.position;
        // // var targetDistance = targetDelta.magnitude;
        _targetDirection = targetDelta.normalized;
        //
        var yaw = 90.0f + Mathf.Rad2Deg * Mathf.Atan2(-_targetDirection.z, _targetDirection.x);
        // var pitch = 180.0f + Mathf.Rad2Deg * Mathf.Asin(_targetDirection.y);
        //
        // var gun = _ai.gameObject.transform.GetChild(0).transform;
        // gun.rotation = Quaternion.Euler(pitch, yaw, 0.0f);
        _ai.gameObject.transform.rotation = Quaternion.Euler(0.0f, yaw, 0.0f);

        _shootTimer.Update(Time.deltaTime);
    }

    public override void Exit()
    {
        _shootTimer.Timeout -= OnShootTimeout;
    }

    private void OnShootTimeout()
    {
        // _ai.projectilePool.Shoot(_ai.projectileSpawn.position, _targetDirection, 1600.0f, 0.5f, _projectileColor, "Player");
        // _ai.projectileLauncher.Shoot();
        _shootTimer.Stop();
        _ai.StartCoroutine(ShootCoroutine());
    }

    private IEnumerator ShootCoroutine()
    {
        var projectileLauncherTransform = _ai.projectileLauncher.gameObject.transform;
        projectileLauncherTransform.SetParent(_ai.enemyWeaponSkin.GetCurrentWeapon().transform.GetChild(0), true);
        projectileLauncherTransform.localScale = Vector3.one;
        projectileLauncherTransform.localPosition = Vector3.zero;
        projectileLauncherTransform.localRotation = Quaternion.identity;

        _ai.animator.Play("Shoot");

        while (!_ai.animator.GetCurrentAnimatorStateInfo(0).IsName("Shoot"))
            yield return null;

        while (_ai.animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 0.35f)
            yield return null;
        
        projectileLauncherTransform.SetParent(_ai.gameObject.transform, true);
        projectileLauncherTransform.localScale = Vector3.one;
        
        for (var i = 0; i < _ai.shotsFired; i++)
        {
            if (!_ai.health.IsAlive())
                yield break;

            var direction = (_ai.player.transform.position - projectileLauncherTransform.transform.position).normalized;
            projectileLauncherTransform.rotation = Quaternion.LookRotation(direction);

            _ai.animator.Play("Shoot", -1, 0.35f);
            _ai.projectileLauncher.Shoot();

            _ai.shootSound.Play();

            yield return new WaitForSeconds(1.3f);
        }

        while (_ai.animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 0.9f)
            yield return null;

        _ai.animator.CrossFade("Idle", 0.1f);

        _shootTimer.Start();
    }
}

public class DeathState : State
{
    private AIBehavior _ai;
    private CountdownTimer _deathTimer;

    public DeathState(string name, AIBehavior ai) : base(name)
    {
        _ai = ai;
        _deathTimer = new CountdownTimer(10.0f);
    }

    public override void Enter()
    {
        _deathTimer.Timeout += Exit;
        _deathTimer.Start();

        _ai.deathSound.pitch = Random.Range(1.8f, 2.0f);
        _ai.deathSound.Play();

        _ai.system.EmitEnemyDied();
    }

    public override void Update()
    {
        _deathTimer.Update(Time.deltaTime);
    }

    public override void Exit()
    {
        _deathTimer.Timeout -= Exit;

        _ai.Destroy();
    }
}


public class AIBehavior : PooledObject
{
    public NavMeshAgent agent;
    public Collider collider;
    public EnemyWeaponSkin enemyWeaponSkin;
    public Animator animator;

    public string currentState = "";

    [HideInInspector] public AISystem system;
    public readonly FiniteStateMachine fsm = new ();
    [HideInInspector] public GameObject player = null;
    // [HideInInspector] public ProjectilePool projectilePool;
    [HideInInspector] public HealthComponent health;
    public ProjectileLauncher projectileLauncher;
    public RagdollController ragdollController;

    [HideInInspector] public AISystem.SpawnZone zone = null;
    [HideInInspector] public ExclusiveColliderZone spawn = null;
    [HideInInspector] public ExclusiveColliderZone destination = null;

    public float shootCooldown;
    public int shotsFired;
    public int healthMax;
    
    // pitch: 1.1 ~ 1.7
    // volume: 0.0 ~ 0.4
    public AudioSource hitSound;
    public AudioSource shootSound;
    public AudioSource hurtSound;
    public AudioSource deathSound;

    protected override void OnConstruction()
    {
        base.OnConstruction();

        fsm.AddState(new IdleState("Idle", this));
        fsm.AddState(new RunningState("Running", this));
        fsm.AddState(new FightingState("Fighting", this));
        fsm.AddState(new DeathState("Death", this));

        // projectilePool = FindObjectOfType<ProjectilePool>();
        // if (projectilePool == null)
            // Debug.LogError($"{GetType().Name}.OnConstruction: No ProjectilePool found! Please make sure a ProjectilePool is in your Scene.");
        
        health = new HealthComponent();
            
        var projectileLauncherTransform = projectileLauncher.gameObject.transform;
        projectileLauncherTransform.SetParent(enemyWeaponSkin.GetCurrentWeapon().transform.GetChild(0));
        projectileLauncherTransform.localScale = Vector3.one;
        projectileLauncherTransform.localPosition = Vector3.zero;
        projectileLauncherTransform.localRotation = Quaternion.identity;

        system = FindObjectOfType<AISystem>();
    }

    protected override void Init()
    {
        base.Init();

        zone = null;
        spawn = null;
        destination = null;

        agent.ResetPath();

        player = GameObject.FindWithTag("Player");

        shootCooldown = system.currentDifficulty.shootCooldown;
        shotsFired = system.currentDifficulty.shotsFired;
        healthMax = system.currentDifficulty.health;

        health.Init(healthMax);
        ragdollController.EnableAnimator();

        fsm.SwitchState("Idle");
    }

    public void SetSpawn(AISystem.SpawnZone _zone, ExclusiveColliderZone _spawn)
    {
        zone = _zone;
        spawn = _spawn;

        spawn.Allocate(collider);
    }

    // public void SetDestination(Transform destination)
    // {
    //     _destination = destination;
    // }

    private void OnEnable()
    {
        ProjectileLauncher.Explosion += OnExplosion;
    }

    private void OnDisable()
    {
        ProjectileLauncher.Explosion -= OnExplosion;
    }

    public void Update()
    {
        fsm.Update();
        currentState = fsm.CurrentState();  // TODO: This is only for debugging purposes

        // if (destination)
        // {
        //     if (!agent.isOnNavMesh)
        //     {
        //         Debug.LogWarning("Tried setting NavMeshAgent destination, while not on NavMesh. Retry next frame!");
        //         return;
        //     }
        //
        //     agent.destination = destination.transform.position;
        // }
    }

    private void OnExplosion(Vector3 position, string targetTag)
    {
        if (!gameObject.CompareTag(targetTag)) 
            return;

        if (!health.IsAlive())
            return;

        // aiHitDelta = 4.0f - 0.0f
        // 0.0 ~ 0.5
        const float hitRadius = 4.0f;  // TODO: Put this somewhere else?
        var aiPos = transform.position;
        var aiHitDelta = hitRadius - Vector3.Distance(position, aiPos);
        if (aiHitDelta <= 0.0f) 
            return;

        var dmgFactor = Mathf.Max(0.0f, aiHitDelta) / hitRadius;
        var dmgPercentage = 0.5f * dmgFactor;

        health.TakeDamage(dmgPercentage);

        hitSound.volume = ExtensionMethods.Map(dmgPercentage, 0.0f, 0.5f, 0.0f, 0.6f);
        hitSound.pitch = 1.7f - ExtensionMethods.Map(dmgPercentage, 0.0f, 0.5f, 0.0f, 0.3f);
        hitSound.Play();
        
        hurtSound.pitch = Random.Range(1.5f, 2.0f);
        hurtSound.Play();

        if (!health.IsAlive())
        {
            ragdollController.EnableRagdoll();
            ragdollController.AddExplosionForce(position, gameObject.tag);
        }
    }

    private void OnDrawGizmos()
    {
        var targetDelta = player.transform.position - gameObject.transform.position;
        var targetDirection = targetDelta.normalized;
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, targetDirection * 100.0f);
    }

    private void OnCollisionEnter(Collision collision)
    {
        print($"COLLISION {gameObject.name}<>{collision.gameObject.name}");
    }
}
