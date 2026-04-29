using UnityEngine;
using Oculus.Interaction;
using System.Collections;

public class ToolSnap : MonoBehaviour
{
    [Header("Identity")]
    public string toolID;

    private float tiltDuration = 2.0f;

    private Grabbable grabbable;
    private Transform activeTarget;
    private bool isInZone = false;

    void Start()
    {
        grabbable = GetComponentInChildren<Grabbable>();
        if (grabbable != null) grabbable.WhenPointerEventRaised += HandlePointerEvent;
    }

    private void HandlePointerEvent(PointerEvent evt)
    {
        if (evt.Type == PointerEventType.Unselect && isInZone && activeTarget != null)
            SnapToTarget();
    }

    private void SnapToTarget()
    {
        // Check for Spray Can
        if (TryGetComponent<SprayCanLogic>(out var spray))
        {
            spray.ApplySpray();
            return;
        }

        // Check for Medkit
        if (TryGetComponent<MedkitTextureSwap>(out var medkit))
        {
            medkit.ApplyHealing();
            return;
        }

        // Prevent Syringe from snapping so user keeps holding it
        if (TryGetComponent<SyringeAnimation>(out var syringe))
        {
            return;
        }

        if (TryGetComponent<CollarSwap>(out var swapScript)) swapScript.MakeSwap();

        if (grabbable != null) grabbable.enabled = false;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.detectCollisions = false;
        }

        transform.SetParent(activeTarget);
        transform.localPosition = Vector3.zero;

        // Check if this is the bucket (has Wobble)
        Wobble water = GetComponentInChildren<Wobble>();

        if (water != null)
        {
            // BUCKET LOGIC: Start at 90 and begin the slow tilt back to 0
            water.StartAutoPour();
            StartCoroutine(AnimateBucketTilt(water));
        }
        else
        {
            // STETHOSCOPE/OTHER LOGIC: Snap to perfectly upright instantly
            transform.localRotation = Quaternion.identity;
        }
    }

    // Pass the water script in so we don't have to find it again
    IEnumerator AnimateBucketTilt(Wobble water)
    {
        float elapsed = 0;
        // Use the variable from the header instead of a hard-coded number
        float slowTiltDuration = tiltDuration;

        Quaternion startRot = Quaternion.Euler(0, 0, 90);
        Quaternion endRot = Quaternion.identity;

        transform.localRotation = startRot;

        while (elapsed < slowTiltDuration)
        {
            elapsed += Time.deltaTime;
            transform.localRotation = Quaternion.Slerp(startRot, endRot, elapsed / slowTiltDuration);
            yield return null;
        }

        transform.localRotation = endRot;
    }



    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("SnapZone"))
        {
            var socket = other.GetComponent<ToolSocket>();
            if (socket != null && socket.socketID == toolID)
            {
                isInZone = true;
                activeTarget = other.transform;

                Debug.Log($"In Zone: {other.name}");
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        // Debug every trigger hit to see what is around the tool
        Debug.Log($"{gameObject.name} is touching trigger: {other.name} (Tag: {other.tag})");

        if (other.CompareTag("SnapZone"))
        {
            var socket = other.GetComponent<ToolSocket>();
            if (socket != null && socket.socketID == toolID)
            {
                isInZone = true;
                activeTarget = other.transform;
            }
        }

        // Check if the thing we touched is the correct socket
        if (other.name == "BloddrawSocket" || other.CompareTag("SnapZone"))
        {
            // Look for the script on THIS object (the syringe), not 'other'
            SyringeAnimation ani = GetComponent<SyringeAnimation>();

            if (ani != null)
            {
                ani.PlayBloodAnimation();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("SnapZone"))
        {
            // If already parented, we didn't exit, we snapped
            if (transform.parent == other.transform) return;

            isInZone = false;
            activeTarget = null;
        }
    }

    void Update()
    {
        // Debug Snap with keyboard
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (isInZone && activeTarget != null) SnapToTarget();
        }
    }

    void OnDestroy()
    {
        if (grabbable != null)
            grabbable.WhenPointerEventRaised -= HandlePointerEvent;
    }
}