using UnityEditor;
using UnityEngine;

public static class MenuPreviewCapture
{
    const string Flag = "MenuPreviewCapture.active";
    static bool shot1, shot2;

    [MenuItem("Tools/Undead Courier/Exit Play Mode")]
    static void ExitPlay()
    {
        EditorApplication.isPlaying = false;
    }

    [MenuItem("Tools/Undead Courier/Capture Menu Preview")]
    static void Run()
    {
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

    static void DumpTMPState()
    {
        logBuf.AppendLine("=== TMP STATE DUMP ===");
        foreach (var tmp in Object.FindObjectsByType<TMPro.TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            float groupAlpha = 1f;
            for (var t = tmp.transform; t != null; t = t.parent)
            {
                var g = t.GetComponent<CanvasGroup>();
                if (g != null) groupAlpha *= g.alpha;
            }
            var cr = tmp.canvasRenderer;
            logBuf.AppendLine(
                $"{GetPath(tmp.transform)} | text='{tmp.text}' | activeEnabled={tmp.isActiveAndEnabled} | " +
                $"font={(tmp.font ? tmp.font.name : "NULL")} | color={tmp.color} | groupAlpha={groupAlpha} | " +
                $"crAlpha={(cr ? cr.GetAlpha().ToString() : "?")} | crCull={(cr ? cr.cull.ToString() : "?")} | " +
                $"verts={(tmp.mesh ? tmp.mesh.vertexCount : -1)} | rect={((RectTransform)tmp.transform).rect} | " +
                $"pos={tmp.transform.position} | shader={(tmp.fontSharedMaterial ? tmp.fontSharedMaterial.shader.name + "/sup=" + tmp.fontSharedMaterial.shader.isSupported : "NULL")}");
        }
        var canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas != null)
            logBuf.AppendLine($"Canvas mode={canvas.renderMode} enabled={canvas.enabled} scale={canvas.transform.localScale}");
    }

    static string GetPath(Transform t)
    {
        string p = t.name;
        while (t.parent != null) { t = t.parent; p = t.parent == null ? t.name + "/" + p : t.name + "/" + p; }
        return p;
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
        if (!shot1 && dt > 4) { shot1 = true; ScreenCapture.CaptureScreenshot("Logs/menu_preview_intro.png"); }
        if (!shot2 && dt > 18)
        {
            shot2 = true;
            ScreenCapture.CaptureScreenshot("Logs/menu_preview_rest.png");
            DumpTMPState();
        }
        if (dt > 20)
        {
            EditorApplication.update -= Tick;
            Application.logMessageReceived -= OnLog;
            System.IO.File.WriteAllText("Logs/menu_play.log", logBuf.ToString());
            SessionState.SetBool(Flag, false);
            EditorApplication.isPlaying = false;
        }
    }
}
