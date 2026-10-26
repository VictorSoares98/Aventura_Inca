using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class ControleJogador : MonoBehaviour
{
    [Header("Movimentação")]
    [SerializeField] private float velocidadeAndar = 3.0f;
    [SerializeField] private float velocidadeCorrer = 6.0f;
    [SerializeField] private float velocidadeRotacao = 15.0f;

    [Header("Pulo e Gravidade")]
    [SerializeField] private float alturaPulo = 1.5f;
    [SerializeField] private float gravidade = -9.81f;

    [Header("Câmera")]
    [SerializeField] private Transform transformCamera;

    private CharacterController controle;
    private Vector3 velocidade;
    private bool estaNoChao;

    private void Start()
    {
        controle = GetComponent<CharacterController>();

        if (transformCamera == null && Camera.main != null)
        {
            transformCamera = Camera.main.transform;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        ProcessarGravidade();
        ProcessarMovimento();
    }

    private void ProcessarGravidade()
    {
        estaNoChao = controle.isGrounded;

        if (estaNoChao && velocidade.y < 0)
        {
            velocidade.y = -2f;
        }

        // Pulo usando a tecla Espaço diretamente para evitar conflito com o Input Manager
        if (Input.GetKey(KeyCode.Space) && estaNoChao)
        {
            velocidade.y = Mathf.Sqrt(alturaPulo * -2f * gravidade);
        }

        velocidade.y += gravidade * Time.deltaTime;
        controle.Move(velocidade * Time.deltaTime);
    }

    private void ProcessarMovimento()
    {
        // Leitura direta das teclas W, A, S, D para contornar o Input System antigo
        float horizontal = 0f;
        float vertical = 0f;

        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) horizontal = 1f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) horizontal = -1f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) vertical = 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) vertical = -1f;

        Vector3 direcao = new Vector3(horizontal, 0f, vertical).normalized;

        if (direcao.magnitude >= 0.1f)
        {
            float anguloAlvo = Mathf.Atan2(direcao.x, direcao.z) * Mathf.Rad2Deg + transformCamera.eulerAngles.y;

            Quaternion rotacaoAlvo = Quaternion.Euler(0f, anguloAlvo, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotacaoAlvo, velocidadeRotacao * Time.deltaTime);

            bool estaCorrendo = Input.GetKey(KeyCode.LeftShift);
            float velocidadeAtual = estaCorrendo ? velocidadeCorrer : velocidadeAndar;

            Vector3 direcaoMovimento = Quaternion.Euler(0f, anguloAlvo, 0f) * Vector3.forward;
            controle.Move(direcaoMovimento.normalized * (velocidadeAtual * Time.deltaTime));
        }
    }
}