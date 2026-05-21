using UnityEngine;
using System.Collections.Generic;

public class ToolResetManager : MonoBehaviour
{
    [Header("Assign tools manually (order = keys 1–0)")]
    public List<Transform> tools = new List<Transform>();

    private Vector3[] startPositions;
    private Quaternion[] startRotations;

    void Start()
    {
        int count = tools.Count;

        startPositions = new Vector3[count];
        startRotations = new Quaternion[count];

        for (int i = 0; i < count; i++)
        {
            startPositions[i] = tools[i].position;
            startRotations[i] = tools[i].rotation;
        }
    }

    void Update()
    {
        // Keys 1–9
        for (int i = 0; i < tools.Count && i < 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                ResetTool(i);
            }
        }

        // Key 0 = index 9
        if (tools.Count > 9 && Input.GetKeyDown(KeyCode.Alpha0))
        {
            ResetTool(9);
        }
    }

    void ResetTool(int index)
    {
        Transform tool = tools[index];

        // Re-parent FIRST
        tool.SetParent(transform);

        // Reset position & rotation
        tool.position = startPositions[index];
        tool.rotation = startRotations[index];

        // Reset physics
        Rigidbody rb = tool.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            rb.isKinematic = false;
            rb.detectCollisions = true;
        }

        // Re-enable grabbing
        var grabbable = tool.GetComponentInChildren<Oculus.Interaction.Grabbable>();
        if (grabbable != null)
        {
            grabbable.enabled = true;
        }

        Debug.Log($"Reset tool: {tool.name}");
    }
}