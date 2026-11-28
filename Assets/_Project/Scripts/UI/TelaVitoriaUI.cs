using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Decoupled victory screen overlay that listens for final portal completion,
/// locks gameplay input, frees the cursor, and supports replay or returning to menu.
/// </summary>
public class TelaVitoriaUI : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private GameObject painelVitoria;

    [Header("Destinos")]
    [SerializeField] private string nomeCenaMenu = "Cena_MenuPrincipal";
    [SerializeField] private string nomeCenaPrimeiraFase = "Cena_principal";

    private void Awake()
    {
        if (painelVitoria != null)
            painelVitoria.SetActive(false);
    }

    private void OnEnable()
    {
        PortalProximaFase.AoConcluirJogo += ExibirVitoria;
    }

    private void OnDisable()
    {
        PortalProximaFase.AoConcluirJogo -= ExibirVitoria;
    }

    public void ExibirVitoria()
    {
        InventarioUI inventarioUI = FindFirstObjectByType<InventarioUI>();
        if (inventarioUI != null && inventarioUI.EstaAberto)
            inventarioUI.Fechar();

        ControleJogador controle = FindFirstObjectByType<ControleJogador>();
        if (controle != null)
            controle.enabled = false;

        CinemachineInputAxisController camInput = FindFirstObjectByType<CinemachineInputAxisController>();
        if (camInput != null)
            camInput.enabled = false;

        if (painelVitoria != null)
            painelVitoria.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void JogarNovamente()
    {
        CarregarCenaSegura(nomeCenaPrimeiraFase);
    }

    public void VoltarAoMenu()
    {
        CarregarCenaSegura(nomeCenaMenu);
    }

    private void CarregarCenaSegura(string nomeCena)
    {
        if (string.IsNullOrWhiteSpace(nomeCena) || !Application.CanStreamedLevelBeLoaded(nomeCena))
        {
            Debug.LogError($"[TelaVitoriaUI] Cena '{nomeCena}' não está registrada em File -> Build Settings / Build Profiles.", this);
            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(nomeCena);
    }
}
