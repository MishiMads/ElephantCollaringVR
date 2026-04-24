using UnityEngine;

public class CollarSwap : MonoBehaviour
{
    public GameObject collarToEnable;
    public GameObject collarToDisable;

    public void MakeSwap()
    {
        if (collarToEnable != null) collarToEnable.SetActive(true);
        if (collarToDisable != null) collarToDisable.SetActive(false);
    }
}