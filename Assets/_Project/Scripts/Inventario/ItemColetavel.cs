using System;
using UnityEngine;

public enum ModoColeta
{
    PorTipoDoItem,
    Automatica,
    Tecla
}

[RequireComponent(typeof(Collider))]
public class ItemColetavel : MonoBehaviour
{
    [Header("Item")]
    [SerializeField] private ItemDados item;
    [SerializeField, Min(1)] private int quantidade = 1;

    [Header("Coleta")]
    [Tooltip("PorTipoDoItem: coins are automatic, everything else requires the key.")]
    [SerializeField] private ModoColeta modoColeta = ModoColeta.PorTipoDoItem;
    [SerializeField] private KeyCode teclaColeta = KeyCode.E;
    [SerializeField] private string tagJogador = "Player";

    [Header("Animação")]
    [SerializeField] private float velocidadeGiro = 90f;
    [SerializeField] private float amplitudeFlutuacao = 0.15f;
    [SerializeField] private float frequenciaFlutuacao = 1.5f;

    [Header("Ajuste de Altura")]
    [Tooltip("Ajusta automaticamente a altura para flutuar acima do solo ou terreno.")]
    [SerializeField] private bool autoAjustarAoSolo = true;
    [SerializeField, Min(0.2f)] private float alturaAcimaDoSolo = 0.85f;

    [Header("Feedback (opcional)")]
    [SerializeField] private AudioClip somColeta;
    [SerializeField] private GameObject efeitoColeta;

    /// <summary>Raised when the player enters the range of a key-based collectible.</summary>
    public static event Action<ItemColetavel> AoEntrarAlcance;

    /// <summary>Raised when the player leaves the range or the collectible is gone.</summary>
    public static event Action<ItemColetavel> AoSairAlcance;

    public ItemDados Item => item;
    public KeyCode TeclaColeta => teclaColeta;

    private Inventario inventarioProximo;
    private Vector3 posicaoInicial;
    private bool coletado;

    private bool EhAutomatica =>
        modoColeta == ModoColeta.Automatica ||
        (modoColeta == ModoColeta.PorTipoDoItem && item != null && item.Tipo == TipoItem.Moeda);

    private void Reset()
    {
        // Collectibles must be triggers so the CharacterController walks through them
        GetComponent<Collider>().isTrigger = true;
    }

    private void Awake()
    {
        posicaoInicial = transform.position;

        if (item == null)
            Debug.LogError($"[ItemColetavel] '{name}' has no ItemDados assigned.", this);

        if (!GetComponent<Collider>().isTrigger)
            Debug.LogWarning($"[ItemColetavel] Collider on '{name}' is not a trigger; collection will not fire.", this);
    }

    private void Start()
    {
        if (autoAjustarAoSolo)
        {
            AjustarAlturaAoTerreno();
        }
    }

    public void AjustarAlturaAoTerreno()
    {
        float alturaChao = transform.position.y;
        bool chaoDetectado = false;

        // Cast ray downwards from 15m above to detect ground or terrain
        Vector3 origem = new Vector3(transform.position.x, transform.position.y + 15f, transform.position.z);
        int mascara = ~LayerMask.GetMask("Ignore Raycast");

        if (Physics.Raycast(origem, Vector3.down, out RaycastHit hit, 40f, mascara, QueryTriggerInteraction.Ignore))
        {
            alturaChao = hit.point.y;
            chaoDetectado = true;
        }
        else if (Terrain.activeTerrain != null)
        {
            alturaChao = Terrain.activeTerrain.SampleHeight(transform.position) + Terrain.activeTerrain.transform.position.y;
            chaoDetectado = true;
        }

        if (chaoDetectado)
        {
            posicaoInicial = new Vector3(transform.position.x, alturaChao + alturaAcimaDoSolo, transform.position.z);
            transform.position = posicaoInicial;
        }
    }

    private void Update()
    {
        Animar();

        if (inventarioProximo != null && Input.GetKeyDown(teclaColeta))
            Coletar(inventarioProximo);
    }

    private void Animar()
    {
        transform.Rotate(Vector3.up, velocidadeGiro * Time.deltaTime, Space.World);

        float deslocamentoY = Mathf.Sin(Time.time * frequenciaFlutuacao * Mathf.PI * 2f) * amplitudeFlutuacao;
        transform.position = posicaoInicial + Vector3.up * deslocamentoY;
    }

    private void OnTriggerEnter(Collider outro)
    {
        if (coletado || item == null || !outro.CompareTag(tagJogador))
            return;

        Inventario inventario = outro.GetComponent<Inventario>() ?? outro.GetComponentInParent<Inventario>();
        if (inventario == null)
        {
            Debug.LogWarning($"[ItemColetavel] '{outro.name}' has tag '{tagJogador}' but no Inventario component.", outro);
            return;
        }

        if (EhAutomatica)
        {
            Coletar(inventario);
            return;
        }

        inventarioProximo = inventario;
        AoEntrarAlcance?.Invoke(this);
    }

    private void OnTriggerExit(Collider outro)
    {
        if (inventarioProximo == null || !outro.CompareTag(tagJogador))
            return;

        LiberarAlcance();
    }

    private void OnDisable()
    {
        LiberarAlcance();
    }

    private void Coletar(Inventario inventario)
    {
        if (coletado)
            return;

        int adicionado = inventario.Adicionar(item, quantidade);

        if (adicionado <= 0)
            return; // Inventory full: Inventario already logs the reason

        quantidade -= adicionado;

        // Partial pickup: keep the remainder in the world
        if (quantidade > 0)
            return;

        coletado = true;

        if (somColeta != null)
            AudioSource.PlayClipAtPoint(somColeta, transform.position);

        if (efeitoColeta != null)
            Instantiate(efeitoColeta, transform.position, Quaternion.identity);

        LiberarAlcance();
        Destroy(gameObject);
    }

    private void LiberarAlcance()
    {
        if (inventarioProximo == null)
            return;

        inventarioProximo = null;
        AoSairAlcance?.Invoke(this);
    }
}
