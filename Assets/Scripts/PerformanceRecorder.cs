using UnityEngine;
using UnityEngine.Profiling;
using System;
using System.Collections;
using System.IO;
using System.Text;

public class PerformanceRecorder : MonoBehaviour
{
    [Header("Recording Settings")]
    [Tooltip("How often the stats are written to the file.")]
    public float sampleInterval = 1.0f;

    [Tooltip("Quest 3 usually runs at 72, 80, 90, or 120 Hz. Used for CPU/GPU budget %.")]
    public float targetRefreshRateHz = 72f;

    [Header("File Settings")]
    [Tooltip("Used in Unity Editor or Windows build only.")]
    public string windowsRootFolder = @"C:\Uni\MED8\ElephantCollaringVR\Assets\Tests";

    [Tooltip("File name inside the participant folder.")]
    public string performanceFileName = "FPS.txt";

    [Header("Options")]
    public bool recordFPS = true;
    public bool recordFrameTime = true;
    public bool recordCpuGpuFrameTime = true;
    public bool recordCpuGpuBudgetPercent = true;
    public bool recordMemory = true;

    private static PerformanceRecorder _instance;

    private StreamWriter _writer;

    private float _sessionStartTime;

    private float _sampleElapsedTime;
    private int _frameCount;

    private double _cpuFrameTimeTotal;
    private double _gpuFrameTimeTotal;
    private int _cpuTimingCount;
    private int _gpuTimingCount;

    private readonly FrameTiming[] _frameTimings = new FrameTiming[1];

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoStart()
    {
        if (_instance != null)
            return;

        GameObject go = new GameObject("Quest Performance Recorder");
        _instance = go.AddComponent<PerformanceRecorder>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        _sessionStartTime = Time.realtimeSinceStartup;

        string sessionFolder = GetOrCreateParticipantFolder();
        string path = Path.Combine(sessionFolder, performanceFileName);

        Debug.Log($"[QuestPerformanceRecorder] Saving performance data to: {path}");

        _writer = new StreamWriter(path, false, Encoding.UTF8);
        WriteHeader();

        StartCoroutine(RecordLoop());
    }

    private void Update()
    {
        float deltaTime = Time.unscaledDeltaTime;

        if (deltaTime <= 0f)
            return;

        _sampleElapsedTime += deltaTime;
        _frameCount++;

        if (recordCpuGpuFrameTime || recordCpuGpuBudgetPercent)
        {
            FrameTimingManager.CaptureFrameTimings();

            uint timingCount = FrameTimingManager.GetLatestTimings(1, _frameTimings);

            if (timingCount > 0)
            {
                FrameTiming timing = _frameTimings[0];

                if (timing.cpuFrameTime > 0)
                {
                    _cpuFrameTimeTotal += timing.cpuFrameTime;
                    _cpuTimingCount++;
                }

                if (timing.gpuFrameTime > 0)
                {
                    _gpuFrameTimeTotal += timing.gpuFrameTime;
                    _gpuTimingCount++;
                }
            }
        }
    }

    private IEnumerator RecordLoop()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(sampleInterval);

