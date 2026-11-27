using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class InventarioUI : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private Inventario inventario;
    [SerializeField] private GameObject painel;
    [SerializeField] private Transform contenedorSlots;
    [SerializeField] private SlotUI prefabSlot;

    [Header("Detalhes do item (opcional)")]
    [SerializeField] private TextMeshProUGUI textoNome;
    [SerializeField] private TextMeshProUGUI textoDescricao;

    [Header("Controle")]
    [SerializeField] private KeyCode teclaAbrir = KeyCode.I;
    [Tooltip("Components disabled while the panel is open (e.g. ControleJogador, CinemachineInputAxisController).")]
    [SerializeField] private Behaviour[] componentesBloqueados;
    [SerializeField] private bool pausarJogo = false;

    private readonly List<SlotUI> slotsUI = new List<SlotUI>();
    private readonly Dictionary<Behaviour, bool> estadoAnterior = new Dictionary<Behaviour, bool>();
    private float escalaTempoAnterior = 1f;

    public bool EstaAberto => painel != null && painel.activeSelf;

    private void Awake()
    {
        if (inventario == null || painel == null || contenedorSlots == null || prefabSlot == null)
        {
            Debug.LogError("[InventarioUI] Missing references (inventario, painel, contenedorSlots or prefabSlot).", this);
            enabled = false;
            return;
        }

        CriarSlots();
        painel.SetActive(false);
        ExibirDetalhes(null);
    }

    private void OnEnable()
    {
        if (inventario != null)
            inventario.AoMudar += Atualizar;
    }

    private void OnDisable()
    {
        if (inventario != null)
            inventario.AoMudar -= Atualizar;

        if (EstaAberto)
            Fechar();
    }

    private void Update()
    {
        if (Input.GetKeyDown(teclaAbrir))
        {
            if (EstaAberto) Fechar();
            else Abrir();
        }
        else if (EstaAberto && Input.GetKeyDown(KeyCode.Escape))
        {
            Fechar();
        }
    }

    private void CriarSlots()
    {
        for (int i = 0; i < inventario.Capacidade; i++)
        {
            SlotUI slot = Instantiate(prefabSlot, contenedorSlots);
            slot.Inicializar(i, UsarItem, ExibirDetalhes);
            slotsUI.Add(slot);
        }

        Atualizar();
    }

    private void Atualizar()
    {
        IReadOnlyList<SlotInventario> slots = inventario.Slots;

        for (int i = 0; i < slotsUI.Count; i++)
            slotsUI[i].Exibir(i < slots.Count ? slots[i] : null);
    }

    private void UsarItem(int indice)
    {
        if (!inventario.Usar(indice))
            return;

        ExibirDetalhes(null);
    }

    private void ExibirDetalhes(SlotInventario slot)
    {
        bool temItem = slot != null && !slot.EstaVazio;

        if (textoNome != null)
            textoNome.text = temItem ? slot.Item.NomeExibicao : string.Empty;

        if (textoDescricao != null)
            textoDescricao.text = temItem ? slot.Item.Descricao : string.Empty;
    }

    public void Abrir()
    {
        painel.SetActive(true);
        Atualizar();

        // Remember previous state so we never re-enable something disabled elsewhere (car, death)
        estadoAnterior.Clear();
        foreach (Behaviour componente in componentesBloqueados)
        {
            if (componente == null) continue;
            estadoAnterior[componente] = componente.enabled;
            componente.enabled = false;
        }

        if (pausarJogo)
        {
            escalaTempoAnterior = Time.timeScale;
            Time.timeScale = 0f;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Fechar()
    {
        painel.SetActive(false);
        ExibirDetalhes(null);

        foreach (KeyValuePair<Behaviour, bool> par in estadoAnterior)
        {
            if (par.Key != null)
                par.Key.enabled = par.Value;
        }
        estadoAnterior.Clear();

        if (pausarJogo)
            Time.timeScale = escalaTempoAnterior;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
