using System.Collections;
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

    Coroutine talkingRoutine;

    public void brieferSpeak()
    {
        if (talkingRoutine != null)
            StopCoroutine(talkingRoutine);

        talkingRoutine = StartCoroutine(TalkingLoop());
    }

    IEnumerator TalkingLoop()
    {
        animatorBrief.SetBool("isTalking", true);

        while (true)
        {
            int current = animatorBrief.GetInteger("talkVariant");
            int next;

            do
            {
                next = Random.Range(0, 4);
            }
            while (next == current);

            animatorBrief.SetInteger("talkVariant", next);

            yield return new WaitForSeconds(Random.Range(3f, 6f));
        }
    }

    public void brieferNoSpeak()
    {
        if (talkingRoutine != null)
            StopCoroutine(talkingRoutine);

        animatorBrief.SetBool("isTalking", false);
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