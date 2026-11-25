using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class InimigoPerseguidor : MonoBehaviour
{
    [Header("UI da Barra de Vida (Opcional)")]
    public Image barraVidaImagem; 

    [Header("Configurações de Dano no Jogador")]
    public float danoPorGolpe = 7f;

    [Header("Configurações de Combos / Ataque")]
    public int golpesPorCombo = 3;           
    public float intervaloEntreGolpes = 0.4f; 
    public float tempoRecargaCombo = 2f;      

    [Header("Configurações de Perseguição e Patrulha")]
    public Transform jogador;
    public float distanciaLimitePerseguicao = 20f; // Afastou 20m, desiste de perseguir
    public float raioPontoAleatorio = 12f;
    public float tempoEsperadoNoPonto = 3f;        // Tempo (segundos) que fica parado antes de andar novamente

    private NavMeshAgent agente;
    private Rigidbody rb;
    private bool emPerseguicao = true;
    private bool atacando = false;
    private bool esperandoNoPonto = false;
    private float proximoCombo = 0f;

    void Start()
    {
        agente = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();

        // Força a física a nunca tombar nem inclinar o monstro
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeRotation; 
        }

        if (agente != null)
        {
            agente.updateRotation = true;
            agente.updatePosition = true;
        }
    }

    void Update()
    {
        if (jogador == null) return;

        float distanciaDoJogador = Vector3.Distance(transform.position, jogador.position);

        if (emPerseguicao)
        {
            // Se o jogador estiver até 20m, persegue
            if (distanciaDoJogador <= distanciaLimitePerseguicao)
            {
                esperandoNoPonto = false; // Cancela qualquer pausa de patrulha
                
                if (!atacando && agente != null)
                {
                    agente.isStopped = false;
                    agente.SetDestination(jogador.position);
                }
            }
            else
            {
                // Jogador afastou-se mais de 20m: desiste e entra em modo de patrulha
                emPerseguicao = false;
                IrParaPontoAleatorio();
            }
        }
        else
        {
            // Se o jogador voltar para a zona de 20m, retoma a perseguição
            if (distanciaDoJogador <= distanciaLimitePerseguicao)
            {
                emPerseguicao = true;
                esperandoNoPonto = false;
                StopAllCoroutines(); // Cancela esperas de patrulha
                atacando = false;

                if (agente != null)
                {
                    agente.isStopped = false;
                    agente.SetDestination(jogador.position);
                }
            }
            else
            {
                // Em patrulha: quando chega ao destino, espera um tempo parado e depois escolhe outro ponto
                if (agente != null && !agente.pathPending && agente.remainingDistance <= agente.stoppingDistance)
                {
                    if (!esperandoNoPonto)
                    {
                        StartCoroutine(EsperarEPatruhar());
                    }
                }
            }
        }
    }

    IEnumerator EsperarEPatruhar()
    {
        esperandoNoPonto = true;
        
        // Para o agente (Fica Parado)
        if (agente != null) agente.isStopped = true;

        // Aguarda alguns segundos parado no local
        yield return new WaitForSeconds(tempoEsperadoNoPonto);

        // Se ainda não estiver perseguindo, escolhe um novo ponto e volta a andar
        if (!emPerseguicao)
        {
            IrParaPontoAleatorio();
        }

        esperandoNoPonto = false;
    }

    void IrParaPontoAleatorio()
    {
        Vector3 direcaoAleatoria = Random.insideUnitSphere * raioPontoAleatorio;
        direcaoAleatoria += transform.position;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(direcaoAleatoria, out hit, raioPontoAleatorio, NavMesh.AllAreas))
        {
            if (agente != null)
            {
                agente.isStopped = false;
                agente.SetDestination(hit.position);
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        // APENAS ataca se estiver em modo de perseguição ativo
        if (emPerseguicao && other.CompareTag("Player") && !atacando && Time.time >= proximoCombo)
        {
            StartCoroutine(ExecutarSequenciaDeGolpes(other.gameObject));
        }
    }

    IEnumerator ExecutarSequenciaDeGolpes(GameObject playerObj)
    {
        atacando = true;
        
        if (agente != null) agente.isStopped = true;

        // Pausa temporária do controle do jogador
        ControleJogador controle = playerObj.GetComponent<ControleJogador>();
        if (controle != null) controle.enabled = false;

        VidaPersonagem vidaScript = playerObj.GetComponent<VidaPersonagem>();

        for (int i = 0; i < golpesPorCombo; i++)
        {
            if (vidaScript != null)
            {
                vidaScript.ReceberDano(danoPorGolpe);
            }
            else if (barraVidaImagem != null)
            {
                barraVidaImagem.fillAmount -= (danoPorGolpe / 100f);
            }

            yield return new WaitForSeconds(intervaloEntreGolpes);
        }

        // Devolve o controle ao jogador
        if (controle != null) controle.enabled = true;

        proximoCombo = Time.time + tempoRecargaCombo;
        
        if (agente != null && emPerseguicao) agente.isStopped = false;
        atacando = false;
    }
}