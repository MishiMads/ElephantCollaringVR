using UnityEngine;
using System.Collections.Generic;

public class RAGDatabase : MonoBehaviour
{
    // Will hold your lore chunks and embeddings later
    public List<string> loreChunks = new List<string>();

    public List<string> GetTopK(string query, int k)
    {
        // Stub: returns first K chunks for now
        return loreChunks.GetRange(0, Mathf.Min(k, loreChunks.Count));
    }
}
