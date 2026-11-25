using System;
using UnityEngine;

// Serve para inimigos E para o jogador (ambos implementam IDamageable).
public class Health : MonoBehaviour, IDamageable
{
    [SerializeField] float maxHealth = 100f;

    public float Current { get; private set; }
    public float Max => maxHealth;
    public bool IsDead { get; private set; }

    public event Action<float> Damaged; // envia o dano recebido
    public event Action Died;

    void Awake() => Current = maxHealth;

    // Chamado pelo EnemyAI para aplicar a vida definida no EnemyData
    public void Init(float max)
    {
        maxHealth = max;
        Current = max;
        IsDead = false;
    }

    public void TakeDamage(float amount)
    {
        if (IsDead) return;

        Current = Mathf.Max(0f, Current - amount);
        Damaged?.Invoke(amount);

        if (Current <= 0f)
        {
            IsDead = true;
            Died?.Invoke();
        }
    }
}

