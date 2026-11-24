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
    [SerializeField] private float aceleracao = 10.0f;
    [SerializeField] private Transform transformCamera;

    [Header("Corrida")]
    [SerializeField] private float velocidadeCorrida = 6.0f;

    [Header("Pulo e gravidade")]
    [SerializeField] private float alturaPulo = 1.2f;
    [SerializeField] private float gravidade = -9.81f;

    [Header("Suavização da Animação")]
    [SerializeField] private float amortecimentoAnimacao = 0.15f;

    private float velocidadeAtual;
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
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector2 inputDirecional = new Vector2(horizontal, vertical);
        if (inputDirecional.sqrMagnitude > 1f)
        {
            inputDirecional.Normalize();
        }

        // Direções da câmera no plano horizontal
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

        // Calcula a direção do movimento baseada nos inputs normalizados
        Vector3 direcaoMovimento = frenteCamera * inputDirecional.y + direitaCamera * inputDirecional.x;

        // Orienta o personagem conforme a câmera
        if (frenteCamera.sqrMagnitude > 0.001f)
        {
            Quaternion rotacaoAlvo = Quaternion.LookRotation(frenteCamera);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                rotacaoAlvo,
                velocidadeRotacao * Time.deltaTime
            );
        }

        // Verifica o contato com o chão
        estaNoChao = character.isGrounded;

        if (estaNoChao && velocidadeY < 0f)
        {
            velocidadeY = -2f;
        }

        // Pulo com disparo imediato da animação
        if (Input.GetButtonDown("Jump") && estaNoChao)
        {
            velocidadeY = Mathf.Sqrt(alturaPulo * -2f * gravidade);
            estaNoChao = false;
            animator.SetBool(noChaoHash, false);
        }

        // Aplica a gravidade
        velocidadeY += gravidade * Time.deltaTime;

        // Detecta corrida: requer deslocamento para frente
        bool temMovimento = inputDirecional.sqrMagnitude > 0.01f;
        bool correndo = Input.GetKey(KeyCode.LeftShift) && temMovimento && inputDirecional.y > 0.1f;

        // Interpolação suave da velocidade física (elimina o efeito de deslizar os pés / foot sliding)
        float velocidadeAlvo = correndo ? velocidadeCorrida : (temMovimento ? velocidade : 0f);
        velocidadeAtual = Mathf.MoveTowards(velocidadeAtual, velocidadeAlvo, aceleracao * Time.deltaTime);

        // Atualiza o Animator com amortecimento (damp) consistente
        animator.SetBool(correndoHash, correndo);
        animator.SetFloat(inputXHash, inputDirecional.x, amortecimentoAnimacao, Time.deltaTime);
        animator.SetFloat(inputYHash, inputDirecional.y, amortecimentoAnimacao, Time.deltaTime);

        // Combina movimento horizontal e vertical
        Vector3 movimentoFinal = direcaoMovimento * velocidadeAtual + Vector3.up * velocidadeY;

        character.Move(movimentoFinal * Time.deltaTime);

        // Atualiza o estado do chão após a movimentação física
        estaNoChao = character.isGrounded;
        animator.SetBool(noChaoHash, estaNoChao);
    }
}