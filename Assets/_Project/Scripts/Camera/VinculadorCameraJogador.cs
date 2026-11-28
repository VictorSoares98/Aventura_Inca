using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Automatically binds CinemachineCamera.Target.TrackingTarget to the scene player
/// when instantiated via Prefab_Camera without manual cross-references.
/// </summary>
[RequireComponent(typeof(CinemachineCamera))]
public class VinculadorCameraJogador : MonoBehaviour
{
    [SerializeField] private string tagJogador = "Player";
    [SerializeField] private string nomeFilhoAlvo = "";

    private CinemachineCamera cinemachineCamera;

    private void Awake()
    {
        cinemachineCamera = GetComponent<CinemachineCamera>();
        VincularSeNecessario();
    }

    private void Start()
    {
        VincularSeNecessario();
    }

    private void VincularSeNecessario()
    {
        if (cinemachineCamera == null)
            return;

        if (cinemachineCamera.Target.TrackingTarget != null)
            return;

        Transform alvo = LocalizarTransformJogador();
        if (alvo == null)
        {
            Debug.LogWarning("[VinculadorCameraJogador] Nenhum jogador encontrado para vincular à câmera.", this);
            return;
        }

        CameraTarget target = cinemachineCamera.Target;
        target.TrackingTarget = alvo;
        cinemachineCamera.Target = target;
    }

    private Transform LocalizarTransformJogador()
    {
        GameObject jogadorObj = !string.IsNullOrEmpty(tagJogador)
            ? GameObject.FindWithTag(tagJogador)
            : null;

        if (jogadorObj == null)
        {
            ControleJogador controle = FindFirstObjectByType<ControleJogador>();
            if (controle != null)
                jogadorObj = controle.gameObject;
        }

        if (jogadorObj == null)
            return null;

        if (!string.IsNullOrEmpty(nomeFilhoAlvo))
        {
            Transform filho = jogadorObj.transform.Find(nomeFilhoAlvo);
            if (filho != null)
                return filho;
        }

        return jogadorObj.transform;
    }
}
