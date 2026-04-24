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

    void Start()
    {
        rend = GetComponent<Renderer>();
        if (rend != null)
        {
            mat = rend.material;
            if (mat.HasProperty("_Fill"))
                currentFill = mat.GetFloat("_Fill");
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

                if (!pourParticles.isPlaying) pourParticles.Play();

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

                if (pourParticles.isPlaying) pourParticles.Stop();

                // Start the routine to disable the whole bucket
                StartCoroutine(DisableBucketRoutine());
            }
        }
    }

    IEnumerator DisableBucketRoutine()
    {
        // Optional: Wait a tiny bit so the end of the particle effect 
        // isn't cut off abruptly when the object vanishes
        yield return new WaitForSeconds(0.2f);

        // Disables the "Water bucket" parent object
        if (transform.parent != null)
        {
            transform.parent.gameObject.SetActive(false);
        }
        else
        {
            // Fallback if it has no parent
            gameObject.SetActive(false);
        }
    }
}