using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class InimigoPerseguidor : MonoBehaviour
{
    [Header("Alvo")]
    [SerializeField] private Transform jogador;
    [SerializeField] private VidaPersonagem vidaJogador;

    [Header("Animação")]
    [SerializeField] private Animator animator;
    [SerializeField] private string animacaoIdle = "Idle1";
    [SerializeField] private string animacaoAndar = "WalkF";
    [SerializeField] private string animacaoCorrer = "Run";
    [SerializeField] private string[] animacoesAtaque = new string[] { "Attack1", "Attack2", "Attack3" };

    [Header("Movimentação")]
    [SerializeField] private float distanciaPerseguicao = 30f;
    [SerializeField] private float distanciaCorrida = 8f;
    [SerializeField] private float velocidadeCaminhada = 2.5f;
    [SerializeField] private float velocidadeCorrida = 5.0f;

    [Header("Combate Letal")]
    [Tooltip("Distância máxima para aplicar o ataque fatal no jogador.")]
    [SerializeField] private float distanciaAtaque = 2.2f;
    [SerializeField] private float danoLetal = 9999f;
    [SerializeField] private bool matarInstantaneo = true;

    private NavMeshAgent agente;
    private bool jaAtacou;
    private string animacaoAtual;

    private void Awake()
    {
        agente = GetComponent<NavMeshAgent>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        GarantirColisorTrigger();
    }

    private void Start()
    {
        LocalizarJogadorSeNecessario();
        TocarAnimacao(animacaoIdle, 0.1f);
    }

    private void GarantirColisorTrigger()
    {
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            CapsuleCollider cap = gameObject.AddComponent<CapsuleCollider>();
            cap.center = new Vector3(0f, 1f, 0f);
            cap.radius = 1.0f;
            cap.height = 2.2f;
            cap.isTrigger = true;
        }
        else
        {
            col.isTrigger = true;
        }
    }

    private void LocalizarJogadorSeNecessario()
    {
        if (jogador == null)
        {
            GameObject objJogador = GameObject.Find("Ch18_nonPBR") ?? GameObject.FindWithTag("Player");
            if (objJogador != null)
            {
                jogador = objJogador.transform;
                vidaJogador = objJogador.GetComponent<VidaPersonagem>();
            }
        }
        else if (vidaJogador == null)
        {
            vidaJogador = jogador.GetComponent<VidaPersonagem>();
        }
    }

    private void Update()
    {
        if (jogador == null)
        {
            LocalizarJogadorSeNecessario();
            if (jogador == null) return;
        }

        // Se já atacou ou o jogador morreu, para de perseguir
        if (jaAtacou || (vidaJogador != null && vidaJogador.EstaMorto))
        {
            if (agente != null && agente.isOnNavMesh)
            {
                agente.isStopped = true;
                agente.velocity = Vector3.zero;
            }
            return;
        }

        float distancia = Vector3.Distance(transform.position, jogador.position);

        // Ataque fatal por proximidade física
        if (distancia <= distanciaAtaque)
        {
            ExecutarAtaqueFatal();
            return;
        }

        // Perseguição se estiver no raio de visão
        if (distancia <= distanciaPerseguicao)
        {
            if (agente != null && agente.isOnNavMesh)
            {
                agente.isStopped = false;
                agente.speed = distancia > distanciaCorrida ? velocidadeCorrida : velocidadeCaminhada;
                agente.SetDestination(jogador.position);
            }
            else
            {
                // Fallback sem NavMesh: move o transform diretamente
                Vector3 direcao = (jogador.position - transform.position).normalized;
                direcao.y = 0f;
                if (direcao.sqrMagnitude > 0.001f)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direcao), 6f * Time.deltaTime);
                    transform.position += direcao * (distancia > distanciaCorrida ? velocidadeCorrida : velocidadeCaminhada) * Time.deltaTime;
                }
            }

            // Alterna animações de corrida ou caminhada
            if (distancia > distanciaCorrida)
            {
                TocarAnimacao(animacaoCorrer, 0.2f);
            }
            else
            {
                TocarAnimacao(animacaoAndar, 0.2f);
            }
        }
        else
        {
            // Ocioso fora de alcance
            if (agente != null && agente.isOnNavMesh)
            {
                agente.isStopped = true;
            }

            TocarAnimacao(animacaoIdle, 0.3f);
        }
    }

    private void OnTriggerEnter(Collider outro)
    {
        if (jaAtacou) return;

        if (outro.CompareTag("Player") || outro.gameObject == jogador?.gameObject)
        {
            ExecutarAtaqueFatal();
        }
    }

    private void OnCollisionEnter(Collision col)
    {
        if (jaAtacou) return;

        if (col.gameObject.CompareTag("Player") || col.gameObject == jogador?.gameObject)
        {
            ExecutarAtaqueFatal();
        }
    }

    private void ExecutarAtaqueFatal()
    {
        if (jaAtacou) return;
        jaAtacou = true;

        if (agente != null && agente.isOnNavMesh)
        {
            agente.isStopped = true;
            agente.velocity = Vector3.zero;
        }

        // Gira para encarar o jogador diretamente no momento do impacto
        if (jogador != null)
        {
            Vector3 dir = (jogador.position - transform.position);
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(dir);
            }
        }

        // Dispara uma das animações de ataque reais da besta (Attack1, Attack2, Attack3)
        if (animacoesAtaque != null && animacoesAtaque.Length > 0)
        {
            string golpeSorteado = animacoesAtaque[Random.Range(0, animacoesAtaque.Length)];
            TocarAnimacao(golpeSorteado, 0.1f);
        }

        if (vidaJogador != null)
        {
            Debug.Log("[InimigoPerseguidor] Ataque fatal desferido com sucesso contra o jogador!");
            vidaJogador.ReceberDano(matarInstantaneo ? 9999f : danoLetal);
        }
    }

    private void TocarAnimacao(string nomeAnimacao, float tempoTransicao = 0.2f)
    {
        if (animator == null || string.IsNullOrEmpty(nomeAnimacao) || animacaoAtual == nomeAnimacao)
            return;

        int hash = Animator.StringToHash(nomeAnimacao);
        if (animator.HasState(0, hash))
        {
            animacaoAtual = nomeAnimacao;
            animator.CrossFadeInFixedTime(nomeAnimacao, tempoTransicao);
        }
    }
}
