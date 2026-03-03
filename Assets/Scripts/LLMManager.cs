using UnityEngine;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;

public class LLMManager : MonoBehaviour
{
    [Header("Responses File")]
    public string responsesFile = "npc_responses.txt";  // StreamingAssets

    private Dictionary<string, string> responses;

    void Start()
    {
        LoadResponses();
    }

    void LoadResponses()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, responsesFile);

#if UNITY_EDITOR || !UNITY_ANDROID
        if (File.Exists(filePath))
        {
            string content = File.ReadAllText(filePath);
            ParseResponses(content);
            Debug.Log($"✅ Loaded {responses.Count} responses from {responsesFile}");
        }
        else
        {
            Debug.LogWarning($"❌ {responsesFile} not found at {filePath}");
        }
#endif
    }

    void ParseResponses(string content)
    {
        responses = new Dictionary<string, string>();
        string[] lines = content.Split('\n');

        foreach (string line in lines)
        {
            string trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || !trimmed.Contains(":")) continue;

            string[] parts = trimmed.Split(':', 2);
            if (parts.Length == 2)
            {
                string key = parts[0].Trim().ToUpper();
                string value = parts[1].Trim();
                responses[key] = value;
            }
        }
    }

    public async Task<string> GenerateReply(string prompt)
    {
        // RAG already gives us the right KEY - just return the text
        // Your NPCVoiceController passes the RAG category as prompt
        string key = prompt.ToUpper().Trim();  // "MISSION", "ELEPHANT", etc.

        if (responses != null && responses.TryGetValue(key, out string response))
        {
            Debug.Log($"🤖 [{key}] {response}");
            return response;
        }

        Debug.LogWarning($"❌ No response for key: {key}");
        return "Focus on your primary objective.";
    }
}
