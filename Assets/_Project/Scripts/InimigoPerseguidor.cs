using System.Collections;
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

    [Header("Tempo e Reação")]
    [Tooltip("Tempo de espera ocioso inicial ao iniciar o jogo (permite ao jogador andar e se posicionar).")]
    [SerializeField] private float tempoEsperaInicial = 3.5f;
    [Tooltip("Tempo de preparação do ataque (windup) permitindo ao jogador ver o golpe e tentar desviar.")]
    [SerializeField] private float tempoPreparoAtaque = 0.7f;

    [Header("Combate Letal")]
    [Tooltip("Distância máxima para aplicar o ataque fatal no jogador.")]
    [SerializeField] private float distanciaAtaque = 2.2f;
    [SerializeField] private float danoLetal = 9999f;
    [SerializeField] private bool matarInstantaneo = true;

    [Header("Áudio")]
    [SerializeField] private AudioClip somAtaque;
    [SerializeField, Range(0f, 1f)] private float volumeAtaque = 0.9f;

    private NavMeshAgent agente;
    private bool jaAtacou;
    private bool preparandoAtaque;
    private float cronometroEspera;
    private float cronometroAtaque;
    private string animacaoAtual;

    private void Awake()
    {
        agente = GetComponent<NavMeshAgent>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        GarantirColisorTrigger();
        cronometroEspera = tempoEsperaInicial;
    }

    private void OnEnable()
    {
        VidaPersonagem.AoJogadorRenascer += AoRenascerJogador;
    }

    private void OnDisable()
    {
        VidaPersonagem.AoJogadorRenascer -= AoRenascerJogador;
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

    private void AoRenascerJogador()
    {
        jaAtacou = false;
        preparandoAtaque = false;
        cronometroEspera = 2.5f; // Dá 2.5s de trégua ao renascer
        TocarAnimacao(animacaoIdle, 0.2f);
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
            PararMovimento();
            if (jaAtacou && !preparandoAtaque)
            {
                TocarAnimacao(animacaoIdle, 0.3f);
            }
            return;
        }

        // Período de carência inicial para o jogador andar livremente
        if (cronometroEspera > 0f)
        {
            cronometroEspera -= Time.deltaTime;
            PararMovimento();
            TocarAnimacao(animacaoIdle, 0.2f);
            return;
        }

        float distancia = Vector3.Distance(transform.position, jogador.position);

        // Se está no meio do preparo do ataque fatal
        if (preparandoAtaque)
        {
            PararMovimento();
            EncararJogador();

            cronometroAtaque -= Time.deltaTime;
            if (cronometroAtaque <= 0f)
            {
                preparandoAtaque = false;
                // Aplica o dano se o jogador não tiver fugido para longe
                if (distancia <= distanciaAtaque + 1.0f)
                {
                    DesferirDanoFatal();
                }
            }
            return;
        }

        // Início do ataque fatal por proximidade física
        if (distancia <= distanciaAtaque)
        {
            IniciarPreparoAtaque();
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
            PararMovimento();
            TocarAnimacao(animacaoIdle, 0.3f);
        }
    }

    private void IniciarPreparoAtaque()
    {
        if (jaAtacou || preparandoAtaque) return;

        preparandoAtaque = true;
        cronometroAtaque = tempoPreparoAtaque;

        PararMovimento();
        EncararJogador();

        if (somAtaque != null)
        {
            Vector3 posAudio = Camera.main != null ? Camera.main.transform.position : transform.position;
            AudioSource.PlayClipAtPoint(somAtaque, posAudio, volumeAtaque);
        }

        // Dispara uma das animações de ataque reais da besta (Attack1, Attack2, Attack3)
        if (animacoesAtaque != null && animacoesAtaque.Length > 0)
        {
            string golpeSorteado = animacoesAtaque[Random.Range(0, animacoesAtaque.Length)];
            TocarAnimacao(golpeSorteado, 0.1f);
        }
    }

    private void DesferirDanoFatal()
    {
        if (jaAtacou) return;
        jaAtacou = true;

        PararMovimento();
        EncararJogador();

        if (vidaJogador != null)
        {
            Debug.LogWarning("[InimigoPerseguidor] Ataque fatal desferido contra o jogador!");
            vidaJogador.ReceberDano(matarInstantaneo ? 9999f : danoLetal);
        }
    }

    private void EncararJogador()
    {
        if (jogador != null)
        {
            Vector3 dir = (jogador.position - transform.position);
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(dir);
            }
        }
    }

    private void PararMovimento()
    {
        if (agente != null && agente.isOnNavMesh)
        {
            agente.isStopped = true;
            agente.velocity = Vector3.zero;
        }
    }

    private void OnTriggerEnter(Collider outro)
    {
        if (jaAtacou || cronometroEspera > 0f) return;

        if (outro.CompareTag("Player") || outro.gameObject == jogador?.gameObject)
        {
            IniciarPreparoAtaque();
        }
    }

    private void OnCollisionEnter(Collision col)
    {
        if (jaAtacou || cronometroEspera > 0f) return;

        if (col.gameObject.CompareTag("Player") || col.gameObject == jogador?.gameObject)
        {
            IniciarPreparoAtaque();
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
