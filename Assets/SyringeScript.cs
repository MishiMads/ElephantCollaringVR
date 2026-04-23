using UnityEngine;

public class SyringeScript : MonoBehaviour
{
    [Header("Animation")]
    public Animator animator;
    public string animationName = "BloodDraw";

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            PlayAnimation();
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Elephant"))
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
}

