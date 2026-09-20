using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] private AudioSource audioSourcePrefab;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public void PlayAudioClip(AudioClip clip, Transform spawnTransform, float volume)
    {
        AudioSource audioSource = Instantiate(audioSourcePrefab, spawnTransform.position, Quaternion.identity);
        //spawn in game object

        //assign the audio clip
        audioSource.clip = clip;
        //assign volume
                audioSource.volume = volume;
        //play sound
        audioSource.Play();
        //get length of sound
        float clipLength = clip.length;
        //Destroy the clip after it has finished playing
        Destroy(audioSource.gameObject, clipLength);
    }

    public void PlayRandomAudioClip(AudioClip[] clip, Transform spawnTransform, float volume)
    {
        //assign a random index
        int rand = Random.Range(0, clip.Length);

        AudioSource audioSource = Instantiate(audioSourcePrefab, spawnTransform.position, Quaternion.identity);
        //spawn in game object

        //assign the audio clip
        audioSource.clip = clip[rand];
        //assign volume
        audioSource.volume = volume;
        //play sound
        audioSource.Play();
        //get length of sound
        float clipLength = clip[rand].length;
        //Destroy the clip after it has finished playing
        Destroy(audioSource.gameObject, clipLength);
    }


}
