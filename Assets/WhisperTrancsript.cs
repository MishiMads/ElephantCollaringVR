using Meta.XR.ImmersiveDebugger.UserInterface.Generic;
using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.InputSystem;
using Whisper.Utils;

namespace Whisper.Samples
{
    public class WhisperTranscriptionService : MonoBehaviour
    {
        public WhisperManager whisper;
        public MicrophoneRecord microphoneRecord;
        public ConversationManager conversationManager;
        public TMPro.TMP_Text stateText;

        private bool isListening;

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
        private void Start()
        {
            if (!microphoneRecord.IsRecording)
            {
                microphoneRecord.useVad = true;
                microphoneRecord.vadStop = true;

                microphoneRecord.StartRecord();
            }
        }
        private void Awake()
        {
            if (microphoneRecord != null)
            {
                microphoneRecord.OnVadStop += OnVadStop;
            }

            if (whisper != null)
            {
                whisper.OnNewSegment += OnNewSegment;
                whisper.OnProgress += OnProgressHandler;
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
                return;
            }

            if (isListening)
                return;

            _buffer = "";
            CurrentTranscript = "";
            LastFullTranscript = "";

            isListening = true;

            microphoneRecord.IsSessionActive = true;

            SetState("Listening...");
        }

        private async void OnVadStop(AudioChunk recordedAudio)
        {
            SetState("Processing...");

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

            LastFullTranscript = text;
            CurrentTranscript = text;

            OnTranscriptCompleted?.Invoke(text);

            isListening = false;
            microphoneRecord.IsSessionActive = false;

            SetState("Idle");
        }

        public void StopRecording()
        {
            if (microphoneRecord == null || !microphoneRecord.IsRecording)
                return;

            
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
        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                OnPeaceSignDetected();
            }

        }


        public void OnPeaceSignDetected()
        {
            microphoneRecord.ResetSpeechDetection();
            UnityEngine.Debug.Log("Peace gesture triggered");
            if (CurrentState == "Idle")
                conversationManager.StartPlayerRecording();
        }
    }


}