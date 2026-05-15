using UnityEngine;

public class MedkitTextureSwap : MonoBehaviour
{
    [Header("Healing Sound")]
    public AudioClip healingSound;

    private bool hasHealed = false;

    public void ApplyHealing()
    {
        if (hasHealed) return;
        hasHealed = true;

        if (MainScript.Instance != null)
        {
            MainScript.Instance.SetHealed();
        }

        PlayHealingSound();

        gameObject.SetActive(false);
    }

    private void PlayHealingSound()
    {
        if (healingSound == null)
            return;

        AudioSource.PlayClipAtPoint(healingSound, transform.position);
    }
}