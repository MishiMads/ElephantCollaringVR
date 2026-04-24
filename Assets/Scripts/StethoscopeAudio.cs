using UnityEngine;

public class StethoscopeAudio : MonoBehaviour
{
    [Header("Audio Settings")]
    public AudioSource heartBeatAudio;
    public string targetSocketID = ""; // Match the ID of your heart SnapZone

    private void OnTriggerEnter(Collider other)
    {
        // Check if the thing we touched is a SnapZone
        if (other.CompareTag("SnapZone"))
        {
            var socket = other.GetComponent<ToolSocket>();

            // Only play if the socket ID matches (e.g., "Heart")
            if (socket != null && socket.socketID == targetSocketID)
            {
                if (heartBeatAudio != null && !heartBeatAudio.isPlaying)
                {
                    heartBeatAudio.Play();
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("SnapZone"))
        {
            // If we move the stethoscope away, stop the sound
            if (heartBeatAudio != null && heartBeatAudio.isPlaying)
            {
                heartBeatAudio.Stop();
            }
        }
    }
}