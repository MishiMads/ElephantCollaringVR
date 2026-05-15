using UnityEngine;
using System.Collections;

public class SprayCanLogic : MonoBehaviour
{
    [Header("Spray Sound")]
    [SerializeField] private AudioSource sprayAudioSource;
    [SerializeField] private AudioClip spraySound;

    private bool hasSprayed = false;

    public void ApplySpray()
    {
        if (hasSprayed) return;
        hasSprayed = true;

        StartCoroutine(ApplySprayRoutine());
    }

    private IEnumerator ApplySprayRoutine()
    {
        if (MainScript.Instance != null)
        {
            MainScript.Instance.SetSprayed();
        }

        if (sprayAudioSource != null && spraySound != null)
        {
            sprayAudioSource.PlayOneShot(spraySound);

            yield return new WaitForSeconds(spraySound.length);
        }

        gameObject.SetActive(false);
    }
}