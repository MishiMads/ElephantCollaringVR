using System.Collections;
using UnityEngine;

public class BrieferLogic : MonoBehaviour
{
    public Animator animatorBrief;

    [Header("Bones")]
    public Transform spine;
    public Transform head;

    [Header("Target")]
    public Transform cameraTarget; // assign Camera.main transform or VR camera

    [Header("Limits")]
    public float headMaxYaw = 100f;
    public float spineMaxYaw = 100f;
    public float rotationSpeed = 5f;

    private Quaternion headStartLocalRot;
    private Quaternion spineStartLocalRot;

    Coroutine talkingRoutine;

    void Start()
    {
        if (animatorBrief == null)
            animatorBrief = GetComponent<Animator>();

        if (cameraTarget == null && Camera.main != null)
            cameraTarget = Camera.main.transform;

        headStartLocalRot = head.localRotation;
        spineStartLocalRot = spine.localRotation;
    }

    public void brieferSpeak()
    {
        if (talkingRoutine != null)
            StopCoroutine(talkingRoutine);

        talkingRoutine = StartCoroutine(TalkingLoop());
    }

    IEnumerator TalkingLoop()
    {
        animatorBrief.SetBool("isTalking", true);

        while (true)
        {
            animatorBrief.SetInteger("talkVariant", Random.Range(0, 4));
            yield return new WaitForSeconds(2f);
        }
    }

    public void brieferNoSpeak()
    {
        animatorBrief.SetBool("isTalking", false);
    }

    void LateUpdate()
    {
        if (cameraTarget == null) return;
        RotateTowardsCamera();
    }

    void RotateTowardsCamera()
    {
        // Direction from body to camera, flattened on Y axis
        Vector3 toCamera = cameraTarget.position - transform.position;
        toCamera.y = 0f;

        if (toCamera.sqrMagnitude < 0.001f) return;

        // World rotation needed for the whole character to face the camera
        Quaternion targetBodyWorldRot = Quaternion.LookRotation(toCamera);

        // Find yaw difference between current body forward and target forward
        float yawToCamera = Vector3.SignedAngle(
            transform.forward,
            toCamera,
            Vector3.up
        );

        // 1) Head takes first part
        float headYaw = Mathf.Clamp(yawToCamera, -headMaxYaw, headMaxYaw);

        // 2) Spine takes remaining part
        float remainingAfterHead = yawToCamera - headYaw;
        float spineYaw = Mathf.Clamp(remainingAfterHead, -spineMaxYaw, spineMaxYaw);

        // 3) Body takes whatever is left
        float bodyYaw = remainingAfterHead - spineYaw;

        // Apply local rotations to bones
        Quaternion targetHeadLocal =
            headStartLocalRot * Quaternion.Euler(0f, headYaw, 0f);

        Quaternion targetSpineLocal =
            spineStartLocalRot * Quaternion.Euler(0f, spineYaw, 0f);

        head.localRotation = Quaternion.Slerp(
            head.localRotation,
            targetHeadLocal,
            rotationSpeed * Time.deltaTime
        );

        spine.localRotation = Quaternion.Slerp(
            spine.localRotation,
            targetSpineLocal,
            rotationSpeed * Time.deltaTime
        );

        // Rotate body in world space by leftover amount
        Quaternion bodyStep = Quaternion.Euler(0f, bodyYaw, 0f) * transform.rotation;

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            bodyStep,
            rotationSpeed * Time.deltaTime
        );
    }
}