
using UnityEngine;
using Unity.Cinemachine;

public class InteracaoCarro : MonoBehaviour
{
    [Header("Personagem")]
    [SerializeField] private Transform personagem;

    [Header("Veículo")]
    [SerializeField] private ControleCarro controleCarro;
    [SerializeField] private Transform pontoMotorista;
    [SerializeField] private Transform pontoSaida;

    [Header("Câmeras Cinemachine 3")]
    [SerializeField] private CinemachineCamera cameraPersonagem;
    [SerializeField] private CinemachineCamera cameraCarro;

    [Header("Interação")]
    [SerializeField] private float distanciaEntrada = 3f;
    [SerializeField] private KeyCode teclaInteracao = KeyCode.E;

    private ControleJogador controleJogador;
    private CharacterController controladorPersonagem;
    private bool dentroDoCarro;

    private void Awake()
    {
        if (personagem != null)
        {
            controleJogador =
                personagem.GetComponent<ControleJogador>();

            controladorPersonagem =
                personagem.GetComponent<CharacterController>();
        }

        if (controleCarro != null)
            controleCarro.AtivarDirecao(false);
    }

    private void Update()
    {
        if (personagem == null ||
            controleCarro == null ||
            pontoMotorista == null ||
            pontoSaida == null)
        {
            return;
        }

        if (!dentroDoCarro)
        {
            float distancia = Vector3.Distance(
                personagem.position,
                pontoMotorista.position
            );

            if (distancia <= distanciaEntrada &&
                Input.GetKeyDown(teclaInteracao))
            {
                EntrarNoCarro();
            }
        }
        else if (Input.GetKeyDown(teclaInteracao))
        {
            SairDoCarro();
        }
    }

    private void EntrarNoCarro()
    {
        dentroDoCarro = true;

        if (controleJogador != null)
            controleJogador.enabled = false;

        if (controladorPersonagem != null)
            controladorPersonagem.enabled = false;

        personagem.SetParent(null);

        personagem.SetPositionAndRotation(
            pontoMotorista.position,
            pontoMotorista.rotation
        );

        personagem.SetParent(pontoMotorista);

        controleCarro.AtivarDirecao(true);

        if (cameraPersonagem != null)
            cameraPersonagem.Priority = 0;

        if (cameraCarro != null)
            cameraCarro.Priority = 20;
    }

    private void SairDoCarro()
    {
        dentroDoCarro = false;

        personagem.SetParent(null);

        personagem.SetPositionAndRotation(
            pontoSaida.position,
            pontoSaida.rotation
        );

        if (controladorPersonagem != null)
            controladorPersonagem.enabled = true;

        if (controleJogador != null)
            controleJogador.enabled = true;

        controleCarro.AtivarDirecao(false);

        if (cameraCarro != null)
            cameraCarro.Priority = 0;

        if (cameraPersonagem != null)
            cameraPersonagem.Priority = 20;
    }
}
