using System.Collections.Generic;
using UnityEngine;

public class BrieferCode : MonoBehaviour
{
    public Animator animatorBrief;
    public Transform cameraTarget;

    [Range(0f, 1f)] public float overallWeight = 1f;
    [Range(0f, 1f)] public float bodyWeight = 0.2f;
    [Range(0f, 1f)] public float headWeight = 0.8f;
    [Range(0f, 1f)] public float eyesWeight = 1f;
    [Range(0f, 1f)] public float clampWeight = 0.5f;

    public List<string> dialogue;

    void Start()
    {
        if (animatorBrief == null)
            animatorBrief = GetComponent<Animator>();

        if (cameraTarget == null && Camera.main != null)
            cameraTarget = Camera.main.transform;
    }

    public void brieferSpeak()
    {
        animatorBrief.SetBool("isSpeaking", true);
    }

    public void brieferNoSpeak()
    {
        animatorBrief.SetBool("isSpeaking", false);
    }

    void OnAnimatorIK(int layerIndex)
    {
        if (animatorBrief == null || cameraTarget == null) return;

        animatorBrief.SetLookAtWeight(
            overallWeight,
            bodyWeight,
            headWeight,
            eyesWeight,
            clampWeight
        );

        animatorBrief.SetLookAtPosition(cameraTarget.position);
    }
}