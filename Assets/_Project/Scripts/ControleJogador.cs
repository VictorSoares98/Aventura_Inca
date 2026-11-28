
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

    [Header("Áudio dos Passos")]
    [SerializeField] private AudioSource passosAudioSource;
    [SerializeField] private AudioClip[] passosAudioClips;
    [SerializeField, Range(0f, 1f)] private float volumePassos = 0.85f;
    [SerializeField, Min(0.15f)] private float intervaloPassoCaminhada = 0.46f;

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

    [Header("Física e Interação")]
    [SerializeField] private float forcaEmpurrao = 2.5f;

    private float velocidadeAtual;
    private float velocidadeY;
    private bool estaNoChao;
    private float cronometroPassoFallback;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        character = GetComponent<CharacterController>();

        if (transformCamera == null && Camera.main != null)
        {
            transformCamera = Camera.main.transform;
        }

        // Garante um AudioSource próprio de efeito (2D, sem loop) para não conflitar com a música de fundo
        if (passosAudioSource == null || passosAudioSource.loop)
        {
            passosAudioSource = gameObject.AddComponent<AudioSource>();
            passosAudioSource.playOnAwake = false;
            passosAudioSource.loop = false;
            passosAudioSource.spatialBlend = 0f;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Start()
    {
        if (character == null)
            character = GetComponent<CharacterController>();

        if (character != null && !character.enabled)
            character.enabled = true;
    }

    private void Update()
    {
        // Pressionar [R] permite reviver caso esteja morto
        if (Input.GetKeyDown(KeyCode.R))
        {
            VidaPersonagem vida = GetComponent<VidaPersonagem>();
            if (vida != null && vida.EstaMorto)
            {
                vida.Renascer();
                return;
            }
        }

        ProcessarMovimento();
    }

    /// <summary>
    /// Restaura o movimento e o CharacterController do jogador caso tenham sido desabilitados.
    /// </summary>
    public void ForcarAtivarMovimento()
    {
        enabled = true;
        if (character == null)
            character = GetComponent<CharacterController>();
        if (character != null)
            character.enabled = true;
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

        Vector3 direcaoMovimento =
            frenteCamera * inputDirecional.y +
            direitaCamera * inputDirecional.x;

        // Rotação do personagem
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

        // Pulo
        if (Input.GetButtonDown("Jump") && estaNoChao)
        {
            velocidadeY = Mathf.Sqrt(alturaPulo * -2f * gravidade);

            estaNoChao = false;
            animator.SetBool(noChaoHash, false);
        }

        // Gravidade
        velocidadeY += gravidade * Time.deltaTime;

        // Corrida
        bool temMovimento = inputDirecional.sqrMagnitude > 0.01f;

        bool correndo =
            Input.GetKey(KeyCode.LeftShift) &&
            temMovimento &&
            inputDirecional.y > 0.1f;

        float velocidadeAlvo = correndo
            ? velocidadeCorrida
            : (temMovimento ? velocidade : 0f);

        velocidadeAtual = Mathf.MoveTowards(
            velocidadeAtual,
            velocidadeAlvo,
            aceleracao * Time.deltaTime
        );

        // Atualização das animações
        animator.SetBool(correndoHash, correndo);

        animator.SetFloat(
            inputXHash,
            inputDirecional.x,
            amortecimentoAnimacao,
            Time.deltaTime
        );

        animator.SetFloat(
            inputYHash,
            inputDirecional.y,
            amortecimentoAnimacao,
            Time.deltaTime
        );

        // Movimento físico
        Vector3 movimentoFinal =
            direcaoMovimento * velocidadeAtual +
            Vector3.up * velocidadeY;

        character.Move(movimentoFinal * Time.deltaTime);

        // Atualiza estado do chão
        estaNoChao = character.isGrounded;
        animator.SetBool(noChaoHash, estaNoChao);

        // Fallback para clipes de caminhada sem Animation Event 'Passos'
        if (estaNoChao && velocidadeAtual > 0.35f && !correndo)
        {
            cronometroPassoFallback -= Time.deltaTime;
            if (cronometroPassoFallback <= 0f)
            {
                Passos();
                cronometroPassoFallback = intervaloPassoCaminhada;
            }
        }
        else
        {
            cronometroPassoFallback = 0.12f;
        }
    }

    // Áudio dos passos
    private void Passos()
    {
        if (!estaNoChao || velocidadeAtual < 0.2f)
            return;

        cronometroPassoFallback = intervaloPassoCaminhada;

        if (passosAudioSource != null &&
            passosAudioClips != null &&
            passosAudioClips.Length > 0)
        {
            int index = Random.Range(0, passosAudioClips.Length);

            AudioClip clip = passosAudioClips[index];

            if (clip != null)
            {
                passosAudioSource.pitch = Random.Range(0.93f, 1.07f);
                passosAudioSource.PlayOneShot(clip, volumePassos);
            }
        }
    }

    // Interação física com objetos
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Rigidbody corpoRigido = hit.collider.attachedRigidbody;

        if (corpoRigido == null || corpoRigido.isKinematic)
            return;

        // Evita empurrar o chão
        if (hit.moveDirection.y < -0.3f)
            return;

        Vector3 direcaoEmpurrao = new Vector3(
            hit.moveDirection.x,
            0f,
            hit.moveDirection.z
        );

        corpoRigido.AddForce(
            direcaoEmpurrao * forcaEmpurrao,
            ForceMode.Impulse
        );
    }
}