            WriteSample();
            ResetSample();
        }
    }

    private void WriteHeader()
    {
        _writer.WriteLine("Quest 3 Performance Recording");
        _writer.WriteLine($"Session started: {DateTime.Now}");
        _writer.WriteLine($"Target refresh rate: {targetRefreshRateHz} Hz");
        _writer.WriteLine(new string('-', 100));

        StringBuilder header = new StringBuilder();

        header.Append("Time(s)");

        if (recordFPS)
            header.Append("\tAvg FPS");

        if (recordFrameTime)
            header.Append("\tAvg Frame Time (ms)");

        if (recordCpuGpuFrameTime)
        {
            header.Append("\tAvg CPU Frame Time (ms)");
            header.Append("\tAvg GPU Frame Time (ms)");
        }

        if (recordCpuGpuBudgetPercent)
        {
            header.Append("\tCPU Budget Used (%)");
            header.Append("\tGPU Budget Used (%)");
        }

        if (recordMemory)
            header.Append("\tMemory Allocated (MB)");

        _writer.WriteLine(header.ToString());
        _writer.Flush();
    }

    private void WriteSample()
    {
        if (_writer == null)
            return;

        float sessionTime = Time.realtimeSinceStartup - _sessionStartTime;

        float avgFPS = _sampleElapsedTime > 0f
            ? _frameCount / _sampleElapsedTime
            : 0f;

        float avgFrameTimeMs = avgFPS > 0f
            ? 1000f / avgFPS
            : 0f;

        double avgCpuFrameTimeMs = _cpuTimingCount > 0
            ? _cpuFrameTimeTotal / _cpuTimingCount
            : 0.0;

        double avgGpuFrameTimeMs = _gpuTimingCount > 0
            ? _gpuFrameTimeTotal / _gpuTimingCount
            : 0.0;

        float frameBudgetMs = targetRefreshRateHz > 0f
            ? 1000f / targetRefreshRateHz
            : 0f;

        double cpuBudgetUsedPercent = frameBudgetMs > 0f && avgCpuFrameTimeMs > 0.0
            ? (avgCpuFrameTimeMs / frameBudgetMs) * 100.0
            : 0.0;

        double gpuBudgetUsedPercent = frameBudgetMs > 0f && avgGpuFrameTimeMs > 0.0
            ? (avgGpuFrameTimeMs / frameBudgetMs) * 100.0
            : 0.0;

        float memoryMB = Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f);

        StringBuilder line = new StringBuilder();

        line.Append($"{sessionTime:F2}");

        if (recordFPS)
            line.Append($"\t{avgFPS:F1}");

        if (recordFrameTime)
            line.Append($"\t{avgFrameTimeMs:F2}");

        if (recordCpuGpuFrameTime)
        {
            line.Append($"\t{avgCpuFrameTimeMs:F2}");
            line.Append($"\t{avgGpuFrameTimeMs:F2}");
        }

        if (recordCpuGpuBudgetPercent)
        {
            line.Append($"\t{cpuBudgetUsedPercent:F1}");
            line.Append($"\t{gpuBudgetUsedPercent:F1}");
        }

        if (recordMemory)
            line.Append($"\t{memoryMB:F1}");

        _writer.WriteLine(line.ToString());
        _writer.Flush();
    }

    private void ResetSample()
    {
        _sampleElapsedTime = 0f;
        _frameCount = 0;

        _cpuFrameTimeTotal = 0.0;
        _gpuFrameTimeTotal = 0.0;
        _cpuTimingCount = 0;
        _gpuTimingCount = 0;
    }

    private string GetOrCreateParticipantFolder()
    {
        string rootFolder = GetRootFolder();

        Directory.CreateDirectory(rootFolder);

        int participantNumber = 1;

        while (Directory.Exists(Path.Combine(rootFolder, $"Test participant {participantNumber}")))
        {
            participantNumber++;
        }

        string participantFolder = Path.Combine(rootFolder, $"Test participant {participantNumber}");
        Directory.CreateDirectory(participantFolder);

        Debug.Log($"[QuestPerformanceRecorder] Test participant folder: {participantFolder}");

        return participantFolder;
    }

    private string GetRootFolder()
    {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        return windowsRootFolder;
#else
        return Path.Combine(Application.persistentDataPath, "Tests");
#endif
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
            _writer?.Flush();
    }

    private void OnApplicationQuit()
    {
        CloseFile();
    }

    private void OnDestroy()
    {
        CloseFile();
    }

    private void CloseFile()
    {
        if (_writer == null)
            return;

        _writer.WriteLine(new string('-', 100));
        _writer.WriteLine($"Session ended: {DateTime.Now}");
        _writer.Flush();
        _writer.Close();
        _writer = null;

        Debug.Log("[QuestPerformanceRecorder] Performance file closed.");
    }
}