using System;
using UnityEngine;
using UnityEngine.AI;


public class AIBehavior : PooledObject
{
    public NavMeshAgent agent;
    public Collider collider;

    public Transform goal;

    // private void Update()
    // {
    //     agent.destination = goal.position;
    // }
}
