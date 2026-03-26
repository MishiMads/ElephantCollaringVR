using UnityEngine.UI;
using LLMUnity;
using System.Threading.Tasks;
using Meta.WitAi.TTS.UX;
using UnityEngine;

namespace LLMUnitySamples
{
    public class BackUpLLMAndRAG : RAGSample
    {
        public LLMAgent llmAgent;
        public Toggle ParaphraseWithLLM;
        [SerializeField] private TextToSpeech ttsSpeakerInput;

        // Public entry point for external scripts (e.g., Whisper bridge)
        public void SubmitExternalInput(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            onInputFieldSubmit(message);
        }

        protected override async void onInputFieldSubmit(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                AIReplyComplete();
                return;
            }

            playerText.interactable = false;
            AIText.text = "...";
            (string[] similarPhrases, float[] distances) = await rag.Search(message, 1);
            string similarPhrase = (similarPhrases != null && similarPhrases.Length > 0)
                ? similarPhrases[0]
                : message;

            bool paraphraseEnabled = ParaphraseWithLLM != null && ParaphraseWithLLM.isOn;
            if (!paraphraseEnabled)
            {
                AIText.text = similarPhrase;
                await Task.Yield();
                AIReplyComplete();
            }
            else
            {
                _ = llmAgent.Chat("Paraphrase the following phrase: " + similarPhrase, SetAIText, OnAIReplyCompleteAndSpeak);
            }
        }

        private void OnAIReplyCompleteAndSpeak()
        {
            AIReplyComplete();
        }

        protected new void AIReplyComplete()
        {
            base.AIReplyComplete();
            TrySpeakCurrentAiText();
        }

        private void TrySpeakCurrentAiText()
        {
            if (ttsSpeakerInput == null)
            {
                ttsSpeakerInput = GetComponent<TextToSpeech>();
            }

            string reply = AIText != null ? AIText.text : string.Empty;
            if (ttsSpeakerInput != null && !string.IsNullOrWhiteSpace(reply) && reply != "...")
            {
                ttsSpeakerInput.SpeakText(reply);
            }
        }

        public void CancelRequests()
        {
            llmAgent.CancelRequests();
            AIReplyComplete();
        }

        protected override void CheckLLMs(bool debug)
        {
            base.CheckLLMs(debug);
            CheckLLM(llmAgent, debug);
        }
    }
}