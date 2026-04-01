using UnityEngine;
using UnityEngine.AI;

public class NPCBehaviour : MonoBehaviour
{
    private NavMeshAgent agent;
    private Animator anim;
    private bool hasArrived = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();

        // Start by running
        anim.SetBool("SlowRunning", true);

        GameObject target = GameObject.FindGameObjectWithTag("Target");
        if (target != null)
        {
            agent.SetDestination(target.transform.position);
        }
    }

    void Update()
    {

        if (hasArrived) return;

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
            {
                TriggerCrouchSequence();
            }
        }
    }

    void TriggerCrouchSequence()
    {
        hasArrived = true;
        agent.isStopped = true;

        // Turn off running, turn on the transition to crouch
        anim.SetBool("SlowRunning", false);
        anim.SetBool("StandToCrouch", true);

        // This triggers the final idle state
        anim.SetBool("CrouchToIdle", true);
    }
}