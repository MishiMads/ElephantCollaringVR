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

    [Header("Conversation")]
    public ConversationManager conversationManager;

    private bool procedureCompleted = false;

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

    // This means the whole procedure is done, including the reversal drug.
    public bool AllTasksCompleted()
    {
        return PreReversalTasksCompleted() && reversalDrugAdministered;
    }

    // This means everything before the reversal drug is done.
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

    private void CompleteProcedure()
    {
        procedureCompleted = true;

        if (conversationManager != null)
        {
            conversationManager.SafeSpeak("You have now completed the procedure, but you can still ask me anything.");
        }

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

        // Do not use socket.SetActive(false).
        // That would turn off the trigger collider.
        // This only hides the visual parts.

        Renderer[] renderers = socket.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = visible;
        }

        Canvas[] canvases = socket.GetComponentsInChildren<Canvas>(true);
        foreach (Canvas canvas in canvases)
        {
            canvas.enabled = visible;
        }

        ParticleSystem[] particles = socket.GetComponentsInChildren<ParticleSystem>(true);
        foreach (ParticleSystem particle in particles)
        {
            if (visible)
            {
                particle.Play();
            }
            else
            {
                particle.Stop();
            }
        }
    }

    private bool IsToolAllowedNow(ToolType toolType)
    {
        switch (toolType)
        {
            // First mandatory tool.
            case ToolType.Stick:
                return !stickInserted;

            // Second mandatory tool.
            case ToolType.Collar:
                return stickInserted && !collarOn;

            // These can be used in any order after the collar.
            case ToolType.Machete:
                return collarOn && !macheteUsed;

            case ToolType.SprayCan:
                return collarOn && !isSprayed;

            case ToolType.MedKit:
                return collarOn && !isHealed;

            case ToolType.BloodDraw:
                return collarOn && !bloodDrawn;

            case ToolType.Stethoscope:
                return collarOn && !heartChecked;

            case ToolType.Lineal:
                return collarOn && !footMeasured;

            case ToolType.WaterBucket:
                return collarOn && !elephantCooled;

            // Last mandatory tool.
            case ToolType.ReversalDrug:
                return PreReversalTasksCompleted() && !reversalDrugAdministered;

            default:
                return false;
        }
    }

    // ----------------------------------------------------
    // PROCEDURE LOGIC
    // ----------------------------------------------------

    public void SetStickInserted()
    {
        if (!stickInserted)
        {
            stickInserted = true;
            Debug.Log("Step 1 complete: Stick inserted.");
        }
    }

    public void SetCollarSwapped()
    {
        if (stickInserted)
        {
            if (!collarOn)
            {
                collarOn = true;
                Debug.Log("Step 2 complete: Collar on.");
            }
        }
        else
        {
            Debug.LogWarning("Sequence Error: Insert the stick first.");
        }
    }

    public void SetMacheteUsed()
    {
        if (collarOn)
        {
            if (!macheteUsed)
            {
                macheteUsed = true;
                Debug.Log("Machete task complete.");
            }
        }
        else
        {
            Debug.LogWarning("Cannot use machete yet. Put the collar on first.");
        }
    }

    public void SetSprayed()
    {
        if (collarOn)
        {
            if (!isSprayed)
            {
                isSprayed = true;
                UpdateVisuals();
                Debug.Log("Spray complete.");
            }
        }
        else
        {
            Debug.LogWarning("Cannot spray yet. Put the collar on first.");
        }
    }

    public void SetHealed()
    {
        if (collarOn)
        {
            if (!isHealed)
            {
                isHealed = true;
                UpdateVisuals();
                Debug.Log("Healing complete.");
            }
        }
        else
        {
            Debug.LogWarning("Cannot heal yet. Put the collar on first.");
        }
    }

    public void SetBloodDrawn()
    {
        if (collarOn)
        {
            if (!bloodDrawn)
            {
                bloodDrawn = true;
                Debug.Log("Blood drawn.");
            }
        }
        else
        {
            Debug.LogWarning("Cannot draw blood yet. Put the collar on first.");
        }
    }

    public void SetHeartChecked()
    {
        if (collarOn)
        {
            if (!heartChecked)
            {
                heartChecked = true;
                Debug.Log("Heart checked.");
            }
        }
        else
        {
            Debug.LogWarning("Cannot check heart yet. Put the collar on first.");
        }
    }

    public void SetFootMeasured()
    {
        if (collarOn)
        {
            if (!footMeasured)
            {
                footMeasured = true;
                Debug.Log("Foot measured.");
            }
        }
        else
        {
            Debug.LogWarning("Cannot measure foot yet. Put the collar on first.");
        }
    }

    public void SetElephantCooled()
    {
        if (collarOn)
        {
            if (!elephantCooled)
            {
                elephantCooled = true;
                Debug.Log("Elephant cooled.");
            }
        }
        else
        {
            Debug.LogWarning("Cannot cool elephant yet. Put the collar on first.");
        }
    }

    public void AdministerReversal()
    {
        if (PreReversalTasksCompleted())
        {
            if (!reversalDrugAdministered)
            {
                reversalDrugAdministered = true;
                Debug.Log("Final step complete: Reversal drug administered.");
                CompleteProcedure();
            }
        }
        else
        {
            Debug.LogWarning("Procedure incomplete. Cannot use reversal drug yet.");

            if (conversationManager != null)
            {
                conversationManager.SafeSpeak("The procedure is not complete yet.");
            }
        }
    }

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