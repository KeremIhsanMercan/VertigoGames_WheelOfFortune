using UnityEngine;

public class AudioManager : MonoBehaviour, IAudioService
{
    public static AudioManager Instance;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource sfxSource;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private AudioClip reviveSound;
    [SerializeField] private AudioClip bombExplosionSound;

    private void Awake()
    {
        // Singleton Pattern
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

    public void PlaySfx(AudioClip clip, float volume = 1f)
    {
        if (clip == null || sfxSource == null)
            return;

        sfxSource.PlayOneShot(clip, volume);
    }

    public void PlayButtonClick()
    {
        PlaySfx(clickSound);
    }

    public void PlayRevive()
    {
        PlaySfx(reviveSound);
    }

    public void PlayBombExplosion()
    {
        PlaySfx(bombExplosionSound);
    }
}