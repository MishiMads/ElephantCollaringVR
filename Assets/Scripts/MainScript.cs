using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class MainScript : MonoBehaviour
{
    [Header("Target Sockets")]
    public List<UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor> managedSockets;

    void OnEnable()
    {
        foreach (var socket in managedSockets)
        {
            if (socket != null)
            {
                // Subscribe to events
                socket.selectEntered.AddListener(OnObjectSocketed);
                socket.selectExited.AddListener(OnObjectUnsocketed);
            }
        }
    }

    void OnDisable()
    {
        foreach (var socket in managedSockets)
        {
            if (socket != null)
            {
                // Always unsubscribe to prevent memory leaks
                socket.selectEntered.RemoveListener(OnObjectSocketed);
                socket.selectExited.RemoveListener(OnObjectUnsocketed);
            }
        }
    }

    private void OnObjectSocketed(SelectEnterEventArgs args)
    {
        string itemName = args.interactableObject.transform.name;
        string socketName = args.interactorObject.transform.name;
        Debug.Log($"<color=green>Logic Manager:</color> {itemName} attached to {socketName}");
    }

    private void OnObjectUnsocketed(SelectExitEventArgs args)
    {
        string itemName = args.interactableObject.transform.name;
        Debug.Log($"<color=red>Logic Manager:</color> {itemName} removed from the socket");
    }
}