using UnityEngine;
using Oculus.Interaction;

public class ToolSnap : MonoBehaviour
{
    [Header("Identity")]
    public string toolID;

    private Grabbable _grabbable;
    private Transform _activeTarget;
    private bool _isInZone = false;

    void Start()
    {
        // Fix: Look in children since your Grabbable is a child object
        _grabbable = GetComponentInChildren<Grabbable>();

        if (_grabbable != null)
        {
            _grabbable.WhenPointerEventRaised += HandlePointerEvent;
            Debug.Log($"{gameObject.name} successfully linked to Grabbable on child.");
        }
        else
        {
            Debug.LogError($"{gameObject.name} could not find a Grabbable component in its children!");
        }
    }

    private void HandlePointerEvent(PointerEvent evt)
    {
        // LOG EVERY EVENT TO SEE WHAT THE HAND IS DOING
        Debug.Log($"Event Detected: {evt.Type} | InZone: {_isInZone}");

        if (evt.Type == PointerEventType.Unselect)
        {
            if (_isInZone && _activeTarget != null)
            {
                SnapToTarget();
            }
        }
    }

    private void SnapToTarget()
    {
        // 1. Stop the SDK from fighting us immediately
        if (_grabbable != null) _grabbable.enabled = false;

        // 2. Disable all Interactables to force the hand to release
        IInteractable[] interactables = GetComponentsInChildren<IInteractable>();
        foreach (var interactable in interactables)
        {
            if (interactable is MonoBehaviour mono) mono.enabled = false;
        }

        // 3. Complete Physics Shutdown
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.interpolation = RigidbodyInterpolation.None; // Stops the "jitter" smoothing
            rb.detectCollisions = false; // Prevents the tool from bumping into the elephant
        }

        // 4. Parenting and Placement
        transform.SetParent(_activeTarget);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        Debug.Log($"LOCKDOWN: {gameObject.name} snapped. If it still jitters, check if the Socket is moving.");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("SnapZone"))
        {
            var socketID = other.GetComponent<ToolSocket>()?.socketID;
            Debug.Log($"Entered Zone: {other.name}. SocketID: {socketID}");

            if (socketID == toolID)
            {
                _isInZone = true;
                _activeTarget = other.transform;
            }
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log($"E Pressed. In Zone: {_isInZone} | Target: {(_activeTarget != null ? _activeTarget.name : "null")}");

            if (_isInZone && _activeTarget != null)
            {
                SnapToTarget();
            }
            else
            {
                Debug.LogWarning("Snap failed: Not in zone or no target.");
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"PHYSICS HIT: I bumped into {collision.gameObject.name}");
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("SnapZone"))
        {
            // If we are already parented to the socket, we didn't "exit," we joined it.
            if (transform.parent == other.transform) return;

            _isInZone = false;
            _activeTarget = null;
        }
    }

    void OnDestroy()
    {
        if (_grabbable != null)
            _grabbable.WhenPointerEventRaised -= HandlePointerEvent;
    }
}