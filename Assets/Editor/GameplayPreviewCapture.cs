using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameplayPreviewCapture
{
    const string Flag = "GameplayPreviewCapture.active";
    const string ScenePath = "Assets/Scenes/SampleScene.unity";
    static bool shot1, shot2;

    [MenuItem("Tools/Undead Courier/Capture Gameplay Preview")]
    internal static void Run()
    {
        Scene scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        SessionState.SetBool(Flag, true);
        shot1 = shot2 = false;
        logBuf.Clear();
        Application.logMessageReceived -= OnLog;
        Application.logMessageReceived += OnLog;
        if (!EditorApplication.isPlaying)
            EditorApplication.isPlaying = true;
        else
            Arm();
    }

    [InitializeOnLoadMethod]
    static void Hook()
    {
        if (SessionState.GetBool(Flag, false))
        {
            Application.logMessageReceived -= OnLog;
            Application.logMessageReceived += OnLog;
        }
        EditorApplication.playModeStateChanged += s =>
        {
            if (s == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Flag, false))
                Arm();
        };
    }

    static readonly System.Text.StringBuilder logBuf = new System.Text.StringBuilder();

    static void Arm()
    {
        Application.runInBackground = true;
        Application.logMessageReceived -= OnLog;
        Application.logMessageReceived += OnLog;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
    }

    static void OnLog(string condition, string stackTrace, LogType type)
    {
        logBuf.AppendLine($"[{type}] {condition}");
        if (type == LogType.Exception || type == LogType.Error)
            logBuf.AppendLine(stackTrace);
    }

    static void Tick()
    {
        if (!EditorApplication.isPlaying) return;
        float dt = Time.time;
        if (!shot1 && dt > 3) { shot1 = true; ScreenCapture.CaptureScreenshot("Logs/gameplay_preview_early.png"); }
        if (!shot2 && dt > 10)
        {
            shot2 = true;
            ScreenCapture.CaptureScreenshot("Logs/gameplay_preview_late.png");
        }
        if (dt > 12)
        {
            EditorApplication.update -= Tick;
            Application.logMessageReceived -= OnLog;
            System.IO.File.WriteAllText("Logs/gameplay_play.log", logBuf.ToString());
            SessionState.SetBool(Flag, false);
            EditorApplication.isPlaying = false;
        }
    }
}
