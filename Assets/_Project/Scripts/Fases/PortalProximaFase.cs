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

    private void Start()
    {
        objetivo = FindFirstObjectByType<ObjetivoReliquias>();
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

        if (somPortal != null)
        {
            Vector3 posAudio = Camera.main != null ? Camera.main.transform.position : transform.position;
            AudioSource.PlayClipAtPoint(somPortal, posAudio, volumeSom);
        }

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
        if (!jogadorNoAlcance)
            return;

        jogadorNoAlcance = false;
        AoOcultarMensagemPortal?.Invoke();
    }

    private IEnumerator RotinaTransicao()
    {
        if (atrasoTransicao > 0f)
            yield return new WaitForSecondsRealtime(atrasoTransicao);

        if (ehFinalDoJogo)
        {
            Debug.Log("[PortalProximaFase] Portal final alcançado! Disparando conclusão do jogo.");
            AoConcluirJogo?.Invoke();
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
