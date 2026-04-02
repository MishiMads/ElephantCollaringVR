using UnityEngine;
using UnityEngine.AI;

public class NPCBehaviour : MonoBehaviour
{
    public enum NPCRole { Director, Assistant1, Assistant2 }

    [Header("Role Settings")]
    public NPCRole role;
    public string targetTag;

    private NavMeshAgent agent;
    private Animator anim;
    private bool hasArrived = false;
    private GameObject targetObject; // Storing this to reference later

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();

        targetObject = GameObject.FindGameObjectWithTag(targetTag);
        if (targetObject != null)
        {
            agent.SetDestination(targetObject.transform.position);
            anim.SetBool("SlowRunning", true);
        }
    }

    void Update()
    {
        if (hasArrived) return;

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
            {
                OnArrival();
            }
        }
    }

    void OnArrival()
    {
        hasArrived = true;
        agent.isStopped = true;

        // Make the NPC face the target immediately upon stopping
        FaceTarget();

        anim.SetBool("SlowRunning", false);

        switch (role)
        {
            case NPCRole.Director:
                DirectorArrival();
                break;
            case NPCRole.Assistant1:
                AssistantArrival();
                break;
            case NPCRole.Assistant2:
                Assistant2Arrival();
                break;
        }
    }

    void FaceTarget()
    {
        if (targetObject != null)
        {
            Vector3 direction = (targetObject.transform.position - transform.position).normalized;
            direction.y = 0; // Keep the NPC upright so they don't tilt up/down

            if (direction != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                transform.rotation = lookRotation;
            }
        }
    }

    void DirectorArrival()
    {
        anim.SetBool("StandToCrouch", true);
        anim.SetBool("CrouchToIdle", true);
    }

    void AssistantArrival()
    {
        anim.SetBool("NormalIdle", true);
    }

    void Assistant2Arrival()
    {
        anim.SetBool("NormalIdle", true);
    }
}