
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VidaPersonagem : MonoBehaviour
{
    [Header("Configuração da Vida")]
    [SerializeField] private float vidaMaxima = 100f;
    [SerializeField] private float vidaAtual;

    [Header("HUD")]
    [SerializeField] private Image barraVida;
    [SerializeField] private TextMeshProUGUI textoVida;

    private bool estaMorto;

    public bool EstaMorto => estaMorto;
    public bool EstaComVidaCheia => vidaAtual >= vidaMaxima;

    public static event System.Action AoJogadorMorrer;
    public static event System.Action AoJogadorRenascer;

    private void Start()
    {
        vidaAtual = vidaMaxima;

        AtualizarHUD();
    }

    private void Update()
    {
        if (estaMorto && Input.GetKeyDown(KeyCode.R))
        {
            Renascer();
        }
    }

    public void ReceberDano(float quantidade)
    {
        if (estaMorto || quantidade <= 0)
            return;

        vidaAtual -= quantidade;

        vidaAtual = Mathf.Clamp(
            vidaAtual, 0, vidaMaxima
        );

        AtualizarHUD();

        Debug.Log("Vida atual: " + vidaAtual);

        if (vidaAtual <= 0)
        {
            Morrer();
        }
    }

    public void RecuperarVida(float quantidade)
    {
        if (estaMorto || quantidade <= 0)
            return;

        vidaAtual += quantidade;

        vidaAtual = Mathf.Clamp(
            vidaAtual, 0, vidaMaxima
        );

        AtualizarHUD();
    }

    private void AtualizarHUD()
    {
        if (barraVida != null)
        {
            barraVida.fillAmount = vidaMaxima > 0f ? (vidaAtual / vidaMaxima) : 0f;
        }

        if (textoVida != null)
        {
            textoVida.text = $"{Mathf.CeilToInt(vidaAtual)} / {Mathf.CeilToInt(vidaMaxima)} HP";
        }
    }

    private void Morrer()
    {
        estaMorto = true;

        Debug.LogWarning("[VidaPersonagem] O personagem morreu! Pressione [R] para renascer com vida cheia.");

        ControleJogador controle = GetComponent<ControleJogador>();
        if (controle != null)
        {
            controle.enabled = false;
        }

        AoJogadorMorrer?.Invoke();
    }

    public void Renascer()
    {
        estaMorto = false;
        vidaAtual = vidaMaxima;
        AtualizarHUD();

        ControleJogador controle = GetComponent<ControleJogador>();
        if (controle != null)
        {
            controle.enabled = true;
        }

        CharacterController character = GetComponent<CharacterController>();
        if (character != null)
        {
            character.enabled = true;
        }

        AoJogadorRenascer?.Invoke();
        Debug.Log("[VidaPersonagem] Jogador renasceu com vida cheia e movimento reativado!");
    }
}
