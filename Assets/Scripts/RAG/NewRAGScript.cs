using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using UnityEngine.UI;
using LLMUnity;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace LLMUnitySamples
{
    public class NewRAGScript : MonoBehaviour
    {
        public RAG rag;
        public InputField playerText;
        public Text AIText;
        public TextAsset HamletText;
        List<string> phrases;
        string ragPath = "TRAINING REFERENCE MANUAL.zip";
        public TextAsset sourceText;   // replace HamletText
        [SerializeField] int chunkSize = 400;
        [SerializeField] int chunkOverlap = 50;

        async void Start()
        {
            CheckLLMs(false);

            if (sourceText == null)
            {
                Debug.LogError("No source text assigned.");
                return;
            }

            ragPath = $"{sourceText.name}_RAG.zip";

            playerText.interactable = false;
            LoadPhrases();
            await CreateEmbeddings();
            playerText.onSubmit.AddListener(onInputFieldSubmit);
            AIReplyComplete();
        }

        public void LoadPhrases()
        {
            phrases = ChunkText(sourceText != null ? sourceText.text : "");
        }
        
        
        private List<string> ChunkText(string text)
        {
            var chunks = new List<string>();
            if (string.IsNullOrWhiteSpace(text)) return chunks;

            // normalize whitespace
            text = Regex.Replace(text, @"\s+", " ").Trim();

            int step = Mathf.Max(1, chunkSize - chunkOverlap);
            for (int i = 0; i < text.Length; i += step)
            {
                int len = Mathf.Min(chunkSize, text.Length - i);
                string chunk = text.Substring(i, len).Trim();
                if (!string.IsNullOrEmpty(chunk))
                    chunks.Add(chunk);

                if (i + len >= text.Length) break;
            }

            return chunks;
        }

        public async Task CreateEmbeddings()
        {
            bool loaded = await rag.Load(ragPath);
            if (!loaded)
            {
    #if UNITY_EDITOR
                // build the embeddings
                playerText.text += $"Creating Embeddings (only once)...\n";
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                foreach (string phrase in phrases) await rag.Add(phrase);
                stopwatch.Stop();
                Debug.Log($"embedded {rag.Count()} phrases in {stopwatch.Elapsed.TotalMilliseconds / 1000f} secs");
                // store the embeddings
                rag.Save(ragPath);
                
    #else
                // if in play mode throw an error
                Debug.LogError("The embeddings could not be found!");
    #endif
            }
        }

        protected virtual async void onInputFieldSubmit(string message)
        {
            playerText.interactable = false;
            AIText.text = "...";
            (string[] similarPhrases, float[] distances) = await rag.Search(message, 1);
            AIText.text = similarPhrases[0];

            await Task.Yield();
            AIReplyComplete();
        }

        public void SetAIText(string text)
        {
            AIText.text = text;
        }

        public void AIReplyComplete()
        {
            playerText.text = "";
            playerText.interactable = true;
            playerText.Select();
        }

        public void ExitGame()
        {
            Debug.Log("Exit button clicked");
            Application.Quit();
        }

        protected void CheckLLM(LLMClient llmClient, bool debug)
        {
            if (!llmClient.remote && llmClient.llm != null && llmClient.llm.model == "")
            {
                string error = $"Please select a llm model in the {llmClient.llm.gameObject.name} GameObject!";
                if (debug) Debug.LogWarning(error);
                else Debug.LogError(error);
            }
        }

        protected virtual void CheckLLMs(bool debug)
        {
            CheckLLM(rag.search.llmEmbedder, debug);
        }

        bool onValidateWarning = true;
        void OnValidate()
        {
            if (onValidateWarning)
            {
                CheckLLMs(true);
                onValidateWarning = false;
            }
        }
    }
}
