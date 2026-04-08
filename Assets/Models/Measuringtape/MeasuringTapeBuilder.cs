using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction;

public class MeasuringTapeBuilder : MonoBehaviour
{
    public Transform armatureRoot;

    public TouchHandGrabInteractable touchHandGrabInteractable;
    public BoxCollider interactionBoundsCollider;
    public Transform interactionColliderContainer;
    public float interactionColliderRadius = 0.015f;

    public int solverIterations = 12;
    public float gravity = -9.81f;
    public float damping = 0.995f;
    public float bendStrength = 0.35f;
    public float visualSmoothing = 20f;

    public LayerMask collisionMask;
    public float collisionRadius = 0.01f;
    public float collisionPadding = 0.002f;
    public int maxCollidersPerPoint = 8;

    public bool pinRoot = true;
    public Transform rootTarget;

    public Transform leftHandTarget;
    public int leftHandIndex = -1;
    public Transform rightHandTarget;
    public int rightHandIndex = -1;

    public Vector3 boneRotationOffsetEuler;

    public List<Transform> bones = new List<Transform>();

    private readonly List<float> segmentLengths = new List<float>();
    private readonly List<SphereCollider> interactionColliders = new List<SphereCollider>();

    private Vector3[] points;
    private Vector3[] previousPoints;
    private Quaternion[] bindRotations;
    private Vector3[] bindDirections;
    private Quaternion rotationOffset;

    private Collider[] collisionBuffer;
    private SphereCollider worldCollisionSphere;
    private GameObject worldCollisionProbe;

    void Awake()
    {
        BuildBoneList();
        CacheSegmentLengths();
        CacheBindData();
        InitializeSimulation();
        BuildInteractionColliders();
        InjectTouchGrabColliders();

        collisionBuffer = new Collider[Mathf.Max(1, maxCollidersPerPoint)];

        worldCollisionProbe = new GameObject("TapeCollisionProbe");
        worldCollisionProbe.hideFlags = HideFlags.HideAndDontSave;
        worldCollisionProbe.transform.SetParent(transform, false);

        worldCollisionSphere = worldCollisionProbe.AddComponent<SphereCollider>();
        worldCollisionSphere.isTrigger = true;
        worldCollisionSphere.radius = collisionRadius;
    }

    void Update()
    {
        if (points == null || points.Length < 2)
            return;

        Simulate(Time.deltaTime);
        ApplyToBones();
        UpdateInteractionColliders();
        UpdateInteractionBounds();
    }

    void BuildBoneList()
    {
        bones.Clear();

        Transform current = armatureRoot;
        bones.Add(current);

        while (current.childCount > 0)
        {
            Transform next = current.GetChild(0);
            if (next.name.EndsWith("_end"))
                break;

            bones.Add(next);
            current = next;
        }
    }

    void CacheSegmentLengths()
    {
        segmentLengths.Clear();

        for (int i = 1; i < bones.Count; i++)
        {
            segmentLengths.Add(Vector3.Distance(bones[i - 1].position, bones[i].position));
        }
    }

    void CacheBindData()
    {
        int count = bones.Count;

        bindRotations = new Quaternion[count];
        bindDirections = new Vector3[count - 1];
        rotationOffset = Quaternion.Euler(boneRotationOffsetEuler);

        for (int i = 0; i < count; i++)
        {
            bindRotations[i] = bones[i].rotation;
        }

        for (int i = 0; i < count - 1; i++)
        {
            Vector3 dir = bones[i + 1].position - bones[i].position;
            bindDirections[i] = dir.sqrMagnitude > 0.000001f ? dir.normalized : Vector3.forward;
        }
    }

    void InitializeSimulation()
    {
        int count = bones.Count;
        points = new Vector3[count];
        previousPoints = new Vector3[count];

        for (int i = 0; i < count; i++)
        {
            points[i] = bones[i].position;
            previousPoints[i] = bones[i].position;
        }
    }

    void BuildInteractionColliders()
    {
        if (interactionColliderContainer == null)
        {
            GameObject go = new GameObject("TapeInteractionColliders");
            go.transform.SetParent(transform, false);
            interactionColliderContainer = go.transform;
        }

        for (int i = interactionColliderContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(interactionColliderContainer.GetChild(i).gameObject);
        }

        interactionColliders.Clear();

        if (interactionBoundsCollider == null)
        {
            GameObject go = new GameObject("TapeInteractionBounds");
            go.transform.SetParent(transform, false);
            interactionBoundsCollider = go.AddComponent<BoxCollider>();
            interactionBoundsCollider.isTrigger = true;
        }

        for (int i = 0; i < points.Length; i++)
        {
            GameObject go = new GameObject("TapeGrabPoint_" + i);
            go.transform.SetParent(interactionColliderContainer, true);
            go.transform.position = points[i];

            SphereCollider col = go.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = interactionColliderRadius;

            interactionColliders.Add(col);
        }
    }

