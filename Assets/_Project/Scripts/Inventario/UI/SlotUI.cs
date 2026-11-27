using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Referências")]
    [SerializeField] private Image icone;
    [SerializeField] private TextMeshProUGUI textoQuantidade;
    [SerializeField] private Button botao;

    private int indice;
    private SlotInventario slotAtual;
    private Action<int> aoClicar;
    private Action<SlotInventario> aoPassarMouse;

    private void Awake()
    {
        if (botao != null)
            botao.onClick.AddListener(Clicar);
    }

    private void OnDestroy()
    {
        if (botao != null)
            botao.onClick.RemoveListener(Clicar);
    }

    public void Inicializar(int indiceSlot, Action<int> callbackClique, Action<SlotInventario> callbackHover)
    {
        indice = indiceSlot;
        aoClicar = callbackClique;
        aoPassarMouse = callbackHover;
    }

    /// <summary>Renders the slot; pass null to show it empty.</summary>
    public void Exibir(SlotInventario slot)
    {
        slotAtual = slot != null && !slot.EstaVazio ? slot : null;
        bool temItem = slotAtual != null;

        if (icone != null)
        {
            icone.enabled = temItem && slotAtual.Item.Icone != null;
            icone.sprite = temItem ? slotAtual.Item.Icone : null;
        }

        if (textoQuantidade != null)
            textoQuantidade.text = temItem && slotAtual.Quantidade > 1 ? slotAtual.Quantidade.ToString() : string.Empty;

        if (botao != null)
            botao.interactable = temItem;
    }

    private void Clicar()
    {
        if (slotAtual != null)
            aoClicar?.Invoke(indice);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        aoPassarMouse?.Invoke(slotAtual);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        aoPassarMouse?.Invoke(null);
    }
}
