using UnityEngine;

public class RecenterOrigin : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraRig;   // OVRCameraRig root
    [SerializeField] private Transform centerEye;   // CenterEyeAnchor
    [SerializeField] private Transform target;      // Where player should go

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            Recenter();
        }
    }

    public void Recenter()
    {
        if (cameraRig == null || centerEye == null || target == null)
        {
            Debug.LogWarning("Missing references!");
            return;
        }

        // --- ROTATION FIRST ---
        Vector3 currentForward = Vector3.ProjectOnPlane(centerEye.forward, Vector3.up);
        Vector3 targetForward = Vector3.ProjectOnPlane(target.forward, Vector3.up);

        float angle = Vector3.SignedAngle(currentForward, targetForward, Vector3.up);
        cameraRig.Rotate(Vector3.up, angle);

        // --- POSITION AFTER ROTATION ---
        Vector3 offset = centerEye.position - cameraRig.position;

        Vector3 newPosition = target.position - offset;

        // Optional: lock height if needed
        newPosition.y = cameraRig.position.y;

        cameraRig.position = newPosition;
    }
}