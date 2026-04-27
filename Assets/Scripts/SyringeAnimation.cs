using UnityEngine;

public class SyringeAnimation : MonoBehaviour
{
    [Header("Animation Settings")]
    public Animator syringeAnimator;
    public string boolParameterName = "BloodDrawBool";

    private bool hasPlayed = false;

    public void PlayBloodAnimation()
    {
        // Use the Bool from your screenshot
        if (syringeAnimator != null && !hasPlayed)
        {
            syringeAnimator.SetBool(boolParameterName, true);
            hasPlayed = true;
        }
    }

    public void ResetSyringe()
    {
        if (syringeAnimator != null)
        {
            syringeAnimator.SetBool(boolParameterName, false);
        }
        hasPlayed = false;
    }
}