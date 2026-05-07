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

    private StreamWriter _writer;
    private float _sessionStartTime;

    private void Start()
    {
        if (playerTransform == null)
        {
            Debug.LogWarning("[PlayerPositionLogger] No player transform assigned — disabling.");
            enabled = false;
            return;
        }

        string folder = TestSessionFolder.GetSessionFolder();
        string path = Path.Combine(folder, "Location.txt");

        Debug.Log($"[PlayerPositionLogger] Saving location data to: {path}");

        _writer = new StreamWriter(path, false, Encoding.UTF8);
        _writer.WriteLine("Time(s)\tX\tY\tZ");
        _writer.Flush();

        _sessionStartTime = Time.realtimeSinceStartup;

        StartCoroutine(LogLoop());
    }

    private IEnumerator LogLoop()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(sampleInterval);

            float sessionTime = Time.realtimeSinceStartup - _sessionStartTime;
            Vector3 pos = playerTransform.position;

            _writer.WriteLine($"{sessionTime:F2}\t{pos.x:F3}\t{pos.y:F3}\t{pos.z:F3}");
            _writer.Flush();
        }
    }

    private void OnDestroy()
    {
        if (_writer != null)
        {
            _writer.WriteLine($"Session ended: {DateTime.Now}");
            _writer.Close();
            _writer = null;
        }
    }
}