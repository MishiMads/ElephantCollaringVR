using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class HeatmapFromFile : MonoBehaviour
{
    public string filePath = @"C:\Users\Nikla\Documents\GitkrakenMapper\ElephantCollaringVR\Assets\PositionLogging\Location.txt";

    public int textureSize = 256;
    public float worldSize = 5f; // MUST match your plane size

    public Renderer targetRenderer;

    void Start()
    {
        List<Vector3> positions = LoadPositions();

        if (positions.Count == 0)
        {
            Debug.LogError("No positions loaded!");
            return;
        }

        Texture2D tex = GenerateHeatmap(positions);

        targetRenderer.material.mainTexture = tex;
    }

    List<Vector3> LoadPositions()
    {
        List<Vector3> positions = new List<Vector3>();

        if (!File.Exists(filePath))
        {
            Debug.LogError("File not found: " + filePath);
            return positions;
        }

        string[] lines = File.ReadAllLines(filePath);

        for (int i = 1; i < lines.Length; i++) // skip header
        {
            if (lines[i].StartsWith("Session")) continue;

            string[] parts = lines[i].Split('\t');

            float x = float.Parse(parts[1].Replace(",", "."));
            float y = float.Parse(parts[2].Replace(",", "."));
            float z = float.Parse(parts[3].Replace(",", "."));

            positions.Add(new Vector3(x, y, z));
        }

        Debug.Log($"Loaded {positions.Count} positions");
        return positions;
    }

    Texture2D GenerateHeatmap(List<Vector3> positions)
    {
        float[,] heat = new float[textureSize, textureSize];

        foreach (var pos in positions)
        {
            int x = Mathf.Clamp(
                (int)((pos.x + worldSize / 2f) / worldSize * textureSize),
                0, textureSize - 1);

            int y = Mathf.Clamp(
                (int)((pos.z + worldSize / 2f) / worldSize * textureSize),
                0, textureSize - 1);

            heat[x, y] += 1f;
        }

        float max = 0f;
        foreach (float v in heat)
            if (v > max) max = v;

        Texture2D tex = new Texture2D(textureSize, textureSize);

        for (int x = 0; x < textureSize; x++)
        {
            for (int y = 0; y < textureSize; y++)
            {
                float v = heat[x, y] / max;

                Color c = Color.Lerp(Color.blue, Color.red, v);
                c.a = v; // transparency

                tex.SetPixel(x, y, c);
            }
        }

        tex.Apply();
        return tex;
    }
}