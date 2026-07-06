using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BloodDecalPreview
{
    const string Flag = "BloodDecalPreview.active";
    const string ScenePath = "Assets/Scenes/SampleScene.unity";
    static bool didSpawn, didShot, didFinish;
    static float spawnTime = -1f;
    static Transform playerTransform;
    static Vector3 groundPoint;
    static readonly System.Text.StringBuilder log = new System.Text.StringBuilder();

    [MenuItem("Tools/Undead Courier/Capture Blood Decal Preview")]
    internal static void Run()
    {
        Scene scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath)
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        SessionState.SetBool(Flag, true);
        didSpawn = didShot = didFinish = false;
        spawnTime = -1f;
        log.Clear();

        if (!EditorApplication.isPlaying) EditorApplication.isPlaying = true;
        else Arm();
    }

    [InitializeOnLoadMethod]
    static void Hook()
    {
        EditorApplication.playModeStateChanged += s =>
        {
            if (s == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Flag, false))
                Arm();
        };
    }

    static void Arm()
    {
        Application.runInBackground = true;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
    }

    static void Tick()
    {
        if (!EditorApplication.isPlaying) return;
        float t = Time.time;

        if (!didSpawn && t > 3f)
        {
            didSpawn = true;
            SpawnCluster();
        }
        if (didSpawn && !didShot && t > spawnTime + 1.0f && spawnTime > 0f)
        {
            didShot = true;
            ScreenCapture.CaptureScreenshot("Logs/decal_preview.png");
        }
        if (didShot && !didFinish && t > spawnTime + 1.6f)
        {
            didFinish = true;
            Finish();
        }
        if (t > 14f && !didFinish)
        {
            log.AppendLine("[Warning] timed out");
            didFinish = true;
            Finish();
        }
    }

    static void SpawnCluster()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            MouseMovement mm = Object.FindFirstObjectByType<MouseMovement>();
            if (mm != null) player = mm.gameObject;
        }
        if (player == null) { log.AppendLine("[Error] no player"); Finish(); return; }
        playerTransform = player.transform;

        Player p = Object.FindFirstObjectByType<Player>();
        if (p != null)
        {
            FieldInfo inv = typeof(Player).GetField("isInvulnerable", BindingFlags.NonPublic | BindingFlags.Instance);
            if (inv != null) inv.SetValue(p, true);
            FieldInfo bs = typeof(Player).GetField("bloodyScreen", BindingFlags.Public | BindingFlags.Instance);
            if (bs != null) { GameObject g = bs.GetValue(p) as GameObject; if (g != null) g.SetActive(false); }
        }

        Vector3 origin = playerTransform.position + Vector3.up * 0.5f;
        if (!TryFindGround(origin, 10f, out RaycastHit gh))
        {
            log.AppendLine("[Error] no ground under player");
            Finish();
            return;
        }
        groundPoint = gh.point + playerTransform.forward * 2.0f;
        if (TryFindGround(groundPoint + Vector3.up * 1f, 4f, out RaycastHit g2))
        {
            groundPoint = g2.point;
            log.AppendLine($"[Diag] ground collider={g2.collider.GetType().Name} name={g2.collider.name} normalY={g2.normal.y:F2}");
            BloodDecalPool.SpawnDeathPool(groundPoint, g2.normal);
            BloodDecalPool.SpawnHitNoThrottle(groundPoint + playerTransform.right * 0.5f, g2.normal, playerTransform.forward);
            BloodDecalPool.SpawnHitNoThrottle(groundPoint - playerTransform.right * 0.5f, g2.normal, playerTransform.forward);
        }
        else
        {
            log.AppendLine("[Error] no ground at cluster point");
            Finish();
            return;
        }

        Camera cam = Camera.main;
        if (cam != null)
        {
            Vector3 camPos = groundPoint - playerTransform.forward * 0.6f + Vector3.up * 1.4f;
            CharacterController cc = playerTransform.GetComponent<CharacterController>();
            if (cc != null) { cc.enabled = false; playerTransform.position = camPos - (cam.transform.position - playerTransform.position); cc.enabled = true; }
            Vector3 toGround = groundPoint - cam.transform.position;
            float yaw = Mathf.Atan2(toGround.x, toGround.z) * Mathf.Rad2Deg;
            float pitch = -Mathf.Atan2(toGround.y, new Vector2(toGround.x, toGround.z).magnitude) * Mathf.Rad2Deg;
            MouseMovement mm = playerTransform.GetComponent<MouseMovement>();
            if (mm != null)
            {
                FieldInfo xr = typeof(MouseMovement).GetField("xRotation", BindingFlags.NonPublic | BindingFlags.Instance);
                FieldInfo yr = typeof(MouseMovement).GetField("yRotation", BindingFlags.NonPublic | BindingFlags.Instance);
                if (xr != null) xr.SetValue(mm, pitch);
                if (yr != null) yr.SetValue(mm, yaw);
            }
            playerTransform.localRotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        var projectors = Object.FindObjectsByType<UnityEngine.Rendering.Universal.DecalProjector>(FindObjectsSortMode.None);
        log.AppendLine($"[Diag] projectors={projectors.Length} groundPoint={groundPoint}");
        spawnTime = Time.time;
    }

    static bool TryFindGround(Vector3 origin, float distance, out RaycastHit hit)
    {
        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, distance, ~0, QueryTriggerInteraction.Ignore);
        hit = default;
        bool found = false;
        float bestDistance = -1f;
        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit candidate = hits[i];
            if (candidate.normal.y < 0.72f) continue;
            if (candidate.collider.GetComponentInParent<PlayerMovement>() != null) continue;
            if (candidate.collider.GetComponentInParent<Enemy>() != null) continue;
            if (candidate.collider.name.Contains("Ammo")) continue;
            if (candidate.distance > bestDistance)
            {
                hit = candidate;
                bestDistance = candidate.distance;
                found = true;
            }
        }
        return found;
    }

    static void Finish()
    {
        EditorApplication.update -= Tick;
        System.IO.File.WriteAllText("Logs/decal_preview.log", log.ToString());
        SessionState.SetBool(Flag, false);
        EditorApplication.isPlaying = false;
    }
}
