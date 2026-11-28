using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Controls the main menu scene navigation (Play, Level Select, Quit)
/// while guaranteeing unlocked cursor state and normal time scale.
/// </summary>
public class MenuPrincipalController : MonoBehaviour
{
    [Header("Nomes das Cenas (Build Settings)")]
    [SerializeField] private string cenaFase1 = "Cena_principal";
    [SerializeField] private string cenaFase2 = "Cena_Fase2_Ruinas";
    [SerializeField] private string cenaFase3 = "Cena_Fase3_Nave";

    [Header("Painéis de UI (opcionais)")]
    [SerializeField] private GameObject painelPrincipal;
    [SerializeField] private GameObject painelSelecaoFases;
    [SerializeField] private TextMeshProUGUI textoStatus;

    private void Awake()
    {
        PrepararEstadoMenu();
    }

    private void Start()
    {
        PrepararEstadoMenu();
    }

    private void PrepararEstadoMenu()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (painelPrincipal != null)
            painelPrincipal.SetActive(true);

        if (painelSelecaoFases != null)
            painelSelecaoFases.SetActive(false);

        if (textoStatus != null)
            textoStatus.text = string.Empty;
    }

    public void Jogar()
    {
        CarregarCenaSegura(cenaFase1);
    }

    public void CarregarFase1()
    {
        CarregarCenaSegura(cenaFase1);
    }

    public void CarregarFase2()
    {
        CarregarCenaSegura(cenaFase2);
    }

    public void CarregarFase3()
    {
        CarregarCenaSegura(cenaFase3);
    }

    public void AbrirSelecaoFases()
    {
        if (painelPrincipal != null)
            painelPrincipal.SetActive(false);

        if (painelSelecaoFases != null)
            painelSelecaoFases.SetActive(true);
    }

    public void FecharSelecaoFases()
    {
        if (painelSelecaoFases != null)
            painelSelecaoFases.SetActive(false);

        if (painelPrincipal != null)
            painelPrincipal.SetActive(true);
    }

    public void SairDoJogo()
    {
        Debug.Log("[MenuPrincipalController] Encerrando execução do jogo.");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void CarregarCenaSegura(string nomeCena)
    {
        if (string.IsNullOrWhiteSpace(nomeCena) || !Application.CanStreamedLevelBeLoaded(nomeCena))
        {
            string aviso = $"Cena '{nomeCena}' não está registrada em File -> Build Settings / Build Profiles.";
            Debug.LogError($"[MenuPrincipalController] {aviso}", this);

            if (textoStatus != null)
                textoStatus.text = aviso;

            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(nomeCena);
    }
}
