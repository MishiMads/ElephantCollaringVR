using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.Surfaces;

public class SnapDebugTester : MonoBehaviour
{
    public SnapInteractor snapInteractor;

    [InspectorButton("ForceSnap")]
    public string forceSnap;

    void Awake()
    {
        if (snapInteractor != null) snapInteractor.enabled = false;
    }

    public void ForceSnap()
    {
        snapInteractor.enabled = true;
        Physics.SyncTransforms();

        SnapInteractable socket = Object.FindAnyObjectByType<SnapInteractable>();
        if (socket == null) return;

        // 1. Force the SDK to recognize the socket as the "Selected" target
        // We skip the 'Candidate' phase and go straight to 'Selection'
        try
        {
            snapInteractor.SetComputeCandidateOverride(() => socket);
            snapInteractor.Preprocess();
            snapInteractor.Process();

            // Manual check: Is the Interactor's PointableElement actually set?
            if (snapInteractor.PointableElement == null)
            {
                Debug.LogError("CRITICAL: The SnapInteractor on the Machete has NO Pointable Element linked in the inspector!");
                return;
            }

            snapInteractor.Select();
            Debug.Log("<color=green>FORCE SELECT CALLED</color> - Check the Scene View!");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Snap failed with error: " + e.Message);
        }
    }
}