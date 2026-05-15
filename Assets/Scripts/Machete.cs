using UnityEngine;
using System.Collections;

public class Machete : MonoBehaviour
{
    public string targetSocketID = "";

    [Header("Cut Sound")]
    [SerializeField] private AudioClip cutSound;
    [SerializeField] private float delayBeforeBushDisappears = 0.3f;

    private bool hasCut = false;

    private void OnTriggerEnter(Collider other)
    {
        if (hasCut) return;

        if (other.CompareTag("SnapZone"))
        {
            if (other.transform.parent != null)
            {
                hasCut = true;

                GameObject bushParent = other.transform.parent.gameObject;

                StartCoroutine(CutBushRoutine(bushParent, other));
            }
        }
    }

    private IEnumerator CutBushRoutine(GameObject bushParent, Collider snapZoneCollider)
    {
        // Prevent the trigger from firing again
        snapZoneCollider.enabled = false;

        // Play sound at the machete position
        if (cutSound != null)
        {
            AudioSource.PlayClipAtPoint(cutSound, transform.position);
        }

        // Wait before removing the bush
        yield return new WaitForSeconds(delayBeforeBushDisappears);

        Destroy(bushParent);
    }
}