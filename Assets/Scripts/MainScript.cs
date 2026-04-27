using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction;

public class MainScript : MonoBehaviour
{
    [Header("Elephant State")]
    public Renderer elephantRenderer;
    private bool _isSprayed = false;
    private bool _isHealed = false;

    [Header("Texture Variants")]
    public Texture2D texBaseInjury;      // Texture 1
    public Texture2D texInjuryAndSpray;  // Texture 2
    public Texture2D texHealedAndSpray;  // Texture 3
    public Texture2D texHealedNoSpray;   // Texture 4

    [Header("Snap Logic")]
    public List<SnapInteractable> managedSnapPoints;

    [Header("Elephant Logic")]
    public ElephantMove elephantScript; // Reference to the actual script

    [Header("NPC Logic")]
    public List<NPCBehaviour> npcScripts; // List of NPC scripts

    public static MainScript Instance;

    private void Awake()
    {
        Instance = this;
    }

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

    // Called by SprayCanAction
    public void SetSprayed()
    {
        _isSprayed = true;
        UpdateVisuals();
    }

    // Called by MedkitTextureSwap
    public void SetHealed()
    {
        _isHealed = true;
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        Texture2D selectedTex = texBaseInjury;

        if (!_isHealed && !_isSprayed) selectedTex = texBaseInjury;
        else if (!_isHealed && _isSprayed) selectedTex = texInjuryAndSpray;
        else if (_isHealed && _isSprayed) selectedTex = texHealedAndSpray;
        else if (_isHealed && !_isSprayed) selectedTex = texHealedNoSpray;

        if (elephantRenderer != null)
        {
            string prop = elephantRenderer.material.HasProperty("_BaseMap") ? "_BaseMap" : "_MainTex";
            elephantRenderer.material.SetTexture(prop, selectedTex);
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