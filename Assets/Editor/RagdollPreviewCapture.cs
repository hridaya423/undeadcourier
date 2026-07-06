using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public static class RagdollPreviewCapture
{
    struct ShotCase
    {
        public string name;
        public HumanBodyBones bone;
        public bool headshot;
    }

    const string Flag = "RagdollPreviewCapture.active";
    const string ScenePath = "Assets/Scenes/SampleScene.unity";
    static readonly ShotCase[] ShotCases =
    {
        new ShotCase { name = "chest", bone = HumanBodyBones.Chest, headshot = false },
        new ShotCase { name = "head", bone = HumanBodyBones.Head, headshot = true },
        new ShotCase { name = "leg", bone = HumanBodyBones.LeftUpperLeg, headshot = false }
    };

    static bool didSetup, didKill, didCapture, didFinish;
    static float killTime = -1f;
    static float shotStartTime = -1f;
    static int shotIndex;
    static Enemy target;
    static Transform playerTransform;
    static Vector3 corpsePoint;
    static readonly System.Collections.Generic.HashSet<Enemy> usedTargets = new System.Collections.Generic.HashSet<Enemy>();
    static readonly System.Text.StringBuilder log = new System.Text.StringBuilder();
    static readonly FieldInfo EnemyHpField = typeof(Enemy).GetField("HP", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);

    [MenuItem("Tools/Undead Courier/Capture Ragdoll Preview")]
    internal static void Run()
    {
        Scene scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath) EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        SessionState.SetBool(Flag, true);
        ResetState();
        Application.logMessageReceived -= OnLog;
        Application.logMessageReceived += OnLog;

        if (!EditorApplication.isPlaying) EditorApplication.isPlaying = true;
        else Arm();
    }

    static void ResetState()
    {
        shotIndex = 0;
        usedTargets.Clear();
        log.Clear();
        ResetShotState();
    }

    static void ResetShotState()
    {
        didSetup = didKill = didCapture = didFinish = false;
        killTime = -1f;
        shotStartTime = -1f;
        target = null;
        playerTransform = null;
        corpsePoint = Vector3.zero;
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
            if (s == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Flag, false)) Arm();
        };
    }

    static void Arm()
    {
        Application.runInBackground = true;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
    }

    static void OnLog(string condition, string stackTrace, LogType type)
    {
        log.AppendLine($"[{type}] {condition}");
        if (type == LogType.Exception || type == LogType.Error) log.AppendLine(stackTrace);
    }

    static void Tick()
    {
        if (!EditorApplication.isPlaying) return;

        float t = Time.time;
        if (shotStartTime < 0f) shotStartTime = t;

        try
        {
            if (!didSetup && t > 3f)
            {
                didSetup = true;
                Setup();
            }

            if (didSetup && !didKill && t > 3.4f)
            {
                didKill = true;
                KillTarget();
                killTime = t;
            }

            if (didKill && !didCapture && t > killTime + 4f)
            {
                didCapture = true;
                AimAtCorpse();
                LogRagdollState();
                ScreenCapture.CaptureScreenshot(ScreenshotPathForCurrentShot());
            }

            if (didCapture && !didFinish && t > killTime + 4.7f)
            {
                CompleteShot();
            }

            if (((!didKill && t > shotStartTime + 20f) || (didKill && t > killTime + 8f)) && !didFinish)
            {
                log.AppendLine("[Warning] timed out");
                didFinish = true;
                Finish();
            }
        }
        catch (Exception e)
        {
            log.AppendLine("[Exception] RagdollPreviewCapture: " + e);
            if (!didFinish)
            {
                didFinish = true;
                Finish();
            }
        }
    }

    static void Setup()
    {
        target = FindTarget();
        if (target == null)
        {
            log.AppendLine("[Error] no enemy target");
            Finish();
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null && GlobalReferences.Instance != null) player = GlobalReferences.Instance.player;
        if (player == null)
        {
            log.AppendLine("[Error] no player");
            Finish();
            return;
        }

        playerTransform = player.transform;
        MakePlayerInvulnerable(player);

        Vector3 basePoint = playerTransform.position + playerTransform.forward * 5f + playerTransform.right * 2f;
        if (NavMesh.SamplePosition(basePoint, out NavMeshHit navHit, 8f, NavMesh.AllAreas))
        {
            basePoint = navHit.position;
        }

        MoveTransform(target.transform, basePoint);
        target.transform.rotation = Quaternion.LookRotation(-playerTransform.forward, Vector3.up);

        NavMeshAgent agent = target.GetComponent<NavMeshAgent>();
        if (agent != null && agent.enabled && agent.isOnNavMesh) agent.ResetPath();

        if (EnemyHpField != null) EnemyHpField.SetValue(target, 80);
        corpsePoint = target.transform.position;
        log.AppendLine($"[Diag] case={CurrentShot().name} target={target.name} pos={corpsePoint}");
    }

    static Enemy FindTarget()
    {
        Enemy[] enemies = UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] == null) continue;
            if (usedTargets.Contains(enemies[i])) continue;
            return enemies[i];
        }
        return null;
    }

    static void KillTarget()
    {
        if (target == null) return;

        ShotCase shot = CurrentShot();
        Animator animator = target.GetComponent<Animator>();
        Vector3 hitPoint = target.transform.position + Vector3.up * 1.2f;
        if (animator != null && animator.avatar != null && animator.avatar.isHuman)
        {
            Transform bone = animator.GetBoneTransform(shot.bone);
            if (bone != null) hitPoint = bone.position;
        }

        Vector3 direction = playerTransform != null ? playerTransform.forward : Vector3.forward;
        target.RegisterHitContext(hitPoint, direction, null, shot.headshot);
        target.TakeDamage(1000);
        corpsePoint = target.transform.position;
        log.AppendLine($"[Diag] case={shot.name} killed at={corpsePoint} hit={hitPoint} dir={direction}");
    }

    static void AimAtCorpse()
    {
        if (playerTransform == null) return;

        Vector3 viewDir = playerTransform.right;
        Vector3 standPos = corpsePoint - viewDir * 4.6f + Vector3.up * 2.1f;
        MoveTransform(playerTransform, standPos);

        Vector3 lookPoint = corpsePoint + Vector3.up * 0.35f;
        Vector3 toTarget = lookPoint - Camera.main.transform.position;
        float yaw = Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg;
        float horizontalDist = new Vector2(toTarget.x, toTarget.z).magnitude;
        float pitch = -Mathf.Atan2(toTarget.y, horizontalDist) * Mathf.Rad2Deg;

        MouseMovement mouseMovement = playerTransform.GetComponent<MouseMovement>();
        if (mouseMovement != null)
        {
            FieldInfo xRotField = typeof(MouseMovement).GetField("xRotation", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo yRotField = typeof(MouseMovement).GetField("yRotation", BindingFlags.NonPublic | BindingFlags.Instance);
            if (xRotField != null) xRotField.SetValue(mouseMovement, pitch);
            if (yRotField != null) yRotField.SetValue(mouseMovement, yaw);
        }

        playerTransform.localRotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    static void MoveTransform(Transform transform, Vector3 position)
    {
        CharacterController controller = transform.GetComponent<CharacterController>();
        if (controller != null)
        {
            controller.enabled = false;
            transform.position = position;
            controller.enabled = true;
            return;
        }

        NavMeshAgent agent = transform.GetComponent<NavMeshAgent>();
        if (agent != null && agent.enabled)
        {
            agent.enabled = false;
            transform.position = position;
            agent.enabled = true;
            return;
        }

        transform.position = position;
    }

    static void MakePlayerInvulnerable(GameObject player)
    {
        Player p = player.GetComponent<Player>();
        if (p == null) p = UnityEngine.Object.FindFirstObjectByType<Player>();
        if (p == null) return;

        FieldInfo invField = typeof(Player).GetField("isInvulnerable", BindingFlags.NonPublic | BindingFlags.Instance);
        if (invField != null) invField.SetValue(p, true);

        FieldInfo bsField = typeof(Player).GetField("bloodyScreen", BindingFlags.Public | BindingFlags.Instance);
        GameObject bs = bsField != null ? bsField.GetValue(p) as GameObject : null;
        if (bs != null) bs.SetActive(false);
    }

    static void LogRagdollState()
    {
        if (target == null) return;

        Rigidbody[] bodies = target.GetComponentsInChildren<Rigidbody>(true);
        int dynamic = 0;
        int kinematic = 0;
        for (int i = 0; i < bodies.Length; i++)
        {
            if (bodies[i].isKinematic) kinematic++;
            else dynamic++;
        }

        log.AppendLine($"[Diag] case={CurrentShot().name} bodies={bodies.Length} dynamic={dynamic} kinematic={kinematic}");
    }

    static ShotCase CurrentShot()
    {
        return ShotCases[Mathf.Clamp(shotIndex, 0, ShotCases.Length - 1)];
    }

    static void CompleteShot()
    {
        string fileName = ScreenshotPathForCurrentShot();
        if (CurrentShot().name == "chest" && System.IO.File.Exists(fileName))
        {
            System.IO.File.Copy(fileName, "Logs/ragdoll_preview_chest.png", true);
        }

        if (shotIndex < ShotCases.Length - 1)
        {
            if (target != null) usedTargets.Add(target);
            shotIndex++;
            ResetShotState();
            return;
        }

        didFinish = true;
        Finish();
    }

    static string ScreenshotPathForCurrentShot()
    {
        if (CurrentShot().name == "chest") return "Logs/ragdoll_preview.png";
        return $"Logs/ragdoll_preview_{CurrentShot().name}.png";
    }

    static void Finish()
    {
        EditorApplication.update -= Tick;
        Application.logMessageReceived -= OnLog;
        System.IO.File.WriteAllText("Logs/ragdoll_preview.log", log.ToString());
        SessionState.SetBool(Flag, false);
        EditorApplication.isPlaying = false;
    }
}
