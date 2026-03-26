using UnityEngine;

public class Wobble : MonoBehaviour
{
    private Quaternion initialRotation;

    Renderer rend;
    Vector3 lastPos;
    Vector3 lastRot;
    Vector3 velocity;
    Vector3 angularVelocity;

    public float MaxWobble = 0.03f;
    public float WobbleSpeed = 1f;
    public float Recovery = 1f;
    float wobbleAmountX, wobbleAmountZ;
    float wobbleAmountToAddX, wobbleAmountToAddZ;
    float time = 0.5f;

    [Header("Pouring Settings")]
    public ParticleSystem pourParticles;
    public GameObject waterPoolPrefab; // Prefab of a flat blue cylinder or plane
    public float pourThreshold = 45f;
    public float emptySpeed = 0.2f;    // How fast the bucket empties

    float currentFill = 1f; // Starts full
    GameObject currentPool;

    void Start()
    {
        rend = GetComponent<Renderer>();
        currentFill = rend.material.GetFloat("_Fill");

        rend = GetComponent<Renderer>();

        // 2. Capture the -90 rotation as the "resting" state
        initialRotation = transform.rotation;

        // Safety check for the shader property name
        if (rend.material.HasProperty("_Fill"))
        {
            currentFill = rend.material.GetFloat("_Fill");
        }
    }

    void Update()
    {
        time += Time.deltaTime;

        // Wobble Logic
        wobbleAmountToAddX = Mathf.Lerp(wobbleAmountToAddX, 0, Time.deltaTime * Recovery);
        wobbleAmountToAddZ = Mathf.Lerp(wobbleAmountToAddZ, 0, Time.deltaTime * Recovery);
        float pulse = 2 * Mathf.PI * WobbleSpeed;
        wobbleAmountX = wobbleAmountToAddX * Mathf.Sin(pulse * time);
        wobbleAmountZ = wobbleAmountToAddZ * Mathf.Sin(pulse * time);

        rend.material.SetFloat("_WobbleX", wobbleAmountX);
        rend.material.SetFloat("_WobbleZ", wobbleAmountZ);

        velocity = (lastPos - transform.position) / Time.deltaTime;
        angularVelocity = transform.rotation.eulerAngles - lastRot;
        wobbleAmountToAddX += Mathf.Clamp((velocity.x + (angularVelocity.z * 0.2f)) * MaxWobble, -MaxWobble, MaxWobble);
        wobbleAmountToAddZ += Mathf.Clamp((velocity.z + (angularVelocity.x * 0.2f)) * MaxWobble, -MaxWobble, MaxWobble);

        lastPos = transform.position;
        lastRot = transform.rotation.eulerAngles;

        // Pouring and Emptying Logic
        // Calculate tilt relative to that -90 starting point
        float tilt = Quaternion.Angle(transform.rotation, initialRotation);

        // Now 'tilt' will be 0 when the bucket is at -90, 
        // and will increase as you tip it over.
        if (tilt > pourThreshold && currentFill > 0)
        {
            if (!pourParticles.isPlaying) pourParticles.Play();

            currentFill -= emptySpeed * Time.deltaTime;
            rend.material.SetFloat("_Fill", currentFill);

            HandleWaterPool();
        }
        else
        {
            if (pourParticles.isPlaying) pourParticles.Stop();
        }


    }

    void HandleWaterPool()
    {
        RaycastHit hit;
        // Raycast straight down in World Space
        if (Physics.Raycast(pourParticles.transform.position, Vector3.down, out hit))
        {
            if (currentPool == null)
            {
                // Spawn the pool flat on the ground (Quaternion.identity)
                currentPool = Instantiate(waterPoolPrefab, hit.point + new Vector3(0, 0.01f, 0), Quaternion.identity);
            }

            // Grow X and Z, keep Y (height) at 0
            currentPool.transform.localScale += new Vector3(0.5f, 0, 0.5f) * Time.deltaTime;
        }
    }
}