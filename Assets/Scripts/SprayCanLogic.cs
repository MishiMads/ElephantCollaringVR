using UnityEngine;

public class SprayCanLogic : MonoBehaviour
{
    public void ApplySpray()
    {
        if (MainScript.Instance != null)
        {
            MainScript.Instance.SetSprayed();
        }
        gameObject.SetActive(false);
    }
}