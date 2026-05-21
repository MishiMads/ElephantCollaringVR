using Meta.WitAi.TTS.Utilities;
using UnityEngine;
using Whisper.Samples;

public enum ToolType
{
    None,
    Stick,
    Machete,
    Collar,
    Lineal,
    MedKit,
    ReversalDrug,
    SprayCan,
    Stethoscope,
    BloodDraw,
    WaterBucket
}

public class MainScript : MonoBehaviour
{
    public static MainScript Instance;

    [Header("Sequence Status")]
    public bool stickInserted = false;
    public bool collarOn = false;

    public bool macheteUsed = false;
    public bool isSprayed = false;
    public bool isHealed = false;
    public bool bloodDrawn = false;
    public bool heartChecked = false;
    public bool footMeasured = false;
    public bool elephantCooled = false;

    public bool reversalDrugAdministered = false;

    [Header("Texture Variants")]
    public Renderer elephantRenderer;

    public Texture2D texBaseInjury;
    public Texture2D texInjuryAndSpray;
    public Texture2D texHealedAndSpray;
    public Texture2D texHealedNoSpray;

    [Header("Sockets")]
    public GameObject bloddrawSocket;
    public GameObject collarSocket;
    public GameObject linealSocket;
    public GameObject macheteSocket;
    public GameObject medKitSocket;
    public GameObject reversalDrugSocket;
    public GameObject sprayCanSocket;
    public GameObject stethoscopeSocket;
    public GameObject stickSocket;
    public GameObject waterBucketSocket;

    [Header("Visual Settings")]
    public bool onlyShowAllowedCurrentStep = true;

    [Header("NPC Guidance")]
    public bool useNpcGuidance = true;
    public float guidanceCooldown = 2.0f;

    [Header("Conversation")]
    public ConversationManager conversationManager;