    void InjectTouchGrabColliders()
    {
        List<Collider> cols = new List<Collider>(interactionColliders.Count);
        for (int i = 0; i < interactionColliders.Count; i++)
        {
            cols.Add(interactionColliders[i]);
        }

        touchHandGrabInteractable.InjectAllTouchHandGrabInteractable(interactionBoundsCollider, cols);
    }

    void UpdateInteractionColliders()
    {
        for (int i = 0; i < points.Length; i++)
        {
            interactionColliders[i].transform.position = points[i];
        }
    }

    void UpdateInteractionBounds()
    {
        Vector3 min = points[0];
        Vector3 max = points[0];

        for (int i = 1; i < points.Length; i++)
        {
            min = Vector3.Min(min, points[i]);
            max = Vector3.Max(max, points[i]);
        }

        float pad = interactionColliderRadius * 2f;
        min -= Vector3.one * pad;
        max += Vector3.one * pad;

        interactionBoundsCollider.transform.position = (min + max) * 0.5f;
        interactionBoundsCollider.transform.rotation = Quaternion.identity;
        interactionBoundsCollider.center = Vector3.zero;
        interactionBoundsCollider.size = max - min;
    }

    void Simulate(float dt)
    {
        ApplyPins();

        Vector3 gravityStep = new Vector3(0f, gravity, 0f) * (dt * dt);

        for (int i = 0; i < points.Length; i++)
        {
            if (IsPinned(i))
                continue;

            Vector3 current = points[i];
            Vector3 velocity = (points[i] - previousPoints[i]) * damping;

            points[i] += velocity + gravityStep;
            previousPoints[i] = current;

            ResolveMotionCollision(i);
        }

        for (int iteration = 0; iteration < solverIterations; iteration++)
        {
            ApplyPins();
            SolveDistanceConstraints();
            SolveBendConstraints();
            SolveWorldCollisions();
            ApplyPins();
        }
    }

    void ResolveMotionCollision(int index)
    {
        if (IsPinned(index))
            return;

        Vector3 start = previousPoints[index];
        Vector3 end = points[index];
        Vector3 move = end - start;
        float moveDist = move.magnitude;

        if (moveDist < 0.000001f)
            return;

        if (Physics.SphereCast(start, collisionRadius, move.normalized, out RaycastHit hit, moveDist, collisionMask, QueryTriggerInteraction.Ignore))
        {
            points[index] = hit.point + hit.normal * (collisionRadius + collisionPadding);
        }
    }

    void SolveWorldCollisions()
    {
        worldCollisionSphere.radius = collisionRadius;

        for (int i = 0; i < points.Length; i++)
        {
            if (IsPinned(i))
                continue;

            int hitCount = Physics.OverlapSphereNonAlloc(
                points[i],
                collisionRadius + collisionPadding,
                collisionBuffer,
                collisionMask,
                QueryTriggerInteraction.Ignore
            );

            for (int h = 0; h < hitCount; h++)
            {
                Collider col = collisionBuffer[h];
                if (col == null)
                    continue;

                bool overlapped = Physics.ComputePenetration(
                    worldCollisionSphere,
                    points[i],
                    Quaternion.identity,
                    col,
                    col.transform.position,
                    col.transform.rotation,
                    out Vector3 direction,
                    out float distance
                );

                if (overlapped && distance > 0f)
                {
                    points[i] += direction * (distance + collisionPadding);
                }
            }
        }
    }

    void SolveDistanceConstraints()
    {
        for (int i = 0; i < points.Length - 1; i++)
        {
            Vector3 delta = points[i + 1] - points[i];
            float distance = delta.magnitude;

            if (distance < 0.000001f)
                continue;

            float error = distance - segmentLengths[i];
            Vector3 correction = delta / distance * error;

            bool aPinned = IsPinned(i);
            bool bPinned = IsPinned(i + 1);

            if (aPinned && bPinned) continue;
            if (aPinned) points[i + 1] -= correction;
            else if (bPinned) points[i] += correction;
            else
            {
                points[i] += correction * 0.5f;
                points[i + 1] -= correction * 0.5f;
            }
        }
    }

