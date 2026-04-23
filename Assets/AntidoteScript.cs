using UnityEngine;
public class AntidoteScript : MonoBehaviour
{
    [Header("Animation")]
    public Animator animator;
    public string animationName = "GiveAntidote";
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
            animator.SetBool("GiveAntidoteBool", true);
        }
        else
        {
            Debug.LogWarning("No Animator assigned!");
        }
    }
}
