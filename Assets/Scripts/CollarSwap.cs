using UnityEngine;

public class CollarSwap : MonoBehaviour
{
    public GameObject collarToEnable;
    public GameObject collarToDisable;

    [Header("Collar Sound")]
    public AudioClip collarSwapSound;

    private bool hasSwapped = false;

    public void MakeSwap()
    {
        if (hasSwapped) return;
        hasSwapped = true;

        if (collarToEnable != null)
            collarToEnable.SetActive(true);

        if (collarToDisable != null)
            collarToDisable.SetActive(false);

        PlayCollarSwapSound();
    }

    private void PlayCollarSwapSound()
    {
        if (collarSwapSound == null)
            return;

        AudioSource.PlayClipAtPoint(collarSwapSound, transform.position);
    }
}