using UnityEngine;

public class MeasuringTapeLogic : MonoBehaviour
{
    public AudioSource audioSource;

    public void UseTape()
    {
        // Play sound
        if (audioSource != null)
        {
            audioSource.Play();
        }

        // Tell your main system the task is done
        if (MainScript.Instance != null)
        {
            MainScript.Instance.SetFootMeasured();
        }

        // Start disappear after sound
        StartCoroutine(DisappearAfterSound());
    }

    private System.Collections.IEnumerator DisappearAfterSound()
    {
        // Wait for sound (or fallback time)
        float waitTime = audioSource != null ? audioSource.clip.length : 0.3f;

        yield return new WaitForSeconds(waitTime);

        gameObject.SetActive(false); // or Destroy(gameObject);
    }
}