    void SolveBendConstraints()
    {
        if (bendStrength <= 0f)
            return;

        for (int i = 0; i < points.Length - 2; i++)
        {
            float targetDistance = segmentLengths[i] + segmentLengths[i + 1];
            Vector3 delta = points[i + 2] - points[i];
            float distance = delta.magnitude;

            if (distance < 0.000001f)
                continue;

            float error = distance - targetDistance;
            Vector3 correction = (delta / distance) * (error * bendStrength);

            bool aPinned = IsPinned(i);
            bool bPinned = IsPinned(i + 2);

            if (aPinned && bPinned) continue;
            if (aPinned) points[i + 2] -= correction;
            else if (bPinned) points[i] += correction;
            else
            {
                points[i] += correction * 0.5f;
                points[i + 2] -= correction * 0.5f;
            }
        }
    }

    void ApplyPins()
    {
        if (pinRoot)
        {
            Vector3 rootPos = rootTarget != null ? rootTarget.position : bones[0].position;
            SetPinnedPoint(0, rootPos);
        }

        if (leftHandTarget != null && leftHandIndex >= 0 && leftHandIndex < points.Length)
            SetPinnedPoint(leftHandIndex, leftHandTarget.position);

        if (rightHandTarget != null && rightHandIndex >= 0 && rightHandIndex < points.Length)
            SetPinnedPoint(rightHandIndex, rightHandTarget.position);
    }

    bool IsPinned(int index)
    {
        if (pinRoot && index == 0) return true;
        if (leftHandTarget != null && index == leftHandIndex) return true;
        if (rightHandTarget != null && index == rightHandIndex) return true;
        return false;
    }

    void ApplyToBones()
    {
        for (int i = 0; i < bones.Count; i++)
        {
            bones[i].position = Vector3.Lerp(bones[i].position, points[i], visualSmoothing * Time.deltaTime);
        }

        for (int i = 0; i < bones.Count - 1; i++)
        {
            Vector3 dir = points[i + 1] - points[i];
            if (dir.sqrMagnitude < 0.000001f)
                continue;

            Quaternion deltaRotation = Quaternion.FromToRotation(bindDirections[i], dir.normalized);
            Quaternion targetRotation = deltaRotation * bindRotations[i] * rotationOffset;

            bones[i].rotation = Quaternion.Slerp(
                bones[i].rotation,
                targetRotation,
                visualSmoothing * Time.deltaTime
            );
        }

        bones[bones.Count - 1].rotation = bones[bones.Count - 2].rotation;
    }

    public int GetClosestPointIndex(Vector3 worldPosition)
    {
        int closestIndex = 0;
        float closestSqr = float.MaxValue;

        for (int i = 0; i < points.Length; i++)
        {
            float sqr = (points[i] - worldPosition).sqrMagnitude;
            if (sqr < closestSqr)
            {
                closestSqr = sqr;
                closestIndex = i;
            }
        }

        return closestIndex;
    }

    public void GrabLeftHand(Transform handTransform)
    {
        leftHandTarget = handTransform;
        leftHandIndex = handTransform != null ? GetClosestPointIndex(handTransform.position) : -1;

        if (leftHandTarget != null && leftHandIndex >= 0)
            SetPinnedPoint(leftHandIndex, leftHandTarget.position);
    }

    public void GrabRightHand(Transform handTransform)
    {
        rightHandTarget = handTransform;
        rightHandIndex = handTransform != null ? GetClosestPointIndex(handTransform.position) : -1;

        if (rightHandTarget != null && rightHandIndex >= 0)
            SetPinnedPoint(rightHandIndex, rightHandTarget.position);
    }

    public void ReleaseLeftHand()
    {
        if (leftHandIndex >= 0 && leftHandIndex < points.Length)
            previousPoints[leftHandIndex] = points[leftHandIndex];

        leftHandTarget = null;
        leftHandIndex = -1;
    }

    public void ReleaseRightHand()
    {
        if (rightHandIndex >= 0 && rightHandIndex < points.Length)
            previousPoints[rightHandIndex] = points[rightHandIndex];

        rightHandTarget = null;
        rightHandIndex = -1;
    }

    private void SetPinnedPoint(int index, Vector3 worldPos)
    {
        if (index < 0 || index >= points.Length)
            return;

        points[index] = worldPos;
        previousPoints[index] = worldPos;
    }

    void OnDestroy()
    {
        if (worldCollisionProbe != null)
            Destroy(worldCollisionProbe);
    }
}