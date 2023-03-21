using System;
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
    }

    public override void Update()
    {
        var destination = _ai.system.GetDestination();
        if (!destination)
            return;

        destination.Allocate(_ai.collider);
        _ai.SetDestination(destination.transform);
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
    }

    public override void Update()
    {
        if (Mathf.Approximately(Vector3.Distance(_ai.agent.pathEndPosition, _ai.transform.position) - 1.0f, 0.0f))
            _ai.fsm.SwitchState("Fighting");
    }

    public override void Exit()
    {
    }
}

public class FightingState : State
{
    private AIBehavior _ai;

    public FightingState(string name, AIBehavior ai) : base(name)
    {
        _ai = ai;
    }

    public override void Enter()
    {
    }

    public override void Update()
    {
        var targetDelta = _ai.player.transform.position - _ai.gameObject.transform.position;
        // var targetDistance = targetDelta.magnitude;
        var targetDirection = targetDelta.normalized;

        var yaw = -90.0f + Mathf.Rad2Deg * Mathf.Atan2(-targetDirection.z, targetDirection.x);
        var pitch = 180.0f + Mathf.Rad2Deg * Mathf.Asin(targetDirection.y);

        var gun = _ai.gameObject.transform.GetChild(0).transform;
        gun.rotation = Quaternion.Euler(pitch, yaw, 0.0f);
    }

    public override void Exit()
    {
    }
}

public class DeathState : State
{
    public DeathState(string name) : base(name)
    {
    }

    public override void Enter()
    {
    }

    public override void Update()
    {
    }

    public override void Exit()
    {
    }
}


public class AIBehavior : PooledObject
{
    public NavMeshAgent agent;
    public Collider collider;

    [HideInInspector] public AISystem system;
    public readonly FiniteStateMachine fsm = new ();
    [HideInInspector] public GameObject player = null;

    private Transform _destination = null;
    
    protected override void OnConstruction()
    {
        base.OnConstruction();

        fsm.AddState(new IdleState("Idle", this));
        fsm.AddState(new RunningState("Running", this));
        fsm.AddState(new FightingState("Fighting", this));
        fsm.AddState(new DeathState("Death"));
    }

    protected override void Init()
    {
        base.Init();

        _destination = null;
        player = GameObject.FindWithTag("Player");

        fsm.SwitchState("Idle");
    }

    public void SetDestination(Transform destination)
    {
        _destination = destination;
    }

    public void Update()
    {
        fsm.Update();

        if (_destination)
            agent.destination = _destination.position;
    }

    private void OnDrawGizmos()
    {
        var targetDelta = player.transform.position - gameObject.transform.position;
        var targetDirection = targetDelta.normalized;
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, targetDirection * 100.0f);
    }
}
