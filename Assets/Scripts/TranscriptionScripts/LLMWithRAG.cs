using LLMUnity;
using Meta.WitAi.TTS.Utilities;
using Meta.WitAi.TTS.UX;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace LLMUnitySamples
{
    public class LLMWithRAG : RAGSample
    {
        public LLMAgent llmAgent;
        [SerializeField] private TextToSpeech ttsSpeakerInput;
        [SerializeField] private int topK = 3;
        [SerializeField] private int maxContextChars = 2400;
        private bool replySent = false;

        public ConversationManager conversationManager;
        public TTSSpeaker speaker;

        private bool _ragReady;
        private Task _initTask;

        private bool isGenerating = false;

        private List<string> currentContextChunks = new List<string>();

        async void Start()
        {
            // Set ragPath based on sourceText before loading embeddings
            if (sourceText != null)
                ragPath = $"{sourceText.name}_RAG.zip";

            // Ensure chunk list + embedding cache path are prepared
            LoadPhrases();

            if (playerText != null) playerText.interactable = false;
            _initTask = CreateEmbeddings();
            await _initTask;

            _ragReady = rag != null && rag.Count() > 0;
            Debug.Log($"LLMWithRAG: RAG ready={_ragReady}, count={(rag == null ? 0 : rag.Count())}");

            if (!_ragReady && AIText != null)
                AIText.text = "RAG index is empty. Check knowledgeText and embedding model setup.";

            if (playerText != null) playerText.interactable = true;
        }

        public async void SubmitExternalInput(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            if (_initTask != null && !_initTask.IsCompleted)
                await _initTask;

            if (!_ragReady)
            {
                Debug.LogError("LLMWithRAG: submit blocked because RAG is not ready.");
                if (AIText != null) AIText.text = "RAG is not ready.";
                AIReplyComplete();
                return;
            }

            onInputFieldSubmit(message);
            currentContextChunks.Add("User: " + message);
        }

        public async void SubmitDirectLLM(string message)
        {
            if (llmAgent == null) return;

            // 🚫 PREVENT DUPLICATES
            if (isGenerating)
            {
                Debug.Log("LLM already generating, skipping...");
                return;
            }

            isGenerating = true;

            string response = "";

            await llmAgent.Chat(message, token =>
            {
                if (!string.IsNullOrEmpty(token))
                {
                    response = token;
                    if (AIText != null) AIText.text = response;
                }
            });

            isGenerating = false;

            AIReplyComplete();
        }

        protected override async void onInputFieldSubmit(string message)
        {

            // 🚫 BLOCK LLM HERE
            

            if (string.IsNullOrWhiteSpace(message))
            {
                AIReplyComplete();
                return;
            }

            if (rag == null || rag.Count() == 0)
            {
                Debug.LogError("LLMWithRAG: rag is null or empty.");
                AIText.text = "RAG not configured.";
                AIReplyComplete();
                return;
            }

            if (llmAgent == null)
            {
                AIText.text = "LLM agent not configured.";
                AIReplyComplete();
                return;
            }

            playerText.interactable = false;
            AIText.text = "...";

            var (similarPhrases, _) = await rag.Search(message, Mathf.Max(1, topK));
            Debug.Log($"LLMWithRAG: query='{message}', topK={topK}, hits={(similarPhrases == null ? 0 : similarPhrases.Length)}");

            if (similarPhrases == null || similarPhrases.Length == 0)
            {
                AIText.text = "No relevant context found.";
                AIReplyComplete();
                return;
            }

            var contextBuilder = new StringBuilder();
            int used = 0;
            for (int i = 0; i < similarPhrases.Length; i++)
            {
                var chunk = similarPhrases[i]?.Trim();
                if (string.IsNullOrWhiteSpace(chunk)) continue;

                int addLen = chunk.Length + 16;
                if (used + addLen > maxContextChars) break;

                contextBuilder.AppendLine($"[Chunk {i + 1}]");
                contextBuilder.AppendLine(chunk);
                contextBuilder.AppendLine();
                used += addLen;
            }

            string context = contextBuilder.ToString().Trim();
            if (string.IsNullOrWhiteSpace(context))
            {
                AIText.text = "No relevant context found.";
                AIReplyComplete();
                return;
            }

            string memory = string.Join("\n", currentContextChunks);
            string prompt =
                @"You are a retrieval-based assistant.

                Use ONLY the information inside <CONTEXT>.
                If the answer is not contained in the context, reply exactly:
                I don't know.

                Keep answers concise.
                Do not continue the conversation.
                Do not roleplay.
                Do not generate dialogue.

                <CHAT_HISTORY>
                " + memory + @"
                </CHAT_HISTORY>

                <CONTEXT>
                " + context + @"
                </CONTEXT>

                <QUESTION>
                " + message + @"
                </QUESTION>

                <ANSWER>";

            string response = "";
            await llmAgent.Chat(prompt, token =>
            {

                if (!string.IsNullOrEmpty(token))
                {
                    response = token;          // ← assign, not append
                    if (AIText != null) AIText.text = response;
                }
            });

            

            if (string.IsNullOrWhiteSpace(response))
                AIText.text = "I don't know based on the provided context.";

            if (conversationManager == null || conversationManager.allowLLM)
            {
                AIReplyComplete();
            }
            else
            {
                Debug.Log("Skipped AIReplyComplete (LLM disabled)");
            }
            
        }

        protected new void AIReplyComplete()
        {
            if (replySent)
            {
                Debug.Log("AIReplyComplete skipped (already sent)");
                return;
            }

            replySent = true;

            base.AIReplyComplete();

            if (conversationManager != null)
            {
                conversationManager.OnLLMResponseReady(AIText.text);
            }

            // reset next frame
            StartCoroutine(ResetReplyFlag());
        }

        IEnumerator ResetReplyFlag()
        {
            yield return null;
            replySent = false;
        }

        private void TrySpeakCurrentAiText()
        {
            

            string reply = AIText != null ? AIText.text : string.Empty;

            if (speaker != null && !string.IsNullOrWhiteSpace(reply) && reply != "...")
            {
                speaker.Stop(); // ✅ now works
                speaker.Speak(reply);
            }
        }

        public void CancelRequests()
        {
            llmAgent?.CancelRequests();

            // 🔥 CRITICAL: clear any pending response
            if (AIText != null) AIText.text = "";

            // 🔥 prevent AIReplyComplete from speaking
            StopAllCoroutines();
        }

        protected override void CheckLLMs(bool debug)
        {
            base.CheckLLMs(debug);
            if (llmAgent != null) CheckLLM(llmAgent, debug);
        }
    }
}