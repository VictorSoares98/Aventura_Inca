using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SlotInventario
{
    [SerializeField] private ItemDados item;
    [SerializeField] private int quantidade;

    public ItemDados Item => item;
    public int Quantidade => quantidade;
    public bool EstaVazio => item == null || quantidade <= 0;
    public int EspacoLivre => item == null ? 0 : item.LimitePilhaEfetivo - quantidade;

    public SlotInventario(ItemDados item, int quantidade)
    {
        this.item = item;
        this.quantidade = quantidade;
    }

    /// <summary>Adds up to the stack limit and returns the leftover amount.</summary>
    public int Adicionar(int valor)
    {
        int aceito = Mathf.Min(valor, EspacoLivre);
        quantidade += aceito;
        return valor - aceito;
    }

    /// <summary>Removes up to the current amount and returns how many were removed.</summary>
    public int Remover(int valor)
    {
        int removido = Mathf.Min(valor, quantidade);
        quantidade -= removido;
        return removido;
    }
}

public class Inventario : MonoBehaviour
{
    [Header("Configuração")]
    [SerializeField, Min(1)] private int capacidade = 20;

    [Header("Estado (somente leitura em jogo)")]
    [SerializeField] private List<SlotInventario> slots = new List<SlotInventario>();

    /// <summary>Raised after any change in the slots.</summary>
    public event Action AoMudar;

    /// <summary>Raised with the item and the amount actually added.</summary>
    public event Action<ItemDados, int> AoItemAdicionado;

    public IReadOnlyList<SlotInventario> Slots => slots;
    public int Capacidade => capacidade;

    /// <summary>Adds items filling existing stacks first. Returns the amount actually added.</summary>
    public int Adicionar(ItemDados item, int quantidade)
    {
        if (item == null || quantidade <= 0)
        {
            Debug.LogWarning($"[Inventario] Invalid add request (item: {item}, quantity: {quantidade}).");
            return 0;
        }

        int restante = quantidade;

        // 1) Fill existing stacks of the same item
        if (item.Empilhavel)
        {
            foreach (SlotInventario slot in slots)
            {
                if (slot.Item != item || slot.EspacoLivre <= 0)
                    continue;

                restante = slot.Adicionar(restante);
                if (restante == 0)
                    break;
            }
        }

        // 2) Open new slots while there is capacity
        while (restante > 0 && slots.Count < capacidade)
        {
            int lote = Mathf.Min(restante, item.LimitePilhaEfetivo);
            slots.Add(new SlotInventario(item, lote));
            restante -= lote;
        }

        int adicionado = quantidade - restante;

        if (restante > 0)
            Debug.LogWarning($"[Inventario] Inventory full: {restante}x '{item.NomeExibicao}' could not be added.");

        if (adicionado > 0)
        {
            AoItemAdicionado?.Invoke(item, adicionado);
            AoMudar?.Invoke();
        }

        return adicionado;
    }

    /// <summary>Removes the given amount only if the inventory holds enough.</summary>
    public bool Remover(ItemDados item, int quantidade)
    {
        if (item == null || quantidade <= 0)
            return false;

        if (ContarItem(item) < quantidade)
            return false;

        int restante = quantidade;

        // Consume from the last stacks first to keep the grid stable
        for (int i = slots.Count - 1; i >= 0 && restante > 0; i--)
        {
            if (slots[i].Item != item)
                continue;

            restante -= slots[i].Remover(restante);

            if (slots[i].EstaVazio)
                slots.RemoveAt(i);
        }

        AoMudar?.Invoke();
        return true;
    }

    /// <summary>Uses the item at the given slot index, consuming one unit on success.</summary>
    public bool Usar(int indice)
    {
        if (indice < 0 || indice >= slots.Count)
        {
            Debug.LogWarning($"[Inventario] Invalid slot index: {indice}.");
            return false;
        }

        SlotInventario slot = slots[indice];

        if (!slot.Item.TentarUsar(gameObject))
            return false;

        slot.Remover(1);

        if (slot.EstaVazio)
            slots.RemoveAt(indice);

        AoMudar?.Invoke();
        return true;
    }

    public int ContarItem(ItemDados item)
    {
        int total = 0;

        foreach (SlotInventario slot in slots)
        {
            if (slot.Item == item)
                total += slot.Quantidade;
        }

        return total;
    }

    public int ContarPorTipo(TipoItem tipo)
    {
        int total = 0;

        foreach (SlotInventario slot in slots)
        {
            if (slot.Item != null && slot.Item.Tipo == tipo)
                total += slot.Quantidade;
        }

        return total;
    }
}
