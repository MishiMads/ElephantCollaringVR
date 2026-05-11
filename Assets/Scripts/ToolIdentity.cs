using UnityEngine;

public class ToolIdentity : MonoBehaviour
{
    public ToolType toolType;

    public void ToolGrabbed()
    {
        Debug.Log("Tool grabbed: " + toolType);

        if (MainScript.Instance != null)
        {
            MainScript.Instance.OnToolGrabbed(toolType);
        }
    }

    public void ToolReleased()
    {
        Debug.Log("Tool released: " + toolType);

        if (MainScript.Instance != null)
        {
            MainScript.Instance.OnToolReleased();
        }
    }
}