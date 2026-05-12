using UnityEngine;

public class ToolSocketTrigger : MonoBehaviour
{
    public ToolType acceptedTool;

    [Header("Completion")]
    public bool completeOnTriggerEnter = true;

    private void OnTriggerEnter(Collider other)
    {
        if (!completeOnTriggerEnter)
        {
            return;
        }

        ToolIdentity tool = other.GetComponentInParent<ToolIdentity>();

        if (tool == null)
        {
            return;
        }

        if (tool.toolType != acceptedTool)
        {
            Debug.Log("Wrong tool in socket. Expected: " + acceptedTool + ", got: " + tool.toolType);
            return;
        }

        if (MainScript.Instance != null)
        {
            MainScript.Instance.TryCompleteTool(acceptedTool);
        }
    }
}