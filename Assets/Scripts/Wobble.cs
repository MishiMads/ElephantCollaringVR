using UnityEngine;
using System.Collections;

public class Wobble : MonoBehaviour
{
    Renderer rend;
    Material mat;

    [Header("Pouring Settings")]
    public ParticleSystem pourParticles;
    public float emptySpeed = 0.05f;
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
        if (!_hasFinishedPouring)
        {
            _isMonitoring = true;
        }
    }

    void Update()
    {
        if (_hasFinishedPouring || (!_isMonitoring && !_isPouring)) return;

        float currentZ = transform.parent.localEulerAngles.z;
        if (currentZ > 180) currentZ -= 360;

        if (Mathf.Abs(currentZ) >= 56.25f || _isPouring)
        {
            if (currentFill > 0)
            {
                _isPouring = true;

                if (pourParticles != null && !pourParticles.isPlaying)
                    pourParticles.Play();

                PlayPouringSound();

                currentFill -= emptySpeed * Time.deltaTime;
                mat.SetFloat("_Fill", currentFill);
            }
            else
            {
                currentFill = 0;
                mat.SetFloat("_Fill", 0);

                _isPouring = false;
                _isMonitoring = false;
                _hasFinishedPouring = true;

                if (pourParticles != null && pourParticles.isPlaying)
                    pourParticles.Stop();

                StopPouringSound();

                StartCoroutine(DisableBucketRoutine());
            }
        }
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
            transform.parent.gameObject.SetActive(false);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}