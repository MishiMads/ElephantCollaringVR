using UnityEngine;
using System.Collections;
using System.Linq;

public class VoiceTest : MonoBehaviour
{
    public VoiceInput voiceInput;
    private AudioClip micClip;

    void Start()
    {
        // ✅ Use HEADSET MIC specifically
        string[] devices = Microphone.devices;
        string headsetMic = devices[0];  // "Headset Microphone (Oculus Virtual Audio Device)"
        Debug.Log($"🎤 Using headset: {headsetMic}");

        voiceInput.OnTranscriptionReady += OnText;
        StartCoroutine(RecordAndProcess());
    }

    IEnumerator RecordAndProcess()
    {
        string headsetMic = Microphone.devices[0];
        Debug.Log("🎤 Recording HEADSET MIC for 6 seconds... SPEAK CLOSE TO QUEST MIC!");

        micClip = Microphone.Start(headsetMic, false, 6, 16000);
        yield return new WaitForSeconds(0.5f);
        Debug.Log("🗣️ SPEAKING TIME! Say 'What is my mission' LOUDLY");
        yield return new WaitForSeconds(6f);

        Microphone.End(headsetMic);
        yield return new WaitForSeconds(0.2f);

        // ✅ LOWERED THRESHOLD - headset mics are quieter
        float[] samples = new float[micClip.samples];
        micClip.GetData(samples, 0);

        float maxVolume = 0f;
        float avgVolume = 0f;
        int loudSamples = 0;

        for (int i = 0; i < samples.Length; i++)
        {
            float abs = Mathf.Abs(samples[i]);
            avgVolume += abs;
            maxVolume = Mathf.Max(maxVolume, abs);
            if (abs > 0.005f) loudSamples++;  // LOWER threshold
        }
        avgVolume /= samples.Length;

        Debug.Log($"📊 Headset Audio:");
        Debug.Log($"   Max: {maxVolume:F4}  (good: >0.005)");
        Debug.Log($"   Avg: {avgVolume:F4}  (good: >0.001)");
        Debug.Log($"   Loud: {loudSamples}/{samples.Length} ({(loudSamples * 100f / samples.Length):F1}%)");

        // ✅ MUCH LOWER threshold for headsets
        if (maxVolume < 0.003f)
        {
            Debug.LogWarning("⚠️  VERY QUIET - try speaking CLOSER to Quest mic");
        }
        else
        {
            Debug.Log("✅ Audio detected! Sending to Whisper...");
            voiceInput.ProcessAudio(micClip);
        }
    }

    void OnText(string text)
    {
        Debug.Log($"🎤 RESULT: '{text}'");
        if (!text.Contains("[BLANK_AUDIO]"))
            Debug.Log("✅ WHISPER SUCCESS!");
    }
}
