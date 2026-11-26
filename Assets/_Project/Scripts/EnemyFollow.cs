using UnityEngine;

public class EnemyFollow : MonoBehaviour
{
    public Transform player;
    public UnityEngine.AI.NavMeshAgent agent;
    void Start()
    {
        
    }

   
    void Update()
    {
        agent.SetDestination(player.position);
    }
}
