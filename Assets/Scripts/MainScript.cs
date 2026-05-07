using Meta.WitAi.TTS.Utilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Whisper.Samples;

public class MainScript : MonoBehaviour
{
    [Header("Sequence Status")]
    public bool stickInserted = false;    // Task 1: Must be first
    public bool collarOn = false;         // Task 2: Must be second
    public bool isSprayed = false;
    public bool isHealed = false;
    public bool bloodDrawn = false;
    public bool heartChecked = false;
    public bool footMeasured = false;     
    public bool elephantCooled = false;   
    
    [Header("Texture Variants")]
    public Renderer elephantRenderer;

    public Texture2D texBaseInjury;
    public Texture2D texInjuryAndSpray;
    public Texture2D texHealedAndSpray;
    public Texture2D texHealedNoSpray;

    [Header("Trigger Zones")]
    public List<GameObject> triggerZones;

    public static MainScript Instance;

    public ConversationManager conversationManager;

    private bool procedureCompleted = false;
    public bool AllTasksCompleted()
    {
        return stickInserted && collarOn && isSprayed && isHealed &&
               bloodDrawn && heartChecked && footMeasured && elephantCooled;

    }

    private void Awake()
    {
        Instance = this;
    }

    public void Update()
    {
        if (AllTasksCompleted() && !procedureCompleted)
        {
            procedureCompleted = true;
            conversationManager.SafeSpeak("You have now completed the procedure, but you can still ask me anything.");
        }
    }

    // --- STEP 1: THE STICK (MANDATORY FIRST) ---
    public void SetStickInserted()
    {
        stickInserted = true;
        Debug.Log("Step 1 Complete: Airway secured.");
    }

    // --- STEP 2: THE COLLAR (MANDATORY AFTER STICK) ---
    public void SetCollarSwapped()
    {
        if (stickInserted)
        {
            collarOn = true;
            Debug.Log("Step 2 Complete: Collar on.");
        }
        else
        {
            Debug.LogWarning("Sequence Error: Secure airway with stick first.");
        }
    }

    // --- OPTIONAL TASKS (ONLY ACCESSIBLE AFTER COLLAR) ---
    public void SetSprayed()
    {
        if (collarOn) { isSprayed = true; UpdateVisuals(); }
    }

    public void SetHealed()
    {
        if (collarOn) { isHealed = true; UpdateVisuals(); }
    }

    public void SetBloodDrawn()
    {
        if (collarOn) bloodDrawn = true;
    }

    public void SetHeartChecked()
    {
        if (collarOn) heartChecked = true;
    }

    public void SetFootMeasured()
    {
        if (collarOn) footMeasured = true;
    }

    public void SetElephantCooled()
    {
        if (collarOn) elephantCooled = true;
    }

    // --- FINAL STEP: REVERSAL DRUG (MANDATORY END) ---
    public void AdministerReversal()
    {
        // Validates all tasks before allowing the procedure to end
        if (stickInserted && collarOn && isSprayed && isHealed &&
            bloodDrawn && heartChecked && footMeasured && elephantCooled)
        {
            Debug.Log("Procedure Successful. Reversal drug administered.");
            // Trigger waking logic here
        }
        else
        {
            Debug.Log("Procedure Incomplete. Cannot wake elephant yet.");
        }
    }

    private void UpdateVisuals()
    {
        Texture2D selectedTex = texBaseInjury;

        if (!isHealed && isSprayed) selectedTex = texInjuryAndSpray;
        else if (isHealed && isSprayed) selectedTex = texHealedAndSpray;
        else if (isHealed && !isSprayed) selectedTex = texHealedNoSpray;

        if (elephantRenderer != null)
        {
            string prop = elephantRenderer.material.HasProperty("_BaseMap") ? "_BaseMap" : "_MainTex";
            elephantRenderer.material.SetTexture(prop, selectedTex);
        }
    }
}