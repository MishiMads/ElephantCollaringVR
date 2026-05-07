using UnityEngine;
using System.Collections;

public class EyelidController : MonoBehaviour
{
    [Header("Eyelid References")]
    public RectTransform topEyelid;
    public RectTransform bottomEyelid;

    [Header("Timing Settings")]
    public float initialDelay = 1.0f;      // Wait before starting the close
    public float closeDuration = 0.5f;     // Speed of closing
    public float stayClosedDuration = 0.5f; // Pause while closed
    public float openDuration = 2.0f;      // Speed of opening (usually slower for "waking up")

    [Header("Coordinates")]
    private readonly Vector2 topOpen = new Vector2(0f, 820f);
    private readonly Vector2 topClosed = new Vector2(0f, 270f);
    private readonly Vector2 bottomOpen = new Vector2(0f, -812f);
    private readonly Vector2 bottomClosed = new Vector2(0f, -270f);

    public AudioClip helicopterSound;

    void Start()
    {
        
    }

    public void TriggerBlink()
    {
        if (topEyelid != null && bottomEyelid != null)
        {
            // Start in the OPEN position
            topEyelid.anchoredPosition = topOpen;
            bottomEyelid.anchoredPosition = bottomOpen;

            StartCoroutine(FullBlinkSequence());
            if (helicopterSound != null)
            {
                AudioSource.PlayClipAtPoint(helicopterSound, Camera.main.transform.position);
            }
        }
    }

    IEnumerator FullBlinkSequence()
    {
        // 1. Initial wait while eyes are open
        yield return new WaitForSeconds(initialDelay);

        // 2. CLOSE (using closeDuration)
        yield return StartCoroutine(AnimateLids(topOpen, topClosed, bottomOpen, bottomClosed, closeDuration));

        // 3. STAY CLOSED
        yield return new WaitForSeconds(stayClosedDuration);

        // 4. OPEN (using openDuration)
        yield return StartCoroutine(AnimateLids(topClosed, topOpen, bottomClosed, bottomOpen, openDuration));
    }

    // This helper now takes a 'duration' parameter so each movement can have a unique speed
    IEnumerator AnimateLids(Vector2 tStart, Vector2 tEnd, Vector2 bStart, Vector2 bEnd, float duration)
    {
        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            // Using SmoothStep for a polished "organic" feel
            float percent = Mathf.SmoothStep(0, 1, elapsed / duration);

            topEyelid.anchoredPosition = Vector2.Lerp(tStart, tEnd, percent);
            bottomEyelid.anchoredPosition = Vector2.Lerp(bStart, bEnd, percent);

            yield return null;
        }

        topEyelid.anchoredPosition = tEnd;
        bottomEyelid.anchoredPosition = bEnd;
    }
}