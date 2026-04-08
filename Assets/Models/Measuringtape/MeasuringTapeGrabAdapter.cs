using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.Input;

public class MeasuringTapeTouchGrabBridge : MonoBehaviour
{
    public MeasuringTapeBuilder tape;

    public Transform leftHandGrabTarget;
    public Transform rightHandGrabTarget;

    public void HandleSelect(PointerEvent evt)
    {
        if (tape == null)
            return;

        HandRef handRef = GetHandRefFromEvent(evt);
        if (handRef == null)
        {
            Debug.LogWarning("HandleSelect: could not find HandRef from PointerEvent.Data");
            return;
        }

        if (handRef.Handedness == Handedness.Left)
        {
            tape.GrabLeftHand(leftHandGrabTarget);
            Debug.Log("Tape grabbed by LEFT hand");
        }
        else if (handRef.Handedness == Handedness.Right)
        {
            tape.GrabRightHand(rightHandGrabTarget);
            Debug.Log("Tape grabbed by RIGHT hand");
        }
    }

    public void HandleUnselect(PointerEvent evt)
    {
        if (tape == null)
            return;

        HandRef handRef = GetHandRefFromEvent(evt);
        if (handRef == null)
        {
            Debug.LogWarning("HandleUnselect: could not find HandRef from PointerEvent.Data");
            return;
        }

        if (handRef.Handedness == Handedness.Left)
        {
            tape.ReleaseLeftHand();
            Debug.Log("Tape released by LEFT hand");
        }
        else if (handRef.Handedness == Handedness.Right)
        {
            tape.ReleaseRightHand();
            Debug.Log("Tape released by RIGHT hand");
        }
    }

    private HandRef GetHandRefFromEvent(PointerEvent evt)
    {
        if (evt.Data == null)
            return null;

        HandRef direct = evt.Data as HandRef;
        if (direct != null)
            return direct;

        Component c = evt.Data as Component;
        if (c != null)
        {
            HandRef handRef = c.GetComponent<HandRef>();
            if (handRef != null)
                return handRef;
        }

        GameObject go = evt.Data as GameObject;
        if (go != null)
        {
            HandRef handRef = go.GetComponent<HandRef>();
            if (handRef != null)
                return handRef;
        }

        return null;
    }
}