using System.IO;
using UnityEngine;

public static class TestSessionFolder
{
    private const string RootFolder = @"C:\Uni\MED8\ElephantCollaringVR\Assets\Tests";

    private static string _currentSessionFolder;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession()
    {
        _currentSessionFolder = null;
    }

    public static string GetSessionFolder()
    {
        if (!string.IsNullOrEmpty(_currentSessionFolder))
            return _currentSessionFolder;

        Directory.CreateDirectory(RootFolder);

        int participantNumber = 1;

        while (Directory.Exists(Path.Combine(RootFolder, $"Test participant {participantNumber}")))
        {
            participantNumber++;
        }

        _currentSessionFolder = Path.Combine(RootFolder, $"Test participant {participantNumber}");
        Directory.CreateDirectory(_currentSessionFolder);

        Debug.Log($"[TestSessionFolder] Saving test data to: {_currentSessionFolder}");

        return _currentSessionFolder;
    }
}