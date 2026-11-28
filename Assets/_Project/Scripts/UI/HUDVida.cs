using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Updates the health bar UI through VidaPersonagem static events without cross-scene references,
/// and prevents duplicate EventSystem instances when used inside Prefab_HUD_Sistema.
/// </summary>
public class HUDVida : MonoBehaviour
{
    [Header("Elementos da Barra de Vida")]
    [SerializeField] private Image barraVida;
    [SerializeField] private TextMeshProUGUI textoVida;

    [Header("EventSystem (opcional no Prefab)")]
    [SerializeField] private EventSystem eventSystemLocal;

    private void Awake()
    {
        if (eventSystemLocal == null)
            return;

        EventSystem[] todos = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
        foreach (EventSystem es in todos)
        {
            if (es != null && es != eventSystemLocal && es.isActiveAndEnabled)
            {
                eventSystemLocal.gameObject.SetActive(false);
                break;
            }
        }
    }

    private void OnEnable()
    {
        VidaPersonagem.AoVidaMudar += AtualizarVida;
    }

    private void OnDisable()
    {
        VidaPersonagem.AoVidaMudar -= AtualizarVida;
    }

    private void Start()
    {
        VidaPersonagem vida = FindFirstObjectByType<VidaPersonagem>();
        if (vida != null)
        {
            AtualizarVida(vida.VidaAtual, vida.VidaMaxima);
        }
    }

    private void AtualizarVida(float atual, float maxima)
    {
        if (barraVida != null)
        {
            barraVida.fillAmount = maxima > 0f ? Mathf.Clamp01(atual / maxima) : 0f;
        }

        if (textoVida != null)
        {
            textoVida.text = $"{Mathf.CeilToInt(atual)} / {Mathf.CeilToInt(maxima)} HP";
        }
    }
}
