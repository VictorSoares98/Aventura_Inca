using System.Collections;
using TMPro;
using UnityEngine;

public class HUDInventario : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private Inventario inventario;
    [SerializeField] private ObjetivoReliquias objetivo;

    [Header("Contadores")]
    [SerializeField] private TextMeshProUGUI textoMoedas;
    [SerializeField] private TextMeshProUGUI textoReliquias;

    [Header("Aviso de interação")]
    [SerializeField] private GameObject painelAviso;
    [SerializeField] private TextMeshProUGUI textoAviso;

    [Header("Notificação de coleta")]
    [SerializeField] private TextMeshProUGUI textoNotificacao;
    [SerializeField, Min(0.1f)] private float duracaoNotificacao = 2f;

    private ItemColetavel coletavelEmFoco;
    private Coroutine rotinaNotificacao;

    private void OnEnable()
    {
        if (inventario == null)
        {
            Debug.LogError("[HUDInventario] Inventario reference is missing.", this);
            enabled = false;
            return;
        }

        inventario.AoMudar += AtualizarContadores;
        inventario.AoItemAdicionado += MostrarNotificacao;

        if (objetivo != null)
            objetivo.AoProgresso += AtualizarReliquias;

        ItemColetavel.AoEntrarAlcance += MostrarAviso;
        ItemColetavel.AoSairAlcance += EsconderAviso;
    }

    private void OnDisable()
    {
        if (inventario != null)
        {
            inventario.AoMudar -= AtualizarContadores;
            inventario.AoItemAdicionado -= MostrarNotificacao;
        }

        if (objetivo != null)
            objetivo.AoProgresso -= AtualizarReliquias;

        ItemColetavel.AoEntrarAlcance -= MostrarAviso;
        ItemColetavel.AoSairAlcance -= EsconderAviso;
    }

    private void Start()
    {
        if (painelAviso != null) painelAviso.SetActive(false);
        if (textoNotificacao != null) textoNotificacao.text = string.Empty;

        AtualizarContadores();
    }

    private void AtualizarContadores()
    {
        if (textoMoedas != null)
            textoMoedas.text = $"Moedas: {inventario.ContarPorTipo(TipoItem.Moeda)}";

        // Without an objective, show the raw relic count
        if (objetivo == null && textoReliquias != null)
            textoReliquias.text = $"Relíquias: {inventario.ContarPorTipo(TipoItem.Reliquia)}";
    }

    private void AtualizarReliquias(int coletadas, int meta)
    {
        if (textoReliquias != null)
            textoReliquias.text = $"Relíquias: {coletadas}/{meta}";
    }

    private void MostrarAviso(ItemColetavel coletavel)
    {
        coletavelEmFoco = coletavel;

        if (textoAviso != null)
            textoAviso.text = $"Pressione [{coletavel.TeclaColeta}] para coletar {coletavel.Item.NomeExibicao}";

        if (painelAviso != null)
            painelAviso.SetActive(true);
    }

    private void EsconderAviso(ItemColetavel coletavel)
    {
        // Ignore exits from collectibles that are not the one being displayed
        if (coletavel != coletavelEmFoco)
            return;

        coletavelEmFoco = null;

        if (painelAviso != null)
            painelAviso.SetActive(false);
    }

    private void MostrarNotificacao(ItemDados item, int quantidade)
    {
        if (textoNotificacao == null)
            return;

        if (rotinaNotificacao != null)
            StopCoroutine(rotinaNotificacao);

        rotinaNotificacao = StartCoroutine(RotinaNotificacao($"+{quantidade} {item.NomeExibicao}"));
    }

    private IEnumerator RotinaNotificacao(string mensagem)
    {
        textoNotificacao.text = mensagem;
        yield return new WaitForSecondsRealtime(duracaoNotificacao);
        textoNotificacao.text = string.Empty;
        rotinaNotificacao = null;
    }
}
