using UnityEngine;
using Oculus.Interaction;
using System.Collections;

public class ToolSnap : MonoBehaviour
{
    [Header("Identity")]
    public string toolID;

    private float tiltDuration = 2.0f;

    private Grabbable _grabbable;
    private Transform _activeTarget;
    private bool _isInZone = false;

    void Start()
    {
        _grabbable = GetComponentInChildren<Grabbable>();
        if (_grabbable != null) _grabbable.WhenPointerEventRaised += HandlePointerEvent;
    }

    private void HandlePointerEvent(PointerEvent evt)
    {
        if (evt.Type == PointerEventType.Unselect && _isInZone && _activeTarget != null)
            SnapToTarget();
    }

    private void SnapToTarget()
    {
        if (TryGetComponent<CollarSwap>(out var swapScript)) swapScript.MakeSwap();

        if (_grabbable != null) _grabbable.enabled = false;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.detectCollisions = false;
        }

        transform.SetParent(_activeTarget);
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

        Debug.Log($"LOCKDOWN: {gameObject.name} successfully snapped.");
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
                _isInZone = true;
                _activeTarget = other.transform;
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
                _isInZone = true;
                _activeTarget = other.transform;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("SnapZone"))
        {
            // If already parented, we didn't exit, we snapped
            if (transform.parent == other.transform) return;

            _isInZone = false;
            _activeTarget = null;
        }
    }

    void Update()
    {
        // Debug Snap with keyboard
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (_isInZone && _activeTarget != null) SnapToTarget();
        }
    }

    void OnDestroy()
    {
        if (_grabbable != null)
            _grabbable.WhenPointerEventRaised -= HandlePointerEvent;
    }
}