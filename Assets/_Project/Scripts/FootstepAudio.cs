using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class FootstepAudio : MonoBehaviour
{
    [SerializeField] private AudioClip[] footstepClips;
    [SerializeField] private Vector2 pitchRange = new Vector2(0.92f, 1.08f);
    [SerializeField] private Vector2 volumeRange = new Vector2(0.8f, 1.0f);

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    // Chamado pelo Animation Event nas animações de caminhada/corrida
    public void PlayFootstep()
    {
        if (footstepClips == null || footstepClips.Length == 0) return;

        AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];
        audioSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
        float volume = Random.Range(volumeRange.x, volumeRange.y);

        audioSource.PlayOneShot(clip, volume);
    }
}