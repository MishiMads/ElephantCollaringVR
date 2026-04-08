using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class DistanceJoint3D : MonoBehaviour
{
    public Transform ConnectedTransform;
    public bool DetermineDistanceOnStart = true;
    public float Distance;
    public float Spring = 20f;
    public float Damper = 5f;

    private Rigidbody rb;
    private Vector3 velocity;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        if (ConnectedTransform != null && DetermineDistanceOnStart)
        {
            Distance = Vector3.Distance(rb.position, ConnectedTransform.position);
        }
    }

    private void FixedUpdate()
    {
        if (ConnectedTransform == null)
            return;

        Vector3 delta = rb.position - ConnectedTransform.position;
        float currentDistance = delta.magnitude;

        if (currentDistance < 0.0001f)
            return;

        Vector3 direction = delta / currentDistance;
        float error = currentDistance - Distance;

        Vector3 springForce = -direction * (error * Spring);
        Vector3 dampingForce = -rb.linearVelocity * Damper;
        Vector3 totalForce = springForce + dampingForce;

        rb.AddForce(totalForce, ForceMode.Acceleration);
    }
}