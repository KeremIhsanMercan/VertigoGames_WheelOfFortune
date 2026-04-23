using UnityEngine;

public interface IAudioService
{
    void PlaySfx(AudioClip clip, float volume = 1f);
    void PlayButtonClick();
    void PlayRevive();
    void PlayBombExplosion();
}