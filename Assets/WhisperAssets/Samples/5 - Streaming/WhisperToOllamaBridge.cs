using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Whisper.Utils;

namespace Whisper.Samples
{
    public class WhisperToOllamaBridge : MonoBehaviour
    {
        [Header("References")]
        public WhisperManager whisper;
        public MicrophoneRecord microphoneRecord;
        public Text llmOutputText;

        [Header("Ollama")]
        public string ollamaUrl = "http://localhost:11434/api/generate";
        public string model = "mistral";
        [TextArea] public string systemPrompt = "You are a concise assistant.";
        public bool useStreamingSegments = false;

        private string _fullTranscript = "";

        [Serializable]
        private class OllamaRequest
        {
            public string model;
            public string prompt;
            public bool stream;
        }

        [Serializable]
        private class OllamaResponse
        {
            public string response;
            public bool done;
        }

        private void Awake()
        {
            if (whisper != null)
                whisper.OnNewSegment += OnNewSegment;

            if (microphoneRecord != null)
                microphoneRecord.OnRecordStop += OnRecordStop;
        }

        private void OnDestroy()
        {
            if (whisper != null)
                whisper.OnNewSegment -= OnNewSegment;

            if (microphoneRecord != null)
                microphoneRecord.OnRecordStop -= OnRecordStop;
        }

        private void OnNewSegment(WhisperSegment segment)
        {
            if (!useStreamingSegments) return;
            if (segment == null || string.IsNullOrWhiteSpace(segment.Text)) return;

            _fullTranscript += segment.Text;
        }

        private async void OnRecordStop(AudioChunk recordedAudio)
        {
            if (whisper == null || recordedAudio.Data == null) return;

            var res = await whisper.GetTextAsync(recordedAudio.Data, recordedAudio.Frequency, recordedAudio.Channels);
            if (res == null || string.IsNullOrWhiteSpace(res.Result)) return;

            var transcript = useStreamingSegments ? _fullTranscript : res.Result;
            _fullTranscript = "";

            StartCoroutine(SendToOllamaCoroutine(transcript));
        }

        private IEnumerator SendToOllamaCoroutine(string transcript)
        {
            if (string.IsNullOrWhiteSpace(transcript)) yield break;

            var prompt = $"{systemPrompt}\n\nUser transcription:\n{transcript}";
            var reqBody = new OllamaRequest
            {
                model = model,
                prompt = prompt,
                stream = false
            };

            var json = JsonUtility.ToJson(reqBody);
            var bodyRaw = Encoding.UTF8.GetBytes(json);

            using (var request = new UnityWebRequest(ollamaUrl, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    if (llmOutputText) llmOutputText.text = $"Ollama error: {request.error}";
                    yield break;
                }

                var raw = request.downloadHandler.text;
                OllamaResponse ollamaRes = null;

                try
                {
                    ollamaRes = JsonUtility.FromJson<OllamaResponse>(raw);
                }
                catch (Exception ex)
                {
                    if (llmOutputText) llmOutputText.text = $"Parse error: {ex.Message}\nRaw: {raw}";
                    yield break;
                }

                if (llmOutputText)
                    llmOutputText.text = ollamaRes != null ? ollamaRes.response : "No response";
            }
        }
    }
}
