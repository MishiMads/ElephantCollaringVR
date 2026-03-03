using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class RAGDatabase : MonoBehaviour
{
    [Header("RAG Text File")]
    public string ragFile = "Elephant Conservation experience.txt";  // StreamingAssets/phinda_lore.txt

    private List<string> loreChunks = new List<string>();

    void Start()
    {
        LoadLoreFromFile();
    }

    void LoadLoreFromFile()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, ragFile);

#if UNITY_EDITOR || !UNITY_ANDROID
        if (File.Exists(filePath))
        {
            string content = File.ReadAllText(filePath);
            loreChunks = ParseChunks(content);
            Debug.Log($"✅ Loaded {loreChunks.Count} lore chunks from {ragFile}");
        }
        else
        {
            Debug.LogError($"❌ {ragFile} not found at {filePath}");
        }
#else
        StartCoroutine(LoadStreamingAsset(filePath));
#endif
    }

    List<string> ParseChunks(string content)
    {
        List<string> chunks = new List<string>();
        string[] paragraphs = content.Split("/n/n");

        foreach (string para in paragraphs)
        {
            string trimmed = para.Trim();
            if (trimmed.Length > 20)
                chunks.Add(trimmed);
        }
        return chunks;
    }

    public List<string> GetTopK(string query, int k)
    {
        string lowerQuery = query.ToLower();
        List<string> matches = new List<string>();

        foreach (string chunk in loreChunks)
        {
            string lowerChunk = chunk.ToLower();

            if (lowerQuery.Contains("collar") && (lowerChunk.Contains("collar") || lowerChunk.Contains("gps")))
                matches.Add(chunk);
            else if (lowerQuery.Contains("contracept") && lowerChunk.Contains("contracept"))
                matches.Add(chunk);
            else if (lowerQuery.Contains("elephant") && lowerChunk.Contains("elephant"))
                matches.Add(chunk);
            else if (lowerQuery.Contains("phinda") && lowerChunk.Contains("phinda"))
                matches.Add(chunk);
        }

        // NO FALLBACK - return only matches (even if empty)
        return matches.Take(k).ToList();
    }

    IEnumerator LoadStreamingAsset(string filePath)
    {
        UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequest.Get(filePath);
        yield return www.SendWebRequest();

        if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
            loreChunks = ParseChunks(www.downloadHandler.text);
            Debug.Log($"✅ Loaded {loreChunks.Count} chunks (Quest mode)");
        }
    }
}
