using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;
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
        GarantirCanvasRaizVisivel();

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
        GarantirCanvasRaizVisivel();

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

        GarantirEventSystemAtivo();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("[TelaVitoriaUI] Tela de Vitória exibida!");
    }

    public void JogarNovamente()
    {
        CarregarCenaSegura(nomeCenaPrimeiraFase);
    }

    public void VoltarAoMenu()
    {
        CarregarCenaSegura(nomeCenaMenu);
    }

    private void GarantirCanvasRaizVisivel()
    {
        // Avoid nesting inside another Canvas (which collapses localScale/sizeDelta to zero)
        if (transform.parent != null && transform.parent.GetComponentInParent<Canvas>() != null)
            transform.SetParent(null, false);

        RectTransform rt = GetComponent<RectTransform>();
        if (rt != null)
            rt.localScale = Vector3.one;

        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.enabled = true;
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 100;
        }
    }

    private void GarantirEventSystemAtivo()
    {
        EventSystem es = FindFirstObjectByType<EventSystem>();
        if (es != null && es.isActiveAndEnabled)
            return;

        GameObject esObj = new GameObject("EventSystem_Vitoria", typeof(EventSystem), typeof(StandaloneInputModule));
        esObj.transform.SetParent(null, false);
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
