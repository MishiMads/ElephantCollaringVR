using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction;

public class MainScript : MonoBehaviour
{
    [Header("Snap Logic")]
    public List<SnapInteractable> managedSnapPoints;

    [Header("Elephant Logic")]
    public ElephantMove elephantScript; // Reference to the actual script

    [Header("NPC Logic")]
    public List<NPCBehaviour> npcScripts; // List of NPC scripts

    private void Start()
    {
        // ---------------------------------------------------------
        // INITIATE ELEPHANT SCRIPT
        // ---------------------------------------------------------
        if (elephantScript != null)
        {
            elephantScript.InitiateMovement();
        }
    }

    // ---------------------------------------------------------
    // SNAP EVENT LOGIC
    // ---------------------------------------------------------
    private void OnEnable()
    {
        foreach (var snapPoint in managedSnapPoints)
        {
            if (snapPoint != null)
                snapPoint.WhenStateChanged += (args) => HandleStateChanged(snapPoint, args);
        }
    }

    private void OnDisable()
    {
        foreach (var snapPoint in managedSnapPoints)
        {
            if (snapPoint != null)
                snapPoint.WhenStateChanged -= (args) => HandleStateChanged(snapPoint, args);
        }
    }

    private void HandleStateChanged(SnapInteractable snapPoint, InteractableStateChangeArgs args)
    {
        if (args.NewState == InteractableState.Select)
        {
            Debug.Log($"<color=green>Snapped:</color> {snapPoint.gameObject.name}");
        }
    }

    // ---------------------------------------------------------
    // NPC INITIATION LOGIC
    // This is called by the ElephantMove script when it arrives
    // ---------------------------------------------------------
    public void InitiateNPCs()
    {
        Debug.Log("MainScript: Elephant arrived. Starting NPCs...");
        foreach (var npc in npcScripts)
        {
            if (npc != null)
            {
                npc.StartNPCLogic();
            }
        }
    }
}