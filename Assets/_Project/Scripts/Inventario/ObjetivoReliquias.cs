using System;
using UnityEngine;
using UnityEngine.Events;

public class ObjetivoReliquias : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private Inventario inventario;

    [Header("Objetivo")]
    [SerializeField, Min(1)] private int meta = 5;

    [Header("Ao concluir (ligar pelo Inspector)")]
    [SerializeField] private UnityEvent aoConcluir;

    /// <summary>Raised with (collected, goal) whenever progress is evaluated.</summary>
    public event Action<int, int> AoProgresso;

    public int Coletadas { get; private set; }
    public int Meta => meta;
    public bool Concluido { get; private set; }

    private void OnEnable()
    {
        if (inventario == null)
        {
            Debug.LogError("[ObjetivoReliquias] Inventario reference is missing.", this);
            enabled = false;
            return;
        }

        inventario.AoMudar += Avaliar;
    }

    private void OnDisable()
    {
        if (inventario != null)
            inventario.AoMudar -= Avaliar;
    }

    private void Start()
    {
        Avaliar();
    }

    private void Avaliar()
    {
        Coletadas = inventario.ContarPorTipo(TipoItem.Reliquia);
        AoProgresso?.Invoke(Coletadas, meta);

        if (Concluido || Coletadas < meta)
            return;

        Concluido = true;
        Debug.Log($"[ObjetivoReliquias] Goal reached: {Coletadas}/{meta} relics.");
        aoConcluir?.Invoke();
    }
}
