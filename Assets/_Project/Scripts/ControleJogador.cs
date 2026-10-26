
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class ControleJogador : MonoBehaviour
{
    private Animator animator;
    private CharacterController character;

    private readonly int inputXHash =
        Animator.StringToHash("inputX");

    private readonly int inputYHash =
        Animator.StringToHash("inputY");

    private readonly int noChaoHash =
        Animator.StringToHash("NoChao");

    private readonly int correndoHash =
        Animator.StringToHash("Correndo");

    [Header("Movimento")]
    [SerializeField] private float velocidade = 3.0f;
    [SerializeField] private float velocidadeRotacao = 15.0f;
    [SerializeField] private Transform transformCamera;

    [Header("Corrida")]
    [SerializeField] private float velocidadeCorrida = 6.0f;

    [Header("Pulo e gravidade")]
    [SerializeField] private float alturaPulo = 1.2f;
    [SerializeField] private float gravidade = -9.81f;

    private float velocidadeY;
    private bool estaNoChao;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        character = GetComponent<CharacterController>();

        if (transformCamera == null && Camera.main != null)
        {
            transformCamera = Camera.main.transform;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        ProcessarMovimento();
    }

    private void ProcessarMovimento()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // Direções da câmera no plano horizontal.
        Vector3 frenteCamera;
        Vector3 direitaCamera;

        if (transformCamera != null)
        {
            frenteCamera = transformCamera.forward;
            direitaCamera = transformCamera.right;
        }
        else
        {
            frenteCamera = transform.forward;
            direitaCamera = transform.right;
        }

        frenteCamera.y = 0f;
        direitaCamera.y = 0f;

        frenteCamera.Normalize();
        direitaCamera.Normalize();

        // Calcula a direção do movimento.
        Vector3 direcaoMovimento =
            frenteCamera * vertical +
            direitaCamera * horizontal;

        if (direcaoMovimento.sqrMagnitude > 1f)
        {
            direcaoMovimento.Normalize();
        }

        // Orienta o personagem conforme a câmera.
        if (frenteCamera.sqrMagnitude > 0.001f)
        {
            Quaternion rotacaoAlvo =
                Quaternion.LookRotation(frenteCamera);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                rotacaoAlvo,
                velocidadeRotacao * Time.deltaTime
            );
        }

        // Verifica o contato com o chão.
        estaNoChao = character.isGrounded;

        if (estaNoChao && velocidadeY < 0f)
        {
            velocidadeY = -2f;
        }

        // Pulo somente quando Jump é pressionado no chão.
        if (Input.GetButtonDown("Jump") && estaNoChao)
        {
            velocidadeY = Mathf.Sqrt(
                alturaPulo * -2f * gravidade
            );
        }

        // Aplica a gravidade.
        velocidadeY += gravidade * Time.deltaTime;

        // Detecta a corrida com Left Shift.
        bool temMovimento =
            Mathf.Abs(horizontal) > 0.1f ||
            Mathf.Abs(vertical) > 0.1f;

        bool correndo =
            Input.GetKey(KeyCode.LeftShift) &&
            temMovimento;

        float velocidadeAtual = correndo
            ? velocidadeCorrida
            : velocidade;

        // Atualiza o Animator.
        animator.SetBool(correndoHash, correndo);

        animator.SetFloat(
            inputXHash,
            horizontal,
            0.1f,
            Time.deltaTime
        );

        animator.SetFloat(
            inputYHash,
            vertical,
            0.1f,
            Time.deltaTime
        );

        // Combina movimento horizontal e vertical.
        Vector3 movimentoFinal =
            direcaoMovimento * velocidadeAtual +
            Vector3.up * velocidadeY;

        character.Move(
            movimentoFinal * Time.deltaTime
        );

        // Atualiza o estado do chão após o movimento.
        estaNoChao = character.isGrounded;

        animator.SetBool(noChaoHash, estaNoChao);
    }
}
