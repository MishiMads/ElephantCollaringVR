using System.Collections;
using UnityEngine;

public class NPCSpeaker : MonoBehaviour
{
    private AudioSource audioSource;
    //private Animator animator;

    void Awake()
    {
        //audioSource = GetComponent<AudioSource>();
        //animator = GetComponent<Animator>();
    }

    public void PlaySpeech(AudioClip clip)
    {
        audioSource.clip = clip;
        audioSource.Play();

        // Trigger talking animation
        //animator.SetBool("IsTalking", true);

        // Stop talking when done
        //StartCoroutine(StopTalkingWhenDone(clip.length));
        Debug.Log("NPC 'speaking' (silent clip playing)");
    }

    private IEnumerator StopTalkingWhenDone(float duration)
    {
        yield return new WaitForSeconds(duration);
        //animator.SetBool("IsTalking", false);
    }
}
