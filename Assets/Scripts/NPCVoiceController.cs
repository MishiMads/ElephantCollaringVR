using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

public class NPCVoiceController : MonoBehaviour
{
    [Header("Script References")]
    public VoiceInput voiceInput;
    public RAGDatabase ragDatabase;
    public LLMManager llmManager;
    public TTSSystem ttsSystem;
    public NPCSpeaker npcSpeaker;

    [Header("Player Input - Type Here in Play Mode!")]
    [SerializeField]
    [TextArea(3, 5)]
    public string playerInput = "";

    void Update()
    {
        // Press K or Enter to send input to NPC
        if (Input.GetKeyDown(KeyCode.K) || Input.GetKeyDown(KeyCode.Return))
        {
            _ = ProcessPlayerInput();
        }
    }

    private string GetRAGCategory(string userText)
    {
        string lowerText = userText.ToLower().Trim();

        // Match your RAGDatabase keys from npc_responses.txt
        if (lowerText.Contains("mission") || lowerText.Contains("task") || lowerText.Contains("objective"))
            return "MISSION";
        if (lowerText.Contains("elephant") || lowerText.Contains("target"))
            return "ELEPHANT";
        if (lowerText.Contains("forest") || lowerText.Contains("where") || lowerText.Contains("location"))
            return "FOREST";
        if (lowerText.Contains("stealth") || lowerText.Contains("quiet") || lowerText.Contains("silent"))
            return "STEALTH";

        return "GENERAL";  // Fallback
    }

    async Task ProcessPlayerInput()
    {
        string input = playerInput.Trim();
        if (string.IsNullOrEmpty(input))
        {
            Debug.LogWarning("**Type something in the Inspector first!**");
            return;
        }

        Debug.Log($"**Player:** {input}");
        await Task.Delay(2000);

        // Keyword detection + RAG filtering (no duplicate prevention)
        string keyword = DetectKeyword(input);
        var relevantChunks = ragDatabase.GetTopK(input, 3);
        var keywordChunks = relevantChunks.Where(chunk =>
            chunk.ToLower().Contains(keyword)).ToList();

        if (keywordChunks.Count > 0)
        {
            foreach (string chunk in keywordChunks)
            {
                Debug.Log($"**NPC:** {chunk}");
                await Task.Delay(2000);

                AudioClip speechClip = await ttsSystem.Synthesize(chunk);
                npcSpeaker.PlaySpeech(speechClip);
                await Task.Delay(1000);
            }
        }
        else
        {
            Debug.Log("**NPC:** I don't have information about that topic.");
        }
    }

    string DetectKeyword(string input)
    {
        input = input.ToLower();

        if (input.Contains("mission") || input.Contains("task") || input.Contains("objective"))
            return "mission";
        if (input.Contains("elephant"))
            return "elephant";
        if (input.Contains("forest") || input.Contains("location"))
            return "forest";
        return "general";
    }

    private async void OnTranscriptionReady(string text)
    {
        Debug.Log($"**Player:** {text}");

        // RAG → Get category key
        string ragCategory = GetRAGCategory(text);

        // LLMManager looks up response by category
        string npcReply = await llmManager.GenerateReply(ragCategory);

        // TTS speaks the response
        AudioClip speechClip = await ttsSystem.Synthesize(npcReply);
        npcSpeaker.PlaySpeech(speechClip);
    }

}
