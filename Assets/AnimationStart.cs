using UnityEngine;

public class AnimationStart : MonoBehaviour
{
    [Header("Animation")]
    public Animator animator;
    public string animationName = "BloodDraw";

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
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
}