    private bool procedureCompleted = false;
    private bool guidanceStarted = false;
    private bool reversalInstructionGiven = false;
    private float lastGuidanceTime = -999f;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        HideAllSocketVisuals();
    }

    private void Update()
    {
        if (AllTasksCompleted() && !procedureCompleted)
        {
            CompleteProcedure();
        }
    }

    public bool AllTasksCompleted()
    {
        return PreReversalTasksCompleted() && reversalDrugAdministered;
    }

    public bool PreReversalTasksCompleted()
    {
        return stickInserted &&
               collarOn &&
               macheteUsed &&
               isSprayed &&
               isHealed &&
               bloodDrawn &&
               heartChecked &&
               footMeasured &&
               elephantCooled;
    }

    // Call this when the eyelids have fully opened again.
    public void StartProcedureGuidance()
    {
        if (guidanceStarted)
        {
            return;
        }

        guidanceStarted = true;
        SpeakGuidance("Start by picking up the branch.", true);
    }

    private void CompleteProcedure()
    {
        if (procedureCompleted)
        {
            return;
        }

        procedureCompleted = true;

        SpeakGuidance("You have now completed the procedure, but you can still ask me anything.", true);

        Debug.Log("Full procedure completed.");
    }

    // ----------------------------------------------------
    // SOCKET VISUAL LOGIC
    // ----------------------------------------------------

    public void OnToolGrabbed(ToolType grabbedTool)
    {
        HideAllSocketVisuals();

        if (grabbedTool == ToolType.None)
        {
            return;
        }

        if (onlyShowAllowedCurrentStep && !IsToolAllowedNow(grabbedTool))
        {
            SpeakWrongToolGuidance(grabbedTool);
            Debug.Log("This tool is not allowed right now: " + grabbedTool);
            return;
        }

        GameObject socket = GetSocketForTool(grabbedTool);

        if (socket == null)
        {
            Debug.LogWarning("No socket has been assigned for: " + grabbedTool);
            return;
        }

        SetSocketVisual(socket, true);
    }

    public void OnToolReleased()
    {
        HideAllSocketVisuals();
    }

    private GameObject GetSocketForTool(ToolType toolType)
    {
        switch (toolType)
        {
            case ToolType.BloodDraw:
                return bloddrawSocket;

            case ToolType.Collar:
                return collarSocket;

            case ToolType.Lineal:
                return linealSocket;

            case ToolType.Machete:
                return macheteSocket;

            case ToolType.MedKit:
                return medKitSocket;

            case ToolType.ReversalDrug:
                return reversalDrugSocket;

            case ToolType.SprayCan:
                return sprayCanSocket;

            case ToolType.Stethoscope:
                return stethoscopeSocket;

            case ToolType.Stick:
                return stickSocket;

            case ToolType.WaterBucket:
                return waterBucketSocket;

            default:
                return null;
        }
    }

    private void HideAllSocketVisuals()
    {
        SetSocketVisual(bloddrawSocket, false);
        SetSocketVisual(collarSocket, false);
        SetSocketVisual(linealSocket, false);
        SetSocketVisual(macheteSocket, false);
        SetSocketVisual(medKitSocket, false);
        SetSocketVisual(reversalDrugSocket, false);
        SetSocketVisual(sprayCanSocket, false);
        SetSocketVisual(stethoscopeSocket, false);
        SetSocketVisual(stickSocket, false);
        SetSocketVisual(waterBucketSocket, false);
    }

    private void SetSocketVisual(GameObject socket, bool visible)
    {
        if (socket == null)
        {
            return;
        }

        Transform visuals = socket.transform.Find("Visuals");

        if (visuals == null)
        {
            Debug.LogWarning(socket.name + " does not have a child named Visuals.");
            return;
        }

        visuals.gameObject.SetActive(visible);
    }

    public bool IsToolAllowedNow(ToolType toolType)
    {
        switch (toolType)
        {
            // STEP 1: Stick first
            case ToolType.Stick:
                return !stickInserted;

            // STEP 2: Stethoscope second
            case ToolType.Stethoscope:
                return stickInserted && !heartChecked;

            // STEP 3: Machete third
            case ToolType.Machete:
                return heartChecked && !macheteUsed;

            // COLLAR → now part of "everything else"
            case ToolType.Collar:
                return macheteUsed && !collarOn;

            // EVERYTHING ELSE (any order AFTER machete)
            case ToolType.SprayCan:
                return macheteUsed && !isSprayed;

            case ToolType.MedKit:
                return macheteUsed && !isHealed;

            case ToolType.BloodDraw:
                return macheteUsed && !bloodDrawn;

            case ToolType.Lineal:
                return macheteUsed && !footMeasured;

            case ToolType.WaterBucket:
                return macheteUsed && !elephantCooled;

            // FINAL STEP
            case ToolType.ReversalDrug:
                return PreReversalTasksCompleted() && !reversalDrugAdministered;

            default:
                return false;
        }
    }

    // ----------------------------------------------------
    // TOOL COMPLETION LOGIC
    // ----------------------------------------------------

    public bool TryCompleteTool(ToolType toolType)
    {
        if (!IsToolAllowedNow(toolType))
        {
            SpeakWrongToolGuidance(toolType);
            Debug.LogWarning("Cannot complete tool right now: " + toolType);
            return false;
        }

        switch (toolType)
        {
            case ToolType.Stick:
                SetStickInserted();
                break;

            case ToolType.Collar:
                SetCollarSwapped();
                break;

            case ToolType.Machete:
                SetMacheteUsed();
                break;

            case ToolType.SprayCan:
                SetSprayed();
                break;

            case ToolType.MedKit:
                SetHealed();
                break;

            case ToolType.BloodDraw:
                SetBloodDrawn();
                break;

            case ToolType.Stethoscope:
                SetHeartChecked();
                break;

            case ToolType.Lineal:
                SetFootMeasured();
                break;

            case ToolType.WaterBucket:
                SetElephantCooled();
                break;

            case ToolType.ReversalDrug:
                AdministerReversal();
                break;

            default:
                return false;
        }

        HideAllSocketVisuals();

        if (PreReversalTasksCompleted() && !reversalDrugAdministered && !reversalInstructionGiven)
        {
            reversalInstructionGiven = true;
            SpeakGuidance("All the other tasks are complete. Now use the reversal drug.", true);
        }

        return true;
    }

    public void SetStickInserted()
    {
        if (!stickInserted)
        {
            stickInserted = true;
            Debug.Log("Step 1 complete: Stick inserted.");

            SpeakGuidance("Now check the heart rate using the stethoscope.", true);
        }
    }

    public void SetCollarSwapped()
    {
        if (macheteUsed && !collarOn)
        {
            collarOn = true;
            Debug.Log("Collar on.");
        }
        else if (!macheteUsed)
        {
            SpeakWrongToolGuidance(ToolType.Collar);
        }
    }

    public void SetMacheteUsed()
    {
        if (heartChecked && !macheteUsed)
        {
            macheteUsed = true;
            Debug.Log("Machete task complete.");

            SpeakGuidance("Now complete the remaining tasks using the other tools.", true);
        }
        else if (!heartChecked)
        {
            SpeakWrongToolGuidance(ToolType.Machete);
        }
    }

    public void SetSprayed()
    {
        if (macheteUsed && !isSprayed)
        {
            isSprayed = true;
            UpdateVisuals();
            Debug.Log("Spray complete.");
        }
        else if (!macheteUsed)
        {
            SpeakWrongToolGuidance(ToolType.SprayCan);
        }
    }

    public void SetHealed()
    {
        if (macheteUsed && !isHealed)
        {
            isHealed = true;
            UpdateVisuals();
            Debug.Log("Healing complete.");
        }
        else if (!macheteUsed)
        {
            SpeakWrongToolGuidance(ToolType.MedKit);
        }
    }

    public void SetBloodDrawn()
    {
        if (macheteUsed && !bloodDrawn)
        {
            bloodDrawn = true;
            Debug.Log("Blood drawn.");
        }
        else if (!macheteUsed)
        {
            SpeakWrongToolGuidance(ToolType.BloodDraw);
        }
    }

    public void SetHeartChecked()
    {
        if (stickInserted && !heartChecked)
        {
            heartChecked = true;
            Debug.Log("Heart checked.");

            SpeakGuidance("Now use the machete to cut the tree.", true);
        }
        else if (!stickInserted)
        {
            SpeakWrongToolGuidance(ToolType.Stethoscope);
        }
    }

    public void SetFootMeasured()
    {
        if (macheteUsed && !footMeasured)
        {
            footMeasured = true;
            Debug.Log("Foot measured.");
        }
        else if (!macheteUsed)
        {
            SpeakWrongToolGuidance(ToolType.Lineal);
        }
    }

    public void SetElephantCooled()
    {
        if (macheteUsed && !elephantCooled)
        {
            elephantCooled = true;
            Debug.Log("Elephant cooled.");
        }
        else if (!macheteUsed)
        {
            SpeakWrongToolGuidance(ToolType.WaterBucket);
        }
    }

    public void AdministerReversal()
    {
        if (PreReversalTasksCompleted() && !reversalDrugAdministered)
        {
            reversalDrugAdministered = true;
            Debug.Log("Final step complete: Reversal drug administered.");
            CompleteProcedure();
        }
        else if (!PreReversalTasksCompleted())
        {
            SpeakGuidance("The procedure is not complete yet. Finish the other tasks before using the reversal drug.", true);
        }
    }

    // ----------------------------------------------------
    // NPC SPEECH LOGIC
    // ----------------------------------------------------

    private void SpeakWrongToolGuidance(ToolType grabbedTool)
    {
        if (!conversationManager.procedureStarted)
            return;

        // STEP 1: Stick
        if (!stickInserted)
        {
            SpeakGuidance("Start by picking up the branch.", false);
            return;
        }

        // STEP 2: Stethoscope
        if (!heartChecked)
        {
            SpeakGuidance("Now check the heart rate using the stethoscope.", false);
            return;
        }

        // STEP 3: Machete
        if (!macheteUsed)
        {
            SpeakGuidance("Now use the machete to cut the tree.", false);
            return;
        }

        // FINAL STEP BLOCK
        if (grabbedTool == ToolType.ReversalDrug && !PreReversalTasksCompleted())
        {
            SpeakGuidance("Use the other tools first. The reversal drug is last.", false);
            return;
        }

        // EVERYTHING ELSE
        SpeakGuidance("Use one of the remaining tools.", false);
    }   

    private void SpeakGuidance(string text, bool force)
    {
        if (!useNpcGuidance)
        {
            return;
        }

        if (conversationManager == null)
        {
            Debug.LogWarning("ConversationManager is not assigned. NPC cannot speak: " + text);
            return;
        }

        if (!force && Time.time - lastGuidanceTime < guidanceCooldown)
        {
            return;
        }

        lastGuidanceTime = Time.time;
        conversationManager.SafeSpeak(text);
    }

    // ----------------------------------------------------
    // TEXTURE LOGIC
    // ----------------------------------------------------

    private void UpdateVisuals()
    {
        Texture2D selectedTex = texBaseInjury;

        if (!isHealed && isSprayed)
        {
            selectedTex = texInjuryAndSpray;
        }
        else if (isHealed && isSprayed)
        {
            selectedTex = texHealedAndSpray;
        }
        else if (isHealed && !isSprayed)
        {
            selectedTex = texHealedNoSpray;
        }

        if (elephantRenderer != null && selectedTex != null)
        {
            string prop = elephantRenderer.material.HasProperty("_BaseMap") ? "_BaseMap" : "_MainTex";
            elephantRenderer.material.SetTexture(prop, selectedTex);
        }
    }
}