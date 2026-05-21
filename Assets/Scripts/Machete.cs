using UnityEngine;
using System.Collections;

public class Machete : MonoBehaviour
{
    public string targetSocketID = "";

    [Header("Cut Sound")]
    [SerializeField] private AudioClip cutSound;
    [SerializeField] private float delayBeforeBushDisappears = 0.3f;

    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField][Range(0f, 1f)] private float volume = 0.3f;

    private bool hasCut = false;

    private void OnTriggerEnter(Collider other)
    {
        if (hasCut) return;

        if (!other.CompareTag("SnapZone")) return;

        var socket = other.GetComponent<ToolSocket>();

        // ONLY allow specific target
        if (socket == null || socket.socketID != targetSocketID)
            return;

        if (other.transform.parent == null)
            return;

        hasCut = true;

        GameObject bushParent = other.transform.parent.gameObject;

        StartCoroutine(CutBushRoutine(bushParent, other));
    }

    private IEnumerator CutBushRoutine(GameObject bushParent, Collider snapZoneCollider)
    {
        snapZoneCollider.enabled = false;

        // Play sound using AudioSource with volume control
        if (cutSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(cutSound, volume);
        }

        yield return new WaitForSeconds(delayBeforeBushDisappears);

        Destroy(bushParent);
    }
}