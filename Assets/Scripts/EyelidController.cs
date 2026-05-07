using UnityEngine;
using System;
using System.Collections;

public class EyelidController : MonoBehaviour
{
    [Header("Eyelid References")]
    public RectTransform topEyelid;
    public RectTransform bottomEyelid;

    [Header("Timing Settings")]
    public float closeDuration = 0.5f;
    public float stayClosedDuration = 0.5f;
    public float openDuration = 2.0f;

    [Header("Audio")]
    public AudioSource helicopterSound;

    [Header("Positions")]
    private readonly Vector2 topOpen = new Vector2(0f, 770f);
    private readonly Vector2 topClosed = new Vector2(0f, 240f);

    private readonly Vector2 bottomOpen = new Vector2(0f, -770f);
    private readonly Vector2 bottomClosed = new Vector2(0f, -240f);

    private bool isRunning = false;

    void Awake()
    {
        SetOpen();
    }

    public void SetOpen()
    {
        if (topEyelid != null)
            topEyelid.anchoredPosition = topOpen;

        if (bottomEyelid != null)
            bottomEyelid.anchoredPosition = bottomOpen;
    }

    public IEnumerator PlayProcedureTransition(Action onFullyClosed = null)
    {
        if (isRunning)
            yield break;

        isRunning = true;

        // Close eyelids
        yield return StartCoroutine(AnimateLids(
            topOpen,
            topClosed,
            bottomOpen,
            bottomClosed,
            closeDuration
        ));

        // Run procedure action while eyes are fully closed
        onFullyClosed?.Invoke();

        // Play helicopter sound while closed
        if (helicopterSound != null)
            helicopterSound.Play();

        // Stay closed briefly
        yield return new WaitForSeconds(stayClosedDuration);

        // Open eyelids slowly
        yield return StartCoroutine(AnimateLids(
            topClosed,
            topOpen,
            bottomClosed,
            bottomOpen,
            openDuration
        ));

        isRunning = false;
    }

    IEnumerator AnimateLids(
        Vector2 topStart,
        Vector2 topEnd,
        Vector2 bottomStart,
        Vector2 bottomEnd,
        float duration
    )
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            t = Mathf.SmoothStep(0f, 1f, t);

            topEyelid.anchoredPosition = Vector2.Lerp(topStart, topEnd, t);
            bottomEyelid.anchoredPosition = Vector2.Lerp(bottomStart, bottomEnd, t);

            yield return null;
        }

        topEyelid.anchoredPosition = topEnd;
        bottomEyelid.anchoredPosition = bottomEnd;
    }
}