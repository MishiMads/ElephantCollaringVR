using UnityEngine;
using Whisper;
using Whisper.Utils; // Needed for some result types

public class VoiceInput : MonoBehaviour
{
    [Header("Whisper References")]
    public WhisperManager whisper; // Drag the WhisperManager prefab/object here in Inspector
    public string language = "en";

    public event System.Action<string> OnTranscriptionReady;

    public async void ProcessAudio(AudioClip clip)
    {
        if (clip == null || whisper == null)
        {
            Debug.LogWarning("Missing AudioClip or WhisperManager reference!");
            return;
        }

        // The actual API call for whisper.unity
        var result = await whisper.GetTextAsync(clip);

        if (result != null)
        {
            OnWhisperComplete(result.Result);
        }
    }

    private void OnWhisperComplete(string text)
    {
        Debug.Log($"🎤 Whisper result: {text}");
        OnTranscriptionReady?.Invoke(text);
    }
}