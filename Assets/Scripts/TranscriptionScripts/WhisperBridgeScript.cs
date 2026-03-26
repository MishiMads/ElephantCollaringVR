using System;
using UnityEngine;
using UnityEngine.UI;
using Whisper.Utils;
using LLMUnitySamples;

namespace Whisper.Samples
{
    public class WhisperBridgeScript : MonoBehaviour
    {
        [Header("References")]
        public WhisperManager whisper;
        public MicrophoneRecord microphoneRecord;
        public Text llmOutputText;
        public LLMWithRAG LLMWithRAGScript;

        [Header("Transcription")]
        public bool useStreamingSegments = false;

        private string _fullTranscript = "";

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

            if (llmOutputText)
                llmOutputText.text = _fullTranscript;
        }

        private async void OnRecordStop(AudioChunk recordedAudio)
        {
            if (whisper == null || recordedAudio.Data == null) return;

            var res = await whisper.GetTextAsync(recordedAudio.Data, recordedAudio.Frequency, recordedAudio.Channels);
            if (res == null || string.IsNullOrWhiteSpace(res.Result))
            {
                _fullTranscript = "";
                return;
            }

            var transcript = useStreamingSegments ? _fullTranscript : res.Result;
            _fullTranscript = "";

            if (llmOutputText)
                llmOutputText.text = transcript;

            if (LLMWithRAGScript != null && !string.IsNullOrWhiteSpace(transcript))
                LLMWithRAGScript.SubmitExternalInput(transcript);
        }
    }
}