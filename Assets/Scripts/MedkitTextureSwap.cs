using UnityEngine;

public class MedkitTextureSwap : MonoBehaviour
{
    public void ApplyHealing()
    {
        if (MainScript.Instance != null)
        {
            MainScript.Instance.SetHealed();
        }
        gameObject.SetActive(false);
    }
}