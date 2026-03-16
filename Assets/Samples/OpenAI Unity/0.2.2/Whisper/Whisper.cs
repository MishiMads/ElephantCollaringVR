using System;
using OpenAI;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Samples.Whisper
{
    public class Whisper : MonoBehaviour
    {
        private enum TranscriptionBackend
        {
            LocalWhisper,
            OpenAI
        }

        [Header("UI")]
        [SerializeField] private Button recordButton;
        [SerializeField] private Image progressBar;
        [SerializeField] private Text message;
        [SerializeField] private Dropdown dropdown;

        [Header("Transcription")]
        [SerializeField] private TranscriptionBackend backend = TranscriptionBackend.LocalWhisper;
        [SerializeField] private string language = "en";

        [Header("Local Whisper (OpenAI-compatible endpoint)")]
        [Tooltip("Example: http://127.0.0.1:8080/v1/audio/transcriptions")]
        [SerializeField] private string localWhisperEndpoint = "http://127.0.0.1:8080/v1/audio/transcriptions";
        [Tooltip("Primary model id. Can be HF-style, e.g. openai/whisper-tiny.en")]
        [SerializeField] private string localWhisperModel = "openai/whisper-tiny.en";
        [Tooltip("Optional fallback alias if server does not accept HF id, e.g. whisper-tiny.en")]
        [SerializeField] private string localWhisperModelFallbackAlias = "whisper-tiny.en";

        [Header("OpenAI (optional fallback)")]
        [SerializeField] private string openAiModel = "whisper-1";

        private readonly string fileName = "output.wav";
        private readonly int duration = 5;

        private AudioClip clip;
        private bool isRecording;
        private float time;

        private OpenAIApi openai;

        private void Start()
        {
            openai = new OpenAIApi();

#if UNITY_WEBGL && !UNITY_EDITOR
            dropdown.options.Add(new Dropdown.OptionData("Microphone not supported on WebGL"));
#else
            foreach (var device in Microphone.devices)
            {
                dropdown.options.Add(new Dropdown.OptionData(device));
            }

            recordButton.onClick.AddListener(StartRecording);
            dropdown.onValueChanged.AddListener(ChangeMicrophone);

            var savedIndex = PlayerPrefs.GetInt("user-mic-device-index", 0);
            var safeIndex = Mathf.Clamp(savedIndex, 0, Mathf.Max(0, dropdown.options.Count - 1));
            dropdown.SetValueWithoutNotify(safeIndex);
#endif
        }

        private void ChangeMicrophone(int index)
        {
            PlayerPrefs.SetInt("user-mic-device-index", index);
        }

        private void StartRecording()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            message.text = "Microphone not supported on WebGL";
            return;
#else
            if (dropdown.options.Count == 0)
            {
                message.text = "No microphone devices found.";
                return;
            }

            isRecording = true;
            recordButton.enabled = false;
            message.text = "Recording...";

            var savedIndex = PlayerPrefs.GetInt("user-mic-device-index", 0);
            var index = Mathf.Clamp(savedIndex, 0, dropdown.options.Count - 1);

            clip = Microphone.Start(dropdown.options[index].text, false, duration, 44100);
#endif
        }

        private async void EndRecording()
        {
            message.text = "Transcribing...";

#if !UNITY_WEBGL
            Microphone.End(null);
#endif

            byte[] data = SaveWav.Save(fileName, clip);

            try
            {
                string transcript;
                switch (backend)
                {
                    case TranscriptionBackend.LocalWhisper:
                        transcript = await TranscribeLocalWhisper(data);
                        break;
                    case TranscriptionBackend.OpenAI:
                    default:
                        transcript = await TranscribeOpenAI(data);
                        break;
                }

                progressBar.fillAmount = 0f;
                message.text = string.IsNullOrWhiteSpace(transcript) ? "(no transcription text returned)" : transcript;
            }
            catch (Exception ex)
            {
                message.text = $"Transcription failed: {ex.Message}";
            }
            finally
            {
                recordButton.enabled = true;
            }
        }

        private async System.Threading.Tasks.Task<string> TranscribeOpenAI(byte[] data)
        {
            var req = new CreateAudioTranscriptionsRequest
            {
                FileData = new FileData { Data = data, Name = "audio.wav" },
                Model = string.IsNullOrWhiteSpace(openAiModel) ? "whisper-1" : openAiModel,
                Language = language
            };

            var res = await openai.CreateAudioTranscription(req);
            return res.Text;
        }

        private async System.Threading.Tasks.Task<string> TranscribeLocalWhisper(byte[] wavBytes)
        {
            if (string.IsNullOrWhiteSpace(localWhisperEndpoint))
                throw new Exception("Local whisper endpoint is empty.");

            if (string.IsNullOrWhiteSpace(localWhisperModel))
                throw new Exception("Local whisper model is empty.");

            try
            {
                return await SendLocalWhisperRequest(wavBytes, localWhisperModel);
            }
            catch
            {
                if (string.IsNullOrWhiteSpace(localWhisperModelFallbackAlias) ||
                    string.Equals(localWhisperModelFallbackAlias, localWhisperModel, StringComparison.OrdinalIgnoreCase))
                {
                    throw;
                }

                return await SendLocalWhisperRequest(wavBytes, localWhisperModelFallbackAlias);
            }
        }

        private async System.Threading.Tasks.Task<string> SendLocalWhisperRequest(byte[] wavBytes, string modelToUse)
        {
            using (var request = new UnityWebRequest(localWhisperEndpoint, "POST"))
            {
                var form = new WWWForm();
                form.AddField("model", modelToUse);

                if (!string.IsNullOrWhiteSpace(language))
                    form.AddField("language", language);

                form.AddBinaryData("file", wavBytes, "audio.wav", "audio/wav");

                byte[] bodyRaw = form.data;
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();

                foreach (var header in form.headers)
                {
                    request.SetRequestHeader(header.Key, header.Value);
                }

                var op = request.SendWebRequest();
                while (!op.isDone)
                    await System.Threading.Tasks.Task.Yield();

#if UNITY_2020_1_OR_NEWER
                if (request.result != UnityWebRequest.Result.Success)
#else
                if (request.isHttpError || request.isNetworkError)
#endif
                {
                    throw new Exception($"{request.error} | {request.downloadHandler?.text}");
                }

                var json = request.downloadHandler.text;
                var parsed = JsonUtility.FromJson<OpenAIStyleTranscriptionResponse>(json);

                if (parsed == null || string.IsNullOrWhiteSpace(parsed.text))
                    throw new Exception($"Unexpected local whisper response: {json}");

                return parsed.text;
            }
        }

        [Serializable]
        private class OpenAIStyleTranscriptionResponse
        {
            public string text;
        }

        private void Update()
        {
            if (!isRecording) return;

            time += Time.deltaTime;
            progressBar.fillAmount = time / duration;

            if (time >= duration)
            {
                time = 0f;
                isRecording = false;
                EndRecording();
            }
        }
    }
}