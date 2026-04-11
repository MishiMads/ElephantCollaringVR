using UnityEngine;
using UnityEngine.AI;

public class NPCBehaviour : MonoBehaviour
{
    public enum NPCRole { Director, Assistant1, Assistant2 }
    public NPCRole role;
    public string targetTag;

    private NavMeshAgent agent;
    private Animator anim;
    private bool hasArrived = false;
    private bool isActive = false;
    private GameObject targetObject;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        targetObject = GameObject.FindGameObjectWithTag(targetTag);

        if (agent != null) agent.enabled = false;
    }

    public void StartNPCLogic()
    {
        if (targetObject != null && agent != null)
        {
            isActive = true;
            agent.enabled = true;
            agent.SetDestination(targetObject.transform.position);

            if (anim != null)
            {
                // Start running
                anim.SetBool("SlowRunning", true);
            }
        }
    }

    void Update()
    {
        if (!isActive || hasArrived) return;

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
        FaceTarget();

        if (anim != null)
        {
            // Turn off running
            anim.SetBool("SlowRunning", false);

            // Trigger the final Idle/Crouch based on role
            if (role == NPCRole.Director)
            {
                anim.SetBool("StandToCrouch", true);
            }
            else
            {
                // Assistant role
                anim.SetBool("NormalIdle", true);
            }
        }
    }

    void FaceTarget()
    {
        if (targetObject != null)
        {
            Vector3 direction = (targetObject.transform.position - transform.position).normalized;
            direction.y = 0;
            if (direction != Vector3.zero) transform.rotation = Quaternion.LookRotation(direction);
        }
    }
}