using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Meta.WitAi.TTS.Utilities;

namespace Meta.WitAi.TTS.UX
{
    public class TextToSpeech : MonoBehaviour
    {
        [SerializeField] private TTSSpeaker _speaker;
        [SerializeField] private InputField _input;
        [SerializeField] private Button _stopButton;
        [SerializeField] private Button _pauseButton;
        [SerializeField] private Button _speakButton;
        [SerializeField] private Toggle _queueButton;
        [SerializeField] private Toggle _asyncToggle;
        [SerializeField] private AudioClip _asyncClip;
        [SerializeField] private string _dateId = "[DATE]";
        [SerializeField] private string[] _queuedText;

        private string _voice;
        private bool _loading;
        private bool _speaking;
        private bool _paused;

        private void OnEnable()
        {
            RefreshStopButton();
            RefreshPauseButton();
            if (_stopButton != null) _stopButton.onClick.AddListener(StopClick);
            if (_pauseButton != null) _pauseButton.onClick.AddListener(PauseClick);
            if (_speakButton != null) _speakButton.onClick.AddListener(SpeakClick);
        }

        private void StopClick()
        {
            if (_speaker != null) _speaker.Stop();
        }

        private void PauseClick()
        {
            if (_speaker == null) return;
            if (_speaker.IsPaused) _speaker.Resume();
            else _speaker.Pause();
        }

        private void SpeakClick()
        {
            SpeakText(_input != null ? _input.text : string.Empty);
        }

        // Public entry point for external scripts (e.g. LLMWithRAG)
        public void SpeakText(string text)
        {
            if (string.IsNullOrWhiteSpace(text) || _speaker == null) return;

            string phrase = FormatText(text);
            bool queued = _queueButton != null && _queueButton.isOn;
            bool async = _asyncToggle != null && _asyncToggle.isOn;

            if (async) StartCoroutine(SpeakAsync(phrase, queued));
            else if (queued) _speaker.SpeakQueued(phrase);
            else _speaker.Speak(phrase);

            if (_queuedText != null && _queuedText.Length > 0 && queued)
            {
                foreach (var queuedText in _queuedText)
                {
                    _speaker.SpeakQueued(FormatText(queuedText));
                }
            }
        }

        private IEnumerator SpeakAsync(string phrase, bool queued)
        {
            if (_speaker == null) yield break;

            if (queued) yield return _speaker.SpeakQueuedAsync(new string[] { phrase });
            else yield return _speaker.SpeakAsync(phrase);

            if (_asyncClip != null && _speaker.AudioSource != null) _speaker.AudioSource.PlayOneShot(_asyncClip);
        }

        private string FormatText(string text)
        {
            string result = text;
            if (result.Contains(_dateId))
            {
                DateTime now = DateTime.UtcNow;
                string dateString = $"{now.ToLongDateString()} at {now.ToLongTimeString()}";
                result = text.Replace(_dateId, dateString);
            }
            return result;
        }

        private void OnDisable()
        {
            if (_stopButton != null) _stopButton.onClick.RemoveListener(StopClick);
            if (_pauseButton != null) _pauseButton.onClick.RemoveListener(PauseClick);
            if (_speakButton != null) _speakButton.onClick.RemoveListener(SpeakClick);
        }

        private void Update()
        {
            if (_speaker == null) return;

            if (!string.Equals(_voice, _speaker.VoiceID))
            {
                _voice = _speaker.VoiceID;
                if (_input != null && _input.placeholder != null)
                {
                    var placeholderText = _input.placeholder.GetComponent<Text>();
                    if (placeholderText != null)
                    {
                        placeholderText.text = $"Write something to say in {_voice}'s voice";
                    }
                }
            }
            if (_loading != _speaker.IsLoading) RefreshStopButton();
            if (_speaking != _speaker.IsSpeaking) RefreshStopButton();
            if (_paused != _speaker.IsPaused) RefreshPauseButton();
        }

        private void RefreshStopButton()
        {
            if (_speaker == null) return;
            _loading = _speaker.IsLoading;
            _speaking = _speaker.IsSpeaking;
            if (_stopButton != null) _stopButton.interactable = _loading || _speaking;
        }

        private void RefreshPauseButton()
        {
            if (_speaker == null) return;
            _paused = _speaker.IsPaused;
            if (_pauseButton != null)
            {
                var pauseText = _pauseButton.GetComponentInChildren<Text>();
                if (pauseText != null) pauseText.text = _paused ? "Resume" : "Pause";
                _pauseButton.interactable = _speaker.IsSpeaking || _paused;
            }
        }
    }
}
