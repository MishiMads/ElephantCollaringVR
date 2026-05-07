using UnityEngine;
using Oculus.Interaction;

public class SyringeScript : MonoBehaviour
{
    [Header("Animation")]
    public Animator animator;
    public string animationName = "BloodDraw";

    [Header("Grab Detection")]
    private Grabbable grabbable;
    private bool isGrabbed = false;

    void Start()
    {
        // Get the Grabbable component (Meta SDK)
        grabbable = GetComponent<Grabbable>();
        
        if (grabbable != null)
        {
            // Listen for grab/release events
            grabbable.WhenPointerEventRaised += OnPointerEvent;
        }
        else
        {
            Debug.LogWarning("No Grabbable found on syringe!");
        }
    }

    void OnPointerEvent(PointerEvent pointerEvent)
    {
        if (pointerEvent.Type == PointerEventType.Select)
        {
            isGrabbed = true;
            Debug.Log("Syringe grabbed");
        }
        else if (pointerEvent.Type == PointerEventType.Unselect)
        {
            isGrabbed = false;
            Debug.Log("Syringe released");
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            PlayAnimation();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Only play animation if syringe is grabbed AND touching elephant
        if (isGrabbed && other.CompareTag("Target"))
        {
            PlayAnimation();
        }
    }

    void PlayAnimation()
    {
        if (animator != null)
        {
            Debug.Log("Playing animation: " + animationName);
            animator.SetBool("BloodDrawBool", true);
        }
        else
        {
            Debug.LogWarning("No Animator assigned!");
        }
    }

    void OnDestroy()
    {
        // Clean up listeners
        if (grabbable != null)
        {
            grabbable.WhenPointerEventRaised -= OnPointerEvent;
        }
    }
}