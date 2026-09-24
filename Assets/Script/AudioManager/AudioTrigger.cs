using UnityEngine;

public class AudioTrigger : MonoBehaviour
{
    [Header("Sound")]
    public AudioClip audioClip;

    [Range(0f, 1f)]
    public float volume = 1f;

    [Header("Optional Position")]
    [Tooltip("If empty, use this object's position.")]
    public Transform spawnPoint;

    public void PlaySound()
    {
        if (AudioManager.Instance == null)
        {
            Debug.LogWarning(
                "[AUDIO] No AudioManager found in the scene.",
                this
            );

            return;
        }

        if (audioClip == null)
        {
            Debug.LogWarning(
                "[AUDIO] Assign an AudioClip on " + gameObject.name,
                this
            );

            return;
        }

        Transform soundPosition =
            spawnPoint != null ? spawnPoint : transform;

        AudioManager.Instance.PlayAudioClip(
            audioClip,
            soundPosition,
            volume
        );
    }
}