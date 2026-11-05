
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ControleCarro : MonoBehaviour
{
    [Header("Configuração")]
    [SerializeField] private float aceleracao = 12f;
    [SerializeField] private float velocidadeMaxima = 20f;
    [SerializeField] private float velocidadeRe = 7f;
    [SerializeField] private float velocidadeRotacao = 50f;

    [Header("Aderência")]
    [SerializeField] private float aderenciaLateral = 5f;
    [SerializeField] private float velocidadeMinimaParaVirar = 0.5f;

    private Rigidbody rb;
    private bool podeDirigir;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void FixedUpdate()
    {
        if (!podeDirigir)
            return;

        float acelerar = Input.GetAxis("Vertical");
        float virar = Input.GetAxis("Horizontal");

        // Calcula a velocidade horizontal do carro.
        Vector3 velocidadeHorizontal = new Vector3(
            rb.linearVelocity.x,
            0f,
            rb.linearVelocity.z
        );

        // Reduz o deslizamento lateral.
        Vector3 velocidadeLateral =
            transform.right * Vector3.Dot(
                velocidadeHorizontal,
                transform.right
            );

        rb.AddForce(
            -velocidadeLateral * aderenciaLateral,
            ForceMode.Acceleration
        );

        // Calcula a velocidade na direção para a qual o carro aponta.
        float velocidadeFrente = Vector3.Dot(
            velocidadeHorizontal,
            transform.forward
        );

        // Aceleração para frente.
        if (acelerar > 0f && velocidadeFrente < velocidadeMaxima)
        {
            rb.AddForce(
                transform.forward * acelerar * aceleracao,
                ForceMode.Acceleration
            );
        }
        // Marcha à ré.
        else if (acelerar < 0f && velocidadeFrente > -velocidadeRe)
        {
            rb.AddForce(
                transform.forward * acelerar * aceleracao,
                ForceMode.Acceleration
            );
        }

        // Direção proporcional à velocidade e ao comando do jogador.
        if (velocidadeHorizontal.magnitude > velocidadeMinimaParaVirar)
        {
            float sentido = velocidadeFrente >= 0f ? 1f : -1f;

            float rotacao = virar
                * velocidadeRotacao
                * sentido
                * Time.fixedDeltaTime;

            rb.MoveRotation(
                rb.rotation * Quaternion.Euler(0f, rotacao, 0f)
            );
        }
    }

    public void AtivarDirecao(bool ativar)
    {
        podeDirigir = ativar;
    }
}
