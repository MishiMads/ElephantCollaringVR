using System.Collections;
using UnityEngine;

public class ElephantMove : MonoBehaviour
{
    public float speed = 1f;
    public GameObject target;
    public MainScript mainManager;

    private bool isMoving = false;
    private Animator anim;

    void Awake()
    {
        anim = GetComponent<Animator>();
    }

    public void InitiateMovement()
    {
        if (target == null) return;
        StartCoroutine(WaitAndStart());
    }

    private IEnumerator WaitAndStart()
    {
        yield return new WaitForSeconds(3f);
        isMoving = true;
        if (anim != null) anim.SetBool("Walking", true);
    }

    void Update()
    {
        if (!isMoving) return;

        // 1. Position Movement
        transform.position = Vector3.MoveTowards(transform.position, target.transform.position, speed * Time.deltaTime);

        // 2. Anti-Tilt Rotation Logic
        Vector3 direction = (target.transform.position - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            // By setting direction.y to 0, we force the elephant to stay level on the horizon
            direction.y = 0;
            Quaternion lookRot = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * 5f);
        }

        // 3. Arrival Check
        if (Vector3.Distance(transform.position, target.transform.position) < 0.25f)
        {
            StopAndLock();
        }
    }

    private void StopAndLock()
    {
        isMoving = false;

        // Snap to exact target position
        transform.position = target.transform.position;

        // Force the rotation to be perfectly upright (Y-axis only)
        Vector3 finalForward = transform.forward;
        finalForward.y = 0;
        transform.rotation = Quaternion.LookRotation(finalForward, Vector3.up);

        if (anim != null)
        {
            anim.SetBool("Walking", false);
            // Play Idle immediately to stop any "transition" movement
            anim.Play("Idle", 0, 0f);
        }

        if (mainManager != null) mainManager.InitiateNPCs();
    }
}