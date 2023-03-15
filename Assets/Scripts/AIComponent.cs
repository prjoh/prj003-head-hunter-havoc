using UnityEngine;
using UnityEngine.AI;


public class AIComponent : MonoBehaviour
{
    public NavMeshAgent agent;

    public Transform goal;

    private void Update()
    {
        agent.destination = goal.position;
    }
}
