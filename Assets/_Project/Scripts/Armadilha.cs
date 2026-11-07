using UnityEngine;

public class Armadilha : MonoBehaviour
{
    [Header("Configuração de Dano")]
    [SerializeField] private float dano = 20f;
    
    [Tooltip("Destruir o objeto após causar dano? (Ex: um projétil ou espinho que quebra)")]
    [SerializeField] private bool destruirAoTocar = false;

    private void OnTriggerEnter(Collider outro)
    {
        // Verifica se quem entrou no gatilho tem a Tag "Player"
        if (outro.CompareTag("Player"))
        {
            // Tenta pegar o script VidaPersonagem do objeto que colidiu
            VidaPersonagem vida = outro.GetComponent<VidaPersonagem>();

            if (vida != null)
            {
                // Aplica o dano chamando o método que você já criou
                vida.ReceberDano(dano);

                if (destruirAoTocar)
                {
                    Destroy(gameObject);
                }
            }
        }
    }
}