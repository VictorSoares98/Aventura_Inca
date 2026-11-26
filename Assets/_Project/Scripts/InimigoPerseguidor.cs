
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class InimigoPerseguidor : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private Transform jogador;
    [SerializeField] private Animator animator;

    [Header("Movimentação")]
    [SerializeField] private float distanciaCorrida = 8f;
    [SerializeField] private float velocidadeCaminhada = 2f;
    [SerializeField] private float velocidadeCorrida = 4f;

    private NavMeshAgent agente;

    private void Awake()
    {
        agente = GetComponent<NavMeshAgent>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if (jogador == null || animator == null)
            return;

        float distancia = Vector3.Distance(
            transform.position,
            jogador.position
        );

        agente.speed = distancia > distanciaCorrida
            ? velocidadeCorrida
            : velocidadeCaminhada;

        agente.SetDestination(jogador.position);

        bool movendo = agente.velocity.magnitude > 0.1f;
        bool correndo = movendo && distancia > distanciaCorrida;

        animator.SetBool("Movendo", movendo);
        animator.SetBool("Correndo", correndo);
    }
}
