using System;
using System.Diagnostics;
using UnityEngine;
using Whisper.Utils;

namespace Whisper.Samples
{
    public class WhisperTranscriptionService : MonoBehaviour
    {
        public WhisperManager whisper;
        public MicrophoneRecord microphoneRecord;
        public TMPro.TMP_Text stateText;

        [Header("Options")]
        public bool printLanguage = true;
        public bool streamSegments = true;

        [Header("VAD Auto Stop")]
        public bool useVadAutoStop = true;
        public float vadSilenceTimeout = 3f;

        public string CurrentState { get; private set; } = "Idle";
        public string CurrentTranscript { get; private set; } = "";
        public string LastFullTranscript { get; private set; } = "";

        public event Action<string> OnStateChanged;
        public event Action<string> OnTranscriptCompleted;
        public event Action<string> OnPartialTranscriptUpdated;

        private string _buffer = "";

        private void Awake()
        {
            if (whisper != null)
            {
                whisper.OnNewSegment += OnNewSegment;
                whisper.OnProgress += OnProgressHandler;
            }

            if (microphoneRecord != null)
            {
                microphoneRecord.OnRecordStop += OnRecordStop;
            }

            SetState("Idle");
        }

        private void OnDestroy()
        {
            if (whisper != null)
            {
                whisper.OnNewSegment -= OnNewSegment;
                whisper.OnProgress -= OnProgressHandler;
            }

            if (microphoneRecord != null)
            {
                microphoneRecord.OnRecordStop -= OnRecordStop;
            }
        }

        public void StartRecording()
        {
            if (whisper == null || microphoneRecord == null)
            {
                SetState("Error");
                UnityEngine.Debug.LogError("WhisperManager or MicrophoneRecord is missing.");
                return;
            }

            if (microphoneRecord.IsRecording)
                return;

            _buffer = "";
            CurrentTranscript = "";
            LastFullTranscript = "";

            microphoneRecord.useVad = true;
            microphoneRecord.vadStop = useVadAutoStop;
            microphoneRecord.vadStopTime = vadSilenceTimeout;

            microphoneRecord.StartRecord();
            SetState("Listening...");
        }

        public void StopRecording()
        {
            if (microphoneRecord == null || !microphoneRecord.IsRecording)
                return;

            microphoneRecord.StopRecord();
        }

        private async void OnRecordStop(AudioChunk recordedAudio)
        {
            SetState("Processing...");

            var sw = new Stopwatch();
            sw.Start();

            var res = await whisper.GetTextAsync(
                recordedAudio.Data,
                recordedAudio.Frequency,
                recordedAudio.Channels
            );

            if (res == null)
            {
                SetState("Idle");
                return;
            }

            var text = res.Result;

            if (printLanguage)
                text += $"\n\nLanguage: {res.Language}";

            LastFullTranscript = text;
            CurrentTranscript = text;

            OnTranscriptCompleted?.Invoke(text);
            SetState("Idle");
        }

        private void OnNewSegment(WhisperSegment segment)
        {
            if (!streamSegments)
                return;

            _buffer += segment.Text;
            CurrentTranscript = _buffer + "...";
            OnPartialTranscriptUpdated?.Invoke(CurrentTranscript);
        }

        private void OnProgressHandler(int progress)
        {
        }

        private void SetState(string newState)
        {
            CurrentState = newState;

            if (stateText != null)
                stateText.text = newState;

            OnStateChanged?.Invoke(newState);
        }

        public void OnPeaceSignDetected()
        {
            
            if (CurrentState == "Idle")
                StartRecording();
        }
    }


}