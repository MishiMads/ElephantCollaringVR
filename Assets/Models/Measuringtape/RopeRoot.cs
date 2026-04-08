using System.Collections.Generic;
using UnityEngine;

public class RopeRoot : MonoBehaviour
{
    [Header("Physics")]
    public float RigidbodyMass = 0.2f;
    public float ColliderRadius = 0.025f;
    public float JointSpring = 20f;
    public float JointDamper = 5f;
    public bool UseGravity = false;

    [Header("Copy Back To Bones")]
    public Vector3 RotationOffset;
    public Vector3 PositionOffset;

    private readonly List<Transform> copySource = new();
    private readonly List<Transform> copyDestination = new();

    private static GameObject rigidBodyContainer;

    private void Awake()
    {
        if (rigidBodyContainer == null)
        {
            rigidBodyContainer = new GameObject("RopeRigidbodyContainer");
        }

        copySource.Clear();
        copyDestination.Clear();

        BuildChain(transform, null);
    }

    private void BuildChain(Transform sourceParent, Transform connectedPhysicsTransform)
    {
        for (int i = 0; i < sourceParent.childCount; i++)
        {
            Transform sourceChild = sourceParent.GetChild(i);

            GameObject proxy = new GameObject(sourceChild.name + "_Physics");
            proxy.transform.SetParent(rigidBodyContainer.transform, true);
            proxy.transform.position = sourceChild.position;
            proxy.transform.rotation = sourceChild.rotation;

            Rigidbody rb = proxy.AddComponent<Rigidbody>();
            rb.mass = RigidbodyMass;
            rb.useGravity = UseGravity;
            rb.isKinematic = false;
            rb.freezeRotation = true;

            SphereCollider col = proxy.AddComponent<SphereCollider>();
            col.radius = ColliderRadius;
            col.center = Vector3.zero;

            DistanceJoint3D joint = proxy.AddComponent<DistanceJoint3D>();
            joint.ConnectedTransform = connectedPhysicsTransform != null ? connectedPhysicsTransform : sourceParent;
            joint.DetermineDistanceOnStart = true;
            joint.Spring = JointSpring;
            joint.Damper = JointDamper;

            copySource.Add(proxy.transform);
            copyDestination.Add(sourceChild);

            BuildChain(sourceChild, proxy.transform);
        }
    }

    private void LateUpdate()
    {
        for (int i = 0; i < copySource.Count; i++)
        {
            copyDestination[i].position = copySource[i].position + PositionOffset;
            copyDestination[i].rotation = copySource[i].rotation * Quaternion.Euler(RotationOffset);
        }
    }
}