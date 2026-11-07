
using UnityEngine;
using UnityEngine.UI;


public class VidaPersonagem : MonoBehaviour
{
    [Header("Configuração da Vida")]
    [SerializeField] private float vidaMaxima = 100f;
    [SerializeField] private float vidaAtual;

    [Header("HUD")]
    [SerializeField] private Image barraVida;
   

    private bool estaMorto;

    private void Start()
    {
        vidaAtual = vidaMaxima;

        AtualizarHUD();
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
            barraVida.fillAmount = vidaAtual / vidaMaxima;
        }
    }

    private void Morrer()
    {
        estaMorto = true;

        Debug.Log("O personagem morreu!");

        ControleJogador controle =
            GetComponent<ControleJogador>();

        if (controle != null)
        {
            controle.enabled = false;
        }
    }
}
