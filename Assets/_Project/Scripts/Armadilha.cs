using UnityEngine;

public class Armadilha : MonoBehaviour
{
    [Header("Configuração de Dano")]
    [SerializeField] private float dano = 20f;
    
    [Tooltip("Destruir o objeto após causar dano? (Ex: um projétil ou espinho que quebra)")]
    [SerializeField] private bool destruirAoTocar = false;

    [Header("Áudio")]
    [SerializeField] private AudioClip somAtivacao;
    [SerializeField, Range(0f, 1f)] private float volumeSom = 0.85f;

    private void OnTriggerEnter(Collider outro)
    {
        // Verifica se quem entrou no gatilho tem a Tag "Player"
        if (outro.CompareTag("Player"))
        {
            // Tenta pegar o script VidaPersonagem do objeto que colidiu
            VidaPersonagem vida = outro.GetComponent<VidaPersonagem>();

            if (vida != null)
            {
                bool estavaVivo = !vida.EstaMorto;

                // Aplica o dano chamando o método que você já criou
                vida.ReceberDano(dano);

                if (estavaVivo && somAtivacao != null)
                {
                    Vector3 posAudio = Camera.main != null ? Camera.main.transform.position : transform.position;
                    AudioSource.PlayClipAtPoint(somAtivacao, posAudio, volumeSom);
                }

                if (destruirAoTocar)
                {
                    Destroy(gameObject);
                }
            }
        }
    }
}