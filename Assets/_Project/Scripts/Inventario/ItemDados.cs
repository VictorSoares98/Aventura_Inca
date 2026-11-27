using UnityEngine;

public enum TipoItem
{
    Moeda,
    Reliquia,
    Consumivel
}

[CreateAssetMenu(fileName = "NovoItem", menuName = "Aventura Inca/Item")]
public class ItemDados : ScriptableObject
{
    [Header("Identificação")]
    [SerializeField] private string id;
    [SerializeField] private string nomeExibicao;
    [SerializeField, TextArea(2, 4)] private string descricao;
    [SerializeField] private Sprite icone;

    [Header("Comportamento")]
    [SerializeField] private TipoItem tipo = TipoItem.Moeda;
    [SerializeField] private bool empilhavel = true;
    [SerializeField, Min(1)] private int limitePilha = 99;

    [Header("Consumível")]
    [Tooltip("Health restored when used (only for Consumivel type).")]
    [SerializeField, Min(0f)] private float valorCura = 25f;

    public string Id => id;
    public string NomeExibicao => string.IsNullOrWhiteSpace(nomeExibicao) ? name : nomeExibicao;
    public string Descricao => descricao;
    public Sprite Icone => icone;
    public TipoItem Tipo => tipo;
    public bool Empilhavel => empilhavel;

    // Non-stackable items always occupy one slot per unit
    public int LimitePilhaEfetivo => empilhavel ? limitePilha : 1;

    /// <summary>
    /// Applies the item effect on the user. Returns true when the item was consumed.
    /// </summary>
    public bool TentarUsar(GameObject usuario)
    {
        if (tipo != TipoItem.Consumivel)
            return false;

        if (usuario == null || !usuario.TryGetComponent(out VidaPersonagem vida))
        {
            Debug.LogWarning($"[ItemDados] '{NomeExibicao}' requires a VidaPersonagem on the user to be consumed.");
            return false;
        }

        // Avoid wasting the potion when health is already full or the character is dead
        if (vida.EstaMorto || vida.EstaComVidaCheia)
        {
            Debug.Log($"[ItemDados] '{NomeExibicao}' not used: health is full or character is dead.");
            return false;
        }

        vida.RecuperarVida(valorCura);
        return true;
    }

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(id))
            id = name;
    }
}
