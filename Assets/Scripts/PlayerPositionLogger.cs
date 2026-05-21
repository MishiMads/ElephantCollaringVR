using UnityEngine;
using System.Collections;
using System.IO;
using System.Text;
using System;

public class PlayerPositionLogger : MonoBehaviour
{
    [Header("References")]
    public Transform playerTransform;

    [Header("Recording Settings")]
    public float sampleInterval = 0.5f;

    [Header("Output Settings")]
    public bool useCustomFolder = true;
    public string customFolder = @"C:\Users\Nikla\Documents\GitkrakenMapper\ElephantCollaringVR\Assets\PositionLogging";

    private StreamWriter _writer;
    private float _sessionStartTime;

    private void Start()
    {
        Debug.Log("[PlayerPositionLogger] Initializing Player Position Logger");

        if (playerTransform == null)
        {
            Debug.LogError("[PlayerPositionLogger] Player Transform is NOT assigned!");
            enabled = false;
            return;
        }

        Debug.Log($"[PlayerPositionLogger] Tracking object: {playerTransform.name}");

        // Choose folder
        string folder = useCustomFolder
            ? customFolder
            : Application.persistentDataPath;

        Debug.Log($"[PlayerPositionLogger] Using folder: {folder}");

        // Ensure directory exists
        try
        {
            Directory.CreateDirectory(folder);
        }
        catch (Exception e)
        {
            Debug.LogError($"[PlayerPositionLogger] Failed to create directory: {e}");
            enabled = false;
            return;
        }

        // ✅ Get a unique file name (no overwrite)
        string path = GetUniqueFilePath(folder, "Location", ".txt");
        Debug.Log($"[PlayerPositionLogger] FULL PATH: {path}");

        // Try opening file
        try
        {
            _writer = new StreamWriter(path, false, Encoding.UTF8);
            _writer.WriteLine("Time(s)\tX\tY\tZ");
            _writer.Flush();
        }
        catch (Exception e)
        {
            Debug.LogError($"[PlayerPositionLogger] Failed to open file: {e}");
            enabled = false;
            return;
        }

        _sessionStartTime = Time.realtimeSinceStartup;

        StartCoroutine(LogLoop());
    }

    private IEnumerator LogLoop()
    {
        Debug.Log("[PlayerPositionLogger] LogLoop started");

        while (true)
        {
            yield return new WaitForSecondsRealtime(sampleInterval);

            if (_writer == null)
            {
                Debug.LogError("[PlayerPositionLogger] Writer is NULL!");
                yield break;
            }

            float sessionTime = Time.realtimeSinceStartup - _sessionStartTime;
            Vector3 pos = playerTransform.position;

            string line = $"{sessionTime:F2}\t{pos.x:F3}\t{pos.y:F3}\t{pos.z:F3}";

            _writer.WriteLine(line);

            
        }
    }

    private void OnDestroy()
    {
        Debug.Log("[PlayerPositionLogger] OnDestroy called");

        if (_writer != null)
        {
            _writer.WriteLine($"Session ended: {DateTime.Now}");
            _writer.Flush();
            _writer.Close();
            _writer = null;
        }
    }

    // ✅ Helper: generates Location_1.txt, Location_2.txt, etc.
    private string GetUniqueFilePath(string folder, string baseName, string extension)
    {
        int index = 1;
        string path;

        do
        {
            string fileName = $"{baseName}_{index}{extension}";
            path = Path.Combine(folder, fileName);
            index++;
        }
        while (File.Exists(path));

        return path;
    }
}