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
    [SerializeField] private float tempoPreparoAtaque = 0.6f;
    [Tooltip("Tempo de recuperação e finalização da animação do ataque antes de retornar a Idle.")]
    [SerializeField] private float tempoRecuperacaoAtaque = 0.8f;
    [Tooltip("Intervalo mínimo de descanso entre ataques consecutivos se o jogador sobreviver.")]
    [SerializeField] private float intervaloEntreAtaques = 1.8f;

    [Header("Combate Letal")]
    [Tooltip("Distância máxima para aplicar o ataque fatal no jogador.")]
    [SerializeField] private float distanciaAtaque = 3.2f;
    [SerializeField] private float danoLetal = 9999f;
    [SerializeField] private bool matarInstantaneo = true;

    [Header("Áudio")]
    [SerializeField] private AudioClip somAtaque;
    [SerializeField, Range(0f, 1f)] private float volumeAtaque = 0.9f;

    [Header("Aderência ao Solo (Fallback sem NavMesh)")]
    [SerializeField] private float offsetAlturaSolo = 0f;
    [SerializeField] private float alturaOrigemRaycast = 5f;
    [SerializeField] private float distanciaRaycastSolo = 20f;
    [SerializeField] private float velocidadeAjusteSolo = 15f;

    private NavMeshAgent agente;
    private bool jaAtacou;
    private bool preparandoAtaque;
    private float cronometroEspera;
    private float cronometroAtaque;
    private float cronometroPosAtaque;
    private float cronometroRecargaAtaque;
    private string animacaoAtual;
    private readonly RaycastHit[] hitsChao = new RaycastHit[8];

    private void Awake()
    {
        // Salvaguarda de alcance para instâncias em cenas antigas
        if (distanciaAtaque < 3.0f)
        {
            distanciaAtaque = 3.2f;
        }

        agente = GetComponent<NavMeshAgent>();
        if (agente != null)
        {
            agente.radius = 1.0f;
            agente.height = 2.0f;
            agente.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            agente.avoidancePriority = 30 + (Mathf.Abs(GetInstanceID()) % 40);
            agente.stoppingDistance = Mathf.Max(2.3f, distanciaAtaque * 0.75f);
        }

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator != null)
        {
            animator.applyRootMotion = false;
        }

        ConfigurarColisorFisico();
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

        if (agente == null || !agente.isOnNavMesh)
        {
            AjustarAlturaAoSolo(instantaneo: true);
        }
    }

    /// <summary>
    /// Configura colisor SÓLIDO e Rigidbody cinemático.
    /// Isso impede que o CharacterController do jogador atravesse o corpo da besta.
    /// </summary>
    private void ConfigurarColisorFisico()
    {
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            CapsuleCollider cap = gameObject.AddComponent<CapsuleCollider>();
            cap.center = new Vector3(0f, 1.1f, 0f);
            cap.radius = 0.95f;
            cap.height = 2.2f;
            cap.isTrigger = false;
        }
        else
        {
            col.isTrigger = false;
            if (col is CapsuleCollider capCol)
            {
                capCol.center = new Vector3(0f, 1.1f, 0f);
                capCol.radius = 0.95f;
                capCol.height = 2.2f;
            }
        }

        // Rigidbody cinemático para que o PhysX impeça a penetração do CharacterController
        // sem causar interferência na navegação do NavMeshAgent
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
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
        cronometroPosAtaque = 0f;
        cronometroRecargaAtaque = 0f;
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

        // Se o jogador já faleceu, cessa qualquer perseguição e mantém postura vitoriosa/idle
        if (vidaJogador != null && vidaJogador.EstaMorto)
        {
            PararMovimento();
            TocarAnimacao(animacaoIdle, 0.3f);
            return;
        }

        // Atualiza temporizador de recarga entre investidas de ataque
        if (cronometroRecargaAtaque > 0f)
        {
            cronometroRecargaAtaque -= Time.deltaTime;
        }

        // Conclusão e recuperação da animação do ataque pós-dano
        if (cronometroPosAtaque > 0f)
        {
            cronometroPosAtaque -= Time.deltaTime;
            PararMovimento();
            EncararJogador();
            if (cronometroPosAtaque <= 0f)
            {
                TocarAnimacao(animacaoIdle, 0.25f);
            }
            return;
        }

        // Se o golpe desferido já liquidou o alvo permanentemente
        if (jaAtacou)
        {
            PararMovimento();
            TocarAnimacao(animacaoIdle, 0.3f);
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

        // Se está no meio do preparo do ataque fatal (windup)
        if (preparandoAtaque)
        {
            PararMovimento();
            EncararJogador();

            cronometroAtaque -= Time.deltaTime;
            if (cronometroAtaque <= 0f)
            {
                preparandoAtaque = false;
                // Aplica o dano se o jogador ainda estiver no alcance tático
                if (distancia <= distanciaAtaque + 1.2f)
                {
                    DesferirDanoFatal();
                }
                else
                {
                    // Jogador conseguiu recuar ou desviar a tempo
                    cronometroRecargaAtaque = intervaloEntreAtaques;
                    TocarAnimacao(animacaoIdle, 0.25f);
                }
            }
            return;
        }

        // Início do ataque por proximidade física com verificação de recarga
        if (distancia <= distanciaAtaque && cronometroRecargaAtaque <= 0f)
        {
            IniciarPreparoAtaque();
            return;
        }

        // Perseguição e cerco ao jogador
        if (distancia <= distanciaPerseguicao)
        {
            Vector3 destinoTatico = ObterDestinoTatico();

            if (agente != null && agente.isOnNavMesh)
            {
                agente.isStopped = false;
                agente.speed = distancia > distanciaCorrida ? velocidadeCorrida : velocidadeCaminhada;
                agente.SetDestination(destinoTatico);
            }
            else
            {
                // Fallback sem NavMesh: move o transform diretamente e acompanha o relevo do solo
                Vector3 direcao = jogador.position - transform.position;
                direcao.y = 0f;
                if (direcao.sqrMagnitude > 0.001f)
                {
                    direcao.Normalize();
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direcao), 6f * Time.deltaTime);
                    transform.position += direcao * (distancia > distanciaCorrida ? velocidadeCorrida : velocidadeCaminhada) * Time.deltaTime;
                }

                AjustarAlturaAoSolo(instantaneo: false);
            }

            // POLIMENTO CRÍTICO: Animação dirigida pela velocidade real da criatura
            // Elimina o caminhar infinito no lugar (walk in place) quando bloqueado por aliados
            float velocidadeReal = (agente != null && agente.isOnNavMesh)
                ? agente.velocity.magnitude
                : (distancia > distanciaCorrida ? velocidadeCorrida : velocidadeCaminhada);

            if (velocidadeReal < 0.2f)
            {
                // Criatura obstruída por aliados, parada no destino ou esperando passagem
                TocarAnimacao(animacaoIdle, 0.25f);
            }
            else if (velocidadeReal > (velocidadeCaminhada + 0.6f) || (distancia > distanciaCorrida && velocidadeReal > 1.0f))
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

    /// <summary>
    /// Distribui os múltiplos inimigos ao redor do jogador em ângulos de cerco (Pack Hunting),
    /// impedindo aglomeração cega em fila indiana.
    /// </summary>
    private Vector3 ObterDestinoTatico()
    {
        if (jogador == null) return transform.position;

        float dist = Vector3.Distance(transform.position, jogador.position);
        if (dist > distanciaCorrida)
        {
            return jogador.position;
        }

        int id = Mathf.Abs(GetInstanceID());
        float anguloOffset = ((id % 6) * 50f) - 125f;

        Vector3 vetorRelativo = transform.position - jogador.position;
        vetorRelativo.y = 0f;
        if (vetorRelativo.sqrMagnitude < 0.01f) vetorRelativo = Vector3.forward;
        vetorRelativo.Normalize();

        Vector3 dirSlot = Quaternion.Euler(0f, anguloOffset * 0.45f, 0f) * vetorRelativo;
        Vector3 posSlot = jogador.position + (dirSlot * (distanciaAtaque * 0.8f));

        if (NavMesh.SamplePosition(posSlot, out NavMeshHit hit, 2.5f, NavMesh.AllAreas))
        {
            return hit.position;
        }

        return jogador.position;
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
        cronometroPosAtaque = tempoRecuperacaoAtaque;
        PararMovimento();
        EncararJogador();

        if (vidaJogador != null)
        {
            Debug.LogWarning("[InimigoPerseguidor] Ataque desferido contra o jogador!");
            vidaJogador.ReceberDano(matarInstantaneo ? 9999f : danoLetal);

            if (matarInstantaneo || vidaJogador.EstaMorto)
            {
                jaAtacou = true;
            }
            else
            {
                cronometroRecargaAtaque = intervaloEntreAtaques;
            }
        }
        else
        {
            cronometroRecargaAtaque = intervaloEntreAtaques;
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
                Quaternion rotAlvo = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.Slerp(transform.rotation, rotAlvo, 12f * Time.deltaTime);
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

    private void AjustarAlturaAoSolo(bool instantaneo)
    {
        Vector3 pos = transform.position;
        Vector3 origem = new Vector3(pos.x, pos.y + alturaOrigemRaycast, pos.z);
        int mascara = ~LayerMask.GetMask("Ignore Raycast");

        int totalHits = Physics.RaycastNonAlloc(
            origem,
            Vector3.down,
            hitsChao,
            distanciaRaycastSolo,
            mascara,
            QueryTriggerInteraction.Ignore
        );

        bool encontrouChao = false;
        float menorDistancia = float.MaxValue;
        float yChao = pos.y;

        for (int i = 0; i < totalHits; i++)
        {
            Collider col = hitsChao[i].collider;
            if (col == null || col.transform.IsChildOf(transform))
                continue;

            if (jogador != null && col.transform.IsChildOf(jogador))
                continue;

            if (hitsChao[i].distance < menorDistancia)
            {
                menorDistancia = hitsChao[i].distance;
                yChao = hitsChao[i].point.y;
                encontrouChao = true;
            }
        }

        if (!encontrouChao && Terrain.activeTerrain != null)
        {
            yChao = Terrain.activeTerrain.SampleHeight(pos) + Terrain.activeTerrain.transform.position.y;
            encontrouChao = true;
        }

        if (!encontrouChao)
            return;

        float yAlvo = yChao + offsetAlturaSolo;
        pos.y = instantaneo
            ? yAlvo
            : Mathf.Lerp(pos.y, yAlvo, velocidadeAjusteSolo * Time.deltaTime);

        transform.position = pos;
    }
}
