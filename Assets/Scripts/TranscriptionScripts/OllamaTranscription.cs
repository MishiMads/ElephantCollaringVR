using System;
using System.Collections;
using System.Text;
using System.Threading.Tasks;
using Samples.Whisper;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace TranscriptionScripts
{
    [Serializable]
    public class OllamaAudioRequest
    {
        public string model;
        public string prompt;
        public string audio; // base64
        public bool stream = false;
    }

    [Serializable]
    public class OllamaAudioResponse
    {
        public string response; // transcription text
    }

    public class Whisper : MonoBehaviour
    {
        [SerializeField] private Button recordButton;
        [SerializeField] private Image progressBar;
        [SerializeField] private Text message;
        [SerializeField] private Dropdown dropdown;

        private readonly string fileName = "output.wav";
        private readonly int duration = 5;
        private const string OllamaUrl = "http://127.0.0.1:11434/api/generate";
        // Here, you can specify which model you want to use for transcription (it needs to be a model that supports audio input in Ollama).
        private const string OllamaModel = "llama3:8b"; 

        private AudioClip clip;
        private bool isRecording;
        private float time;

        private void Start()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            dropdown.options.Add(new Dropdown.OptionData("Microphone not supported on WebGL"));
#else
            foreach (var device in Microphone.devices)
                dropdown.options.Add(new Dropdown.OptionData(device));

            recordButton.onClick.AddListener(StartRecording);
            dropdown.onValueChanged.AddListener(ChangeMicrophone);

            var index = PlayerPrefs.GetInt("user-mic-device-index", 0);
            dropdown.SetValueWithoutNotify(Mathf.Clamp(index, 0, Mathf.Max(0, dropdown.options.Count - 1)));
#endif
        }

        private void ChangeMicrophone(int index) => PlayerPrefs.SetInt("user-mic-device-index", index);

        private void StartRecording()
        {
            isRecording = true;
            recordButton.enabled = false;

            var index = PlayerPrefs.GetInt("user-mic-device-index", 0);
#if !UNITY_WEBGL
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
                var text = await TranscribeWithOllama(data);
                progressBar.fillAmount = 0f;
                message.text = string.IsNullOrWhiteSpace(text) ? "No transcription returned." : text;
            }
            catch (Exception ex)
            {
                message.text = $"Ollama error: {ex.Message}";
            }
            finally
            {
                recordButton.enabled = true;
            }
        }

        private async Task<string> TranscribeWithOllama(byte[] wavData)
        {
            var reqBody = new OllamaAudioRequest
            {
                model = OllamaModel,
                prompt = "Transcribe this audio to plain text.",
                audio = Convert.ToBase64String(wavData),
                stream = false
            };

            var json = JsonUtility.ToJson(reqBody);
            var bodyRaw = Encoding.UTF8.GetBytes(json);

            using var req = new UnityWebRequest(OllamaUrl, "POST");
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            var op = req.SendWebRequest();
            while (!op.isDone)
                await Task.Yield();

#if UNITY_2020_1_OR_NEWER
            if (req.result != UnityWebRequest.Result.Success)
#else
            if (req.isHttpError || req.isNetworkError)
#endif
                throw new Exception(req.error + " | " + req.downloadHandler.text);

            var resp = JsonUtility.FromJson<OllamaAudioResponse>(req.downloadHandler.text);
            return resp?.response ?? string.Empty;
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
