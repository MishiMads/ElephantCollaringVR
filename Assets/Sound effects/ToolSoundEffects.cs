using UnityEngine;

public class ToolSoundEffects : MonoBehaviour
{
    [Header("Audio Source")]
    [SerializeField] private AudioSource audioSource;

    [Header("Sound Effects")]
    [SerializeField] private AudioClip pickupSound;

    public void PlayPickupSound()
    {
        if (audioSource == null || pickupSound == null)
            return;

        audioSource.PlayOneShot(pickupSound);
    }
}