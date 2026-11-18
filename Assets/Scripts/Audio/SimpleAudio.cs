
using UnityEngine;

/// <summary>
/// Componente reutilizable para reproducir sonidos.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class SimpleAudio : MonoBehaviour
{
    [Header("Audio Clips")]
    [SerializeField] private AudioClip _attackSound;
    [SerializeField] private AudioClip _hitSound;
    [SerializeField] private AudioClip _deathSound;

    private AudioSource _audioSource;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();

        _audioSource.playOnAwake = false;
    }

    public void PlayAttack()
    {
        PlaySound(_attackSound);
    }

    public void PlayHit()
    {
        PlaySound(_hitSound);
    }

    public void PlayDeath()
    {
        PlaySound(_deathSound);
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip == null)
            return;

        _audioSource.PlayOneShot(clip);
    }
}
