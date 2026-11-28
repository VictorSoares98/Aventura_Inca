using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Trigger portal placed along the level path that advances to the next scene
/// or triggers victory when ehFinalDoJogo is enabled, decoupled via static events.
/// </summary>
[RequireComponent(typeof(Collider))]
public class PortalProximaFase : MonoBehaviour
{
    [Header("Destino")]
    [SerializeField] private string nomeCenaDestino = "Cena_Fase2_Ruinas";
    [SerializeField] private bool ehFinalDoJogo = false;

    [Header("Requisitos")]
    [SerializeField] private bool exigirObjetivoReliquias = true;
    [SerializeField] private string tagJogador = "Player";

    [Header("Aparição Dinâmica (Opcional)")]
    [Tooltip("Quando marcado, o portal move-se para frente do jogador assim que todas as relíquias forem coletadas.")]
    [SerializeField] private bool abrirPertoDoJogadorAoConcluir = false;
    [SerializeField, Min(3f)] private float distanciaAoAbrir = 7f;

    [Header("Feedback e Transição")]
    [SerializeField] private AudioClip somPortal;
    [SerializeField, Range(0f, 1f)] private float volumeSom = 0.9f;
    [SerializeField, Min(0f)] private float atrasoTransicao = 0.35f;

    /// <summary>Raised when the player enters the portal while requirements are missing or loading fails.</summary>
    public static event Action<string> AoExibirMensagemPortal;

    /// <summary>Raised when the portal message should be hidden.</summary>
    public static event Action AoOcultarMensagemPortal;

    /// <summary>Raised when the final level portal is completed.</summary>
    public static event Action AoConcluirJogo;

    private ObjetivoReliquias objetivo;
    private bool transicaoIniciada;
    private bool jogadorNoAlcance;
    private bool avisoObjetivoExibido;
    private bool inscritoObjetivo;
    private Coroutine rotinaOcultarAviso;

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
    }

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
            Debug.LogWarning($"[PortalProximaFase] Collider em '{name}' não está marcado como Is Trigger.", this);
    }

    private void OnEnable()
    {
        GarantirInscricaoObjetivo();
    }

    private void Start()
    {
        GarantirInscricaoObjetivo();
    }

    private void GarantirInscricaoObjetivo()
    {
        if (objetivo == null)
            objetivo = FindFirstObjectByType<ObjetivoReliquias>();

        if (objetivo != null && !inscritoObjetivo)
        {
            objetivo.AoProgresso += AoAtualizarObjetivo;
            inscritoObjetivo = true;
        }
    }

    private void AoAtualizarObjetivo(int coletadas, int meta)
    {
        if (!exigirObjetivoReliquias || avisoObjetivoExibido || coletadas < meta)
            return;

        avisoObjetivoExibido = true;

        string mensagem;
        if (abrirPertoDoJogadorAoConcluir)
        {
            ReposicionarPertoDoJogador();
            mensagem = "Todas as relíquias coletadas! O portal se abriu à sua frente!";
        }
        else
        {
            mensagem = "Todas as relíquias coletadas! Siga até o feixe azul do Portal para avançar!";
        }

        TocarSomPortal();
        AoExibirMensagemPortal?.Invoke(mensagem);

        if (rotinaOcultarAviso != null)
            StopCoroutine(rotinaOcultarAviso);
        rotinaOcultarAviso = StartCoroutine(RotinaOcultarAvisoDesbloqueio());
    }

    private IEnumerator RotinaOcultarAvisoDesbloqueio()
    {
        yield return new WaitForSecondsRealtime(4.5f);

        if (!jogadorNoAlcance && !transicaoIniciada)
            AoOcultarMensagemPortal?.Invoke();

        rotinaOcultarAviso = null;
    }

    private void ReposicionarPertoDoJogador()
    {
        GameObject jogador = !string.IsNullOrEmpty(tagJogador)
            ? GameObject.FindWithTag(tagJogador)
            : null;

        if (jogador == null)
        {
            ControleJogador controle = FindFirstObjectByType<ControleJogador>();
            if (controle != null)
                jogador = controle.gameObject;
        }

        if (jogador == null)
            return;

        Vector3 frente = jogador.transform.forward;
        frente.y = 0f;
        if (frente.sqrMagnitude < 0.001f)
            frente = Vector3.forward;
        frente.Normalize();

        Vector3 posAlvo = jogador.transform.position + frente * distanciaAoAbrir;
        Vector3 origemRay = new Vector3(posAlvo.x, jogador.transform.position.y + 30f, posAlvo.z);
        int mascara = ~LayerMask.GetMask("Ignore Raycast");

        if (Physics.Raycast(origemRay, Vector3.down, out RaycastHit hit, 80f, mascara, QueryTriggerInteraction.Ignore))
        {
            posAlvo.y = hit.point.y;
        }
        else if (Terrain.activeTerrain != null)
        {
            posAlvo.y = Terrain.activeTerrain.SampleHeight(posAlvo) + Terrain.activeTerrain.transform.position.y;
        }

        transform.position = posAlvo;

        Vector3 dirParaJogador = jogador.transform.position - posAlvo;
        dirParaJogador.y = 0f;
        if (dirParaJogador.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(dirParaJogador);
    }

    private void OnTriggerEnter(Collider outro)
    {
        if (transicaoIniciada || !outro.CompareTag(tagJogador))
            return;

        VidaPersonagem vida = outro.GetComponent<VidaPersonagem>() ?? outro.GetComponentInParent<VidaPersonagem>();
        if (vida != null && vida.EstaMorto)
            return;

        jogadorNoAlcance = true;

        if (exigirObjetivoReliquias && objetivo != null && !objetivo.Concluido)
        {
            int faltantes = Mathf.Max(1, objetivo.Meta - objetivo.Coletadas);
            AoExibirMensagemPortal?.Invoke($"Colete mais {faltantes} relíquia(s) para ativar o portal!");
            return;
        }

        transicaoIniciada = true;
        AoOcultarMensagemPortal?.Invoke();
        TocarSomPortal();
        StartCoroutine(RotinaTransicao());
    }

    private void OnTriggerExit(Collider outro)
    {
        if (!jogadorNoAlcance || !outro.CompareTag(tagJogador))
            return;

        jogadorNoAlcance = false;
        AoOcultarMensagemPortal?.Invoke();
    }

    private void OnDisable()
    {
        if (objetivo != null && inscritoObjetivo)
        {
            objetivo.AoProgresso -= AoAtualizarObjetivo;
            inscritoObjetivo = false;
        }

        if (rotinaOcultarAviso != null)
        {
            StopCoroutine(rotinaOcultarAviso);
            rotinaOcultarAviso = null;
        }

        if (jogadorNoAlcance)
        {
            jogadorNoAlcance = false;
            AoOcultarMensagemPortal?.Invoke();
        }
    }

    private void TocarSomPortal()
    {
        if (somPortal == null)
            return;

        Vector3 posAudio = Camera.main != null ? Camera.main.transform.position : transform.position;
        AudioSource.PlayClipAtPoint(somPortal, posAudio, volumeSom);
    }

    private IEnumerator RotinaTransicao()
    {
        if (atrasoTransicao > 0f)
            yield return new WaitForSecondsRealtime(atrasoTransicao);

        if (ehFinalDoJogo)
        {
            Debug.Log("[PortalProximaFase] Portal final alcançado! Disparando conclusão do jogo.");
            if (AoConcluirJogo != null)
            {
                AoConcluirJogo.Invoke();
                yield break;
            }

            Debug.LogWarning("[PortalProximaFase] Nenhum TelaVitoriaUI inscrito em AoConcluirJogo; retornando ao Menu Principal.");
            AoExibirMensagemPortal?.Invoke("Fase final concluída! Retornando ao Menu Principal...");
            yield return new WaitForSecondsRealtime(1.5f);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 1f;

            if (Application.CanStreamedLevelBeLoaded("Cena_MenuPrincipal"))
                SceneManager.LoadScene("Cena_MenuPrincipal");
            yield break;
        }

        if (string.IsNullOrWhiteSpace(nomeCenaDestino) || !Application.CanStreamedLevelBeLoaded(nomeCenaDestino))
        {
            Debug.LogError($"[PortalProximaFase] Cena '{nomeCenaDestino}' não está registrada em File -> Build Settings / Build Profiles.", this);
            AoExibirMensagemPortal?.Invoke($"Cena '{nomeCenaDestino}' não registrada no Build Settings.");
            transicaoIniciada = false;
            yield break;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(nomeCenaDestino);
    }
}
