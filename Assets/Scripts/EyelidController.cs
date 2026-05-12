using UnityEngine;
using System;
using System.Collections;

public class EyelidController : MonoBehaviour
{
    [Header("Eyelid References")]
    public RectTransform topEyelid;
    public RectTransform bottomEyelid;

    [Header("Timing Settings")]
    [Min(0f)]
    public float closeDuration = 0.5f;

    [Min(0f)]
    public float stayClosedDuration = 1.0f;

    [Min(0f)]
    public float openDuration = 2.0f;

    [Header("Audio")]
    public AudioSource helicopterSound;

    [Header("Positions")]
    public Vector2 topOpen = new Vector2(0f, 770f);
    public Vector2 topClosed = new Vector2(0f, 240f);

    public Vector2 bottomOpen = new Vector2(0f, -770f);
    public Vector2 bottomClosed = new Vector2(0f, -240f);

    private bool isRunning = false;

    private void Awake()
    {
        SetOpen();
    }

    public void SetOpen()
    {
        if (topEyelid != null)
        {
            topEyelid.anchoredPosition = topOpen;
        }

        if (bottomEyelid != null)
        {
            bottomEyelid.anchoredPosition = bottomOpen;
        }
    }

    public IEnumerator PlayProcedureTransition(Action onFullyClosed = null)
    {
        if (isRunning)
        {
            yield break;
        }

        isRunning = true;

        Debug.Log("Eyelids: closing");

        yield return StartCoroutine(AnimateLids(
            topEyelid.anchoredPosition,
            topClosed,
            bottomEyelid.anchoredPosition,
            bottomClosed,
            closeDuration
        ));

        Debug.Log("Eyelids: fully closed");

        try
        {
            onFullyClosed?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError("Error during onFullyClosed action.");
            Debug.LogException(e);
        }

        if (helicopterSound != null)
        {
            helicopterSound.Play();
        }

        Debug.Log("Eyelids: staying closed");

        yield return new WaitForSecondsRealtime(stayClosedDuration);

        Debug.Log("Eyelids: opening");

        yield return StartCoroutine(AnimateLids(
            topEyelid.anchoredPosition,
            topOpen,
            bottomEyelid.anchoredPosition,
            bottomOpen,
            openDuration
        ));

        Debug.Log("Eyelids: open again");

        isRunning = false;

        if (MainScript.Instance != null)
        {
            MainScript.Instance.StartProcedureGuidance();
        }
    }

    private IEnumerator AnimateLids(
        Vector2 topStart,
        Vector2 topEnd,
        Vector2 bottomStart,
        Vector2 bottomEnd,
        float duration
    )
    {
        if (topEyelid == null || bottomEyelid == null)
        {
            Debug.LogWarning("Missing eyelid references.");
            yield break;
        }

        if (duration <= 0f)
        {
            topEyelid.anchoredPosition = topEnd;
            bottomEyelid.anchoredPosition = bottomEnd;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

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