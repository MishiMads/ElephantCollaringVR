using UnityEngine;
using System.Collections;

public class Wobble : MonoBehaviour
{
    Renderer rend;
    Material mat;

    [Header("Pour Settings")]
    public ParticleSystem pourParticles;

    [Tooltip("Total time in seconds for the bucket to empty")]
    public float pourDuration = 3f;

    private float currentFill = 1f;
    private bool _isMonitoring = false;
    private bool _isPouring = false;
    private bool _hasFinishedPouring = false;

    [Header("Pouring Sound")]
    [SerializeField] private AudioSource pouringAudioSource;
    [SerializeField] private AudioClip pouringSound;

    void Start()
    {
        rend = GetComponent<Renderer>();

        if (rend != null)
        {
            mat = rend.material;

            if (mat.HasProperty("_Fill"))
                currentFill = mat.GetFloat("_Fill");
        }

        if (pouringAudioSource != null)
        {
            pouringAudioSource.playOnAwake = false;
            pouringAudioSource.loop = true;

            if (pouringSound != null)
                pouringAudioSource.clip = pouringSound;
        }
    }

    public void StartAutoPour()
    {
        if (_hasFinishedPouring) return;

        _isMonitoring = true;
    }

    void Update()
    {
        // HARD STOP once finished
        if (_hasFinishedPouring) return;

        if (!_isMonitoring && !_isPouring) return;

        float currentZ = transform.parent.localEulerAngles.z;
        if (currentZ > 180) currentZ -= 360;

        // Start pouring when tilted enough
        if (Mathf.Abs(currentZ) >= 56.25f || _isPouring)
        {
            if (currentFill > 0)
            {
                _isPouring = true;

                // Play particles
                if (pourParticles != null && !pourParticles.isPlaying)
                    pourParticles.Play();

                // Play sound
                PlayPouringSound();

                // Drain over fixed duration
                float drainRate = 1f / pourDuration;
                currentFill -= drainRate * Time.deltaTime;

                if (mat != null)
                    mat.SetFloat("_Fill", currentFill);
            }
            else
            {
                FinishPouring();
            }
        }
    }

    private void FinishPouring()
    {
        currentFill = 0;
        if (mat != null)
            mat.SetFloat("_Fill", 0);

        _isPouring = false;
        _isMonitoring = false;
        _hasFinishedPouring = true;

        // Stop particles
        if (pourParticles != null && pourParticles.isPlaying)
            pourParticles.Stop();

        // Stop sound
        StopPouringSound();

        // Disable/destroy bucket
        StartCoroutine(DisableBucketRoutine());
    }

    private void PlayPouringSound()
    {
        if (pouringAudioSource == null)
            return;

        if (!pouringAudioSource.isPlaying)
            pouringAudioSource.Play();
    }

    private void StopPouringSound()
    {
        if (pouringAudioSource == null)
            return;

        if (pouringAudioSource.isPlaying)
            pouringAudioSource.Stop();
    }

    IEnumerator DisableBucketRoutine()
    {
        yield return new WaitForSeconds(0.2f);

        if (transform.parent != null)
        {
            Destroy(transform.parent.gameObject); // cleaner than SetActive(false)
        }
        else
        {
            Destroy(gameObject);
        }
    }
}