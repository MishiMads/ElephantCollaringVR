using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class BlinkEffect : MonoBehaviour
{
    [Header("Eyelid Objects")]
    [SerializeField] private RectTransform topLid;
    [SerializeField] private RectTransform bottomLid;

    [Header("Settings")]
    [Tooltip("The position lids move TO to cover the eye (usually 0).")]
    [SerializeField] private float closedPositionY = 0f;

    [Tooltip("The distance lids retreat when open. Match this to your UI height.")]
    [SerializeField] private float openOffset = 500f;

    [SerializeField] private float blinkSpeed = 0.1f;

    private Coroutine blinkCoroutine;

    private void Start()
    {
        // Set initial state: Open and Disabled
        SetLidPositions(openOffset);
        ToggleLids(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TriggerBlink();
        }
    }

    public void TriggerBlink()
    {
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
        }
        blinkCoroutine = StartCoroutine(BlinkRoutine());
    }

    private IEnumerator BlinkRoutine()
    {
        ToggleLids(true);

        // 1. Lids Close
        yield return StartCoroutine(MoveLids(openOffset, closedPositionY, blinkSpeed));

        // 2. Lids Open
        yield return StartCoroutine(MoveLids(closedPositionY, openOffset, blinkSpeed));

        ToggleLids(false);
    }

    private IEnumerator MoveLids(float startY, float endY, float duration)
    {
        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float currentY = Mathf.Lerp(startY, endY, elapsed / duration);
            SetLidPositions(currentY);
            yield return null;
        }
        SetLidPositions(endY);
    }

    private void SetLidPositions(float offsetY)
    {
        topLid.anchoredPosition = new Vector2(topLid.anchoredPosition.x, offsetY);
        bottomLid.anchoredPosition = new Vector2(bottomLid.anchoredPosition.x, -offsetY);
    }

    private void ToggleLids(bool state)
    {
        topLid.gameObject.SetActive(state);
        bottomLid.gameObject.SetActive(state);
    }
}