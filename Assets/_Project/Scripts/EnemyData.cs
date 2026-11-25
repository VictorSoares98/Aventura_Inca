using UnityEngine;

// Crie um asset por tipo de inimigo: Create > Enemies > Enemy Data
[CreateAssetMenu(menuName = "Enemies/Enemy Data", fileName = "NewEnemyData")]
public class EnemyData : ScriptableObject
{
    [Header("Vida")]
    public float maxHealth = 100f;
    public float corpseTime = 5f;

    [Header("Movimento")]
    public float patrolSpeed = 1.5f;
    public float chaseSpeed = 3.5f;
    public float patrolRadius = 8f;
    public float patrolWaitTime = 2f;

    [Header("Percepção")]
    public float detectionRange = 10f;
    public float loseRange = 15f;

    [Header("Ataque")]
    public float attackRange = 2f;
    public float damage = 10f;
    public float attackCooldown = 1.5f;
    public float damageDelay = 0.4f;
    public float attackDuration = 1.0f;

    [Header("Reação a dano")]
    public float hitStunTime = 0.4f;

    [Header("Nomes dos ESTADOS no Animator (exatamente como aparecem)")]
    public string idleState = "Idle1";
    public string walkState = "WalkF";
    public string runState = "Run";
    public string[] attackStates = { "Attack1", "Attack2" };   // sorteia um a cada ataque
    public string[] hitStates = { "GetHitL", "GetHitR" };      // sorteia ao levar dano
    public string[] deathStates = { "Death1", "Death2" };     // sorteia ao morrer
}
