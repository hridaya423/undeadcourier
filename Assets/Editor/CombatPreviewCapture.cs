using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CombatPreviewCapture
{
    const string Flag = "CombatPreviewCapture.active";
    const string ScenePath = "Assets/Scenes/SampleScene.unity";
    const int CaptureShots = 9;

    static bool didSetup, didAim, didShot3, didAfter, didFinish;
    static bool didDecalAim, didDecalShot;
    static float decalAimTime = -1f;
    static Vector3 decalGroundPoint;
    static bool haveDecalGroundPoint;
    static Vector3 killedEnemyPosition;
    static bool haveKilledEnemy;
    static float pitchBeforeFirst = float.NaN;
    static float pitchSettleCheckTime = -1f;
    static int shotsFired;
    static float nextShotTime;
    static bool firingStarted;
    static float lastShotFiredTime = -1f;

    static Weapon activeWeapon;
    static Transform playerTransform;

    static readonly System.Text.StringBuilder logBuf = new System.Text.StringBuilder();

    static readonly FieldInfo EnemyIsDeadField = typeof(Enemy).GetField("isDead", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
    static readonly FieldInfo EnemyHpField = typeof(Enemy).GetField("HP", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);

    static bool IsEnemyDead(Enemy e)
    {
        if (e == null) return true;
        if (EnemyIsDeadField == null) return false;
        return (bool)EnemyIsDeadField.GetValue(e);
    }

    static void RaiseEnemyHp(Enemy e, int minimumHp)
    {
        if (e == null || EnemyHpField == null) return;
        int hp = (int)EnemyHpField.GetValue(e);
        if (hp < minimumHp) EnemyHpField.SetValue(e, minimumHp);
    }

    [MenuItem("Tools/Undead Courier/Capture Combat Preview")]
    internal static void Run()
    {
        Scene scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        SessionState.SetBool(Flag, true);
        ResetState();
        Application.logMessageReceived -= OnLog;
        Application.logMessageReceived += OnLog;

        if (!EditorApplication.isPlaying)
            EditorApplication.isPlaying = true;
        else
            Arm();
    }

    static void ResetState()
    {
        didSetup = didAim = didShot3 = didAfter = didFinish = false;
        didDecalAim = didDecalShot = false;
        decalAimTime = -1f;
        haveDecalGroundPoint = false;
        haveKilledEnemy = false;
        pitchBeforeFirst = float.NaN;
        pitchSettleCheckTime = -1f;
        shotsFired = 0;
        nextShotTime = 0f;
        firingStarted = false;
        lastShotFiredTime = -1f;
        activeWeapon = null;
        playerTransform = null;
        trackedEnemy = null;
        logBuf.Clear();
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

    static void Arm()
    {
        Application.runInBackground = true;
        Application.logMessageReceived -= OnLog;
        Application.logMessageReceived += OnLog;
        GameEvents.EnemyDied -= OnEnemyDied;
        GameEvents.EnemyDied += OnEnemyDied;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
    }

    static void OnEnemyDied(Enemy e)
    {
        if (haveKilledEnemy || e == null) return;
        killedEnemyPosition = e.transform.position;
        haveKilledEnemy = true;
        logBuf.AppendLine($"[Diag] killed enemy at {killedEnemyPosition}");
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

        float t = Time.time;

        try
        {
            if (!didSetup && t > 4f)
            {
                didSetup = true;
                SetupWeapon();
            }

            if (didSetup && !didAim && t > 4.1f)
            {
                didAim = true;
                AimAtNearestEnemy();
            }

            if (didAim && !didFinish)
            {
                DriveFiring(t);
            }

            if (firingStarted && !didShot3 && shotsFired >= 3)
            {
                didShot3 = true;
                ScreenCapture.CaptureScreenshot("Logs/combat_midburst.png");
            }

            if (lastShotFiredTime > 0f && !didDecalAim && t > lastShotFiredTime + 1.7f)
            {
                didDecalAim = true;
                AimDownAtDecals();
                decalAimTime = t;
            }

            if (didDecalAim && !didDecalShot && decalAimTime > 0f)
            {
                if (trackedEnemy != null)
                {
                    HoldDownAim();
                }
                HideBloodyScreen(null);
                if (t > decalAimTime + 1.2f)
                {
                    didDecalShot = true;
                    LogDecalInventory();
                    if (!didAfter)
                    {
                        didAfter = true;
                        ScreenCapture.CaptureScreenshot("Logs/combat_after.png");
                        LogPitch("1s_after_last_shot_settle", GetCurrentPitch());
                    }
                    ScreenCapture.CaptureScreenshot("Logs/combat_decals.png");
                }
            }

            if (didDecalShot && !didFinish && t > decalAimTime + 2.0f)
            {
                didFinish = true;
                Finish();
            }

            if (t > 24f && !didFinish)
            {
                logBuf.AppendLine("[Warning] Timed out before completing capture sequence.");
                didFinish = true;
                Finish();
            }
        }
        catch (Exception e)
        {
            logBuf.AppendLine("[Exception] CombatPreviewCapture: " + e);
            if (!didFinish)
            {
                didFinish = true;
                Finish();
            }
        }
    }

    static void SetupWeapon()
    {
        GameObject slot = WeaponManager.Instance != null ? WeaponManager.Instance.activeWeaponSlot : null;
        Weapon weapon = null;

        if (slot != null && slot.transform.childCount > 0)
        {
            weapon = slot.transform.GetChild(0).GetComponent<Weapon>();
        }

        if (weapon == null)
        {
            Weapon[] sceneWeapons = UnityEngine.Object.FindObjectsByType<Weapon>(FindObjectsSortMode.None);
            foreach (Weapon candidate in sceneWeapons)
            {
                if (!candidate.isActiveWeapon)
                {
                    weapon = candidate;
                    break;
                }
            }

            if (weapon != null && WeaponManager.Instance != null)
            {
                MethodInfo pickup = typeof(WeaponManager).GetMethod("PickupWeapon", BindingFlags.Public | BindingFlags.Instance);
                pickup.Invoke(WeaponManager.Instance, new object[] { weapon.gameObject });

                if (slot != null && slot.transform.childCount > 0)
                {
                    weapon = slot.transform.GetChild(0).GetComponent<Weapon>();
                }
            }
        }

        if (weapon == null)
        {
            logBuf.AppendLine("[Error] CombatPreviewCapture: no Weapon found in scene.");
            return;
        }

        weapon.enabled = true;
        weapon.isActiveWeapon = true;
        weapon.thisWeaponModel = Weapon.WeaponModel.AK74;

        WeaponData ak = WeaponConfig.Get(Weapon.WeaponModel.AK74);
        weapon.weaponDamage = ak.damage;
        weapon.shootingDelay = ak.shotDelay;
        weapon.hipSpreadIntensity = ak.hipSpread;
        weapon.ADSSpreadIntensity = ak.adsSpread;
        weapon.spreadIntensity = ak.adsSpread;
        weapon.currentshootingMode = Weapon.ShootingMode.Auto;

        FieldInfo bulletsLeftField = typeof(Weapon).GetField("bulletsLeft", BindingFlags.Public | BindingFlags.Instance);
        FieldInfo magazineSizeField = typeof(Weapon).GetField("magazineSize", BindingFlags.Public | BindingFlags.Instance);
        FieldInfo isReloadingField = typeof(Weapon).GetField("isReloading", BindingFlags.Public | BindingFlags.Instance);
        FieldInfo readyToShootField = typeof(Weapon).GetField("readyToShoot", BindingFlags.Public | BindingFlags.Instance);

        int magazineSize = Mathf.Max((int)magazineSizeField.GetValue(weapon), 30);
        if (magazineSize <= 0) magazineSize = 30;
        magazineSizeField.SetValue(weapon, magazineSize);
        bulletsLeftField.SetValue(weapon, magazineSize);
        isReloadingField.SetValue(weapon, false);
        readyToShootField.SetValue(weapon, true);

        activeWeapon = weapon;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null && GlobalReferences.Instance != null)
        {
            player = GlobalReferences.Instance.player;
        }
        if (player == null)
        {
            MouseMovement mm = UnityEngine.Object.FindFirstObjectByType<MouseMovement>();
            if (mm != null) player = mm.gameObject;
        }

        if (player != null)
        {
            playerTransform = player.transform;
            MakePlayerInvulnerable(player);
        }
        else
        {
            logBuf.AppendLine("[Error] CombatPreviewCapture: player not found.");
        }
    }

    static void MakePlayerInvulnerable(GameObject player)
    {
        Player p = player.GetComponent<Player>();
        if (p == null) p = UnityEngine.Object.FindFirstObjectByType<Player>();
        if (p == null) return;
        FieldInfo invField = typeof(Player).GetField("isInvulnerable", BindingFlags.NonPublic | BindingFlags.Instance);
        if (invField != null) invField.SetValue(p, true);
        HideBloodyScreen(p);
    }

    static void HideBloodyScreen(Player p)
    {
        if (p == null) p = UnityEngine.Object.FindFirstObjectByType<Player>();
        if (p == null) return;
        FieldInfo bsField = typeof(Player).GetField("bloodyScreen", BindingFlags.Public | BindingFlags.Instance);
        if (bsField == null) return;
        GameObject bs = bsField.GetValue(p) as GameObject;
        if (bs != null) bs.SetActive(false);
    }

    static void MoveObjectNear(Transform toMove, Vector3 targetPosition, float distance)
    {
        Vector3 flatOffset = toMove.position - targetPosition;
        flatOffset.y = 0f;
        if (flatOffset.sqrMagnitude < 0.0001f) flatOffset = Vector3.back;
        Vector3 desiredPosition = targetPosition + flatOffset.normalized * distance;
        desiredPosition.y = targetPosition.y;

        CharacterController controller = toMove.GetComponent<CharacterController>();
        if (controller != null)
        {
            controller.enabled = false;
            toMove.position = desiredPosition;
            controller.enabled = true;
        }
        else
        {
            toMove.position = desiredPosition;
        }
    }

    static void AimAtNearestEnemy()
    {
        if (playerTransform == null || activeWeapon == null) return;

        Enemy[] enemies = UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        Enemy nearest = null;
        float bestDist = float.MaxValue;

        foreach (Enemy e in enemies)
        {
            if (IsEnemyDead(e)) continue;
            float d = Vector3.Distance(e.transform.position, playerTransform.position);
            if (d < bestDist)
            {
                bestDist = d;
                nearest = e;
            }
        }

        if (nearest == null)
        {
            logBuf.AppendLine("[Error] CombatPreviewCapture: no living Enemy found in scene.");
            return;
        }

        trackedEnemy = nearest;
        RaiseEnemyHp(nearest, 300);

        logBuf.AppendLine($"[Diag] nearestEnemy={nearest.name} pos={nearest.transform.position} playerPosBefore={playerTransform.position}");

        MoveObjectNear(playerTransform, nearest.transform.position, 9f);

        logBuf.AppendLine($"[Diag] playerPosAfterMove={playerTransform.position}");

        Vector3 chestPoint = nearest.transform.position + Vector3.up * 1.3f;
        Animator enemyAnimator = nearest.GetComponent<Animator>();
        if (enemyAnimator != null && enemyAnimator.avatar != null && enemyAnimator.avatar.isHuman)
        {
            Transform chestBone = enemyAnimator.GetBoneTransform(HumanBodyBones.Chest);
            if (chestBone != null) chestPoint = chestBone.position;
        }

        Camera cam = Camera.main;
        Vector3 camPos = cam != null ? cam.transform.position : playerTransform.position;

        Vector3 toTarget = chestPoint - camPos;
        float yaw = Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg;
        float horizontalDist = new Vector2(toTarget.x, toTarget.z).magnitude;
        float pitch = -Mathf.Atan2(toTarget.y, horizontalDist) * Mathf.Rad2Deg;

        logBuf.AppendLine($"[Diag] camPos={camPos} chestPoint={chestPoint} toTarget={toTarget} yaw={yaw:F2} pitch={pitch:F2}");

        MouseMovement mouseMovement = playerTransform.GetComponent<MouseMovement>();
        if (mouseMovement != null)
        {
            FieldInfo xRotField = typeof(MouseMovement).GetField("xRotation", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo yRotField = typeof(MouseMovement).GetField("yRotation", BindingFlags.NonPublic | BindingFlags.Instance);
            if (xRotField != null) xRotField.SetValue(mouseMovement, pitch);
            if (yRotField != null) yRotField.SetValue(mouseMovement, yaw);
        }

        playerTransform.localRotation = Quaternion.Euler(pitch, yaw, 0f);

        pitchBeforeFirst = GetCurrentPitch();
        LogPitch("before_first_shot", pitchBeforeFirst);
    }

    static void DriveFiring(float t)
    {
        if (activeWeapon == null) return;
        if (shotsFired >= CaptureShots)
        {
            if (!firingStarted)
            {
                firingStarted = true;
            }
            return;
        }

        if (!firingStarted)
        {
            firingStarted = true;
            nextShotTime = t;
        }

        Enemy target = FindNearestLivingEnemy();
        if (target != null)
        {
            trackedEnemy = target;
        }
        if (trackedEnemy != null)
        {
            KeepAimingAt(trackedEnemy);
        }

        if (t >= nextShotTime)
        {
            FireOnce();
            shotsFired++;
            nextShotTime = t + 0.35f;
            lastShotFiredTime = t;

            if (shotsFired == CaptureShots)
            {
                LogPitch("after_last_shot", GetCurrentPitch());
                ForceKillTrackedEnemy();
            }
        }
    }

    static void ForceKillTrackedEnemy()
    {
        Enemy target = trackedEnemy;
        if (target == null || IsEnemyDead(target)) target = FindNearestLivingEnemy();
        if (target == null) return;
        target.TakeDamage(100000);
    }

    static Enemy trackedEnemy;

    static Enemy FindNearestLivingEnemy()
    {
        if (playerTransform == null) return null;

        Enemy[] enemies = UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        Enemy nearest = null;
        float bestDist = float.MaxValue;

        foreach (Enemy e in enemies)
        {
            if (IsEnemyDead(e)) continue;
            float d = Vector3.Distance(e.transform.position, playerTransform.position);
            if (d < bestDist)
            {
                bestDist = d;
                nearest = e;
            }
        }

        return nearest;
    }

    static void KeepAimingAt(Enemy target)
    {
        if (target == null || playerTransform == null) return;
        if (IsEnemyDead(target)) return;

        Vector3 chestPoint = target.transform.position + Vector3.up * 1.3f;
        Animator enemyAnimator = target.GetComponent<Animator>();
        if (enemyAnimator != null && enemyAnimator.avatar != null && enemyAnimator.avatar.isHuman)
        {
            Transform chestBone = enemyAnimator.GetBoneTransform(HumanBodyBones.Chest);
            if (chestBone != null) chestPoint = chestBone.position;
        }

        Camera cam = Camera.main;
        Vector3 camPos = cam != null ? cam.transform.position : playerTransform.position;

        Vector3 toTarget = chestPoint - camPos;
        if (toTarget.sqrMagnitude < 0.0001f) return;

        float yaw = Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg;
        float horizontalDist = Mathf.Max(new Vector2(toTarget.x, toTarget.z).magnitude, 3f);
        float basePitch = -Mathf.Atan2(toTarget.y, horizontalDist) * Mathf.Rad2Deg;
        basePitch = Mathf.Clamp(basePitch, -30f, 30f);

        MouseMovement mouseMovement = playerTransform.GetComponent<MouseMovement>();
        if (mouseMovement != null)
        {
            FieldInfo xRotField = typeof(MouseMovement).GetField("xRotation", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo yRotField = typeof(MouseMovement).GetField("yRotation", BindingFlags.NonPublic | BindingFlags.Instance);
            if (xRotField != null) xRotField.SetValue(mouseMovement, basePitch);
            if (yRotField != null) yRotField.SetValue(mouseMovement, yaw);
        }

        playerTransform.localRotation = Quaternion.Euler(basePitch, yaw, 0f);
    }

    static void AimDownAtDecals()
    {
        if (playerTransform == null) return;

        Vector3 groundPoint;
        bool decalSourced = true;
        string src;
        if (BloodDecalPool.HasLastPool)
        {
            groundPoint = BloodDecalPool.LastPoolPosition;
            src = "lastPool";
        }
        else if (BloodDecalPool.HasLastDecal)
        {
            groundPoint = BloodDecalPool.LastDecalPosition;
            src = "lastDecal";
        }
        else if (haveKilledEnemy)
        {
            groundPoint = killedEnemyPosition;
            decalSourced = false;
            src = "killedEnemy";
        }
        else if (trackedEnemy != null)
        {
            groundPoint = trackedEnemy.transform.position;
            decalSourced = false;
            src = "trackedEnemy";
        }
        else
        {
            groundPoint = playerTransform.position + playerTransform.forward * 3f;
            decalSourced = false;
            src = "fallback";
        }

        if (!decalSourced)
        {
            Vector3 origin = groundPoint + Vector3.up * 1.5f;
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit gh, 5f, ~0, QueryTriggerInteraction.Ignore))
            {
                groundPoint = gh.point;
            }
        }
        decalGroundPoint = groundPoint;
        haveDecalGroundPoint = true;
        logBuf.AppendLine($"[Diag] decal source={src}");

        Vector3 viewDir = decalGroundPoint - playerTransform.position;
        viewDir.y = 0f;
        if (viewDir.sqrMagnitude < 0.0001f) viewDir = playerTransform.forward;
        viewDir.y = 0f;
        if (viewDir.sqrMagnitude < 0.0001f) viewDir = Vector3.forward;
        viewDir.Normalize();

        Vector3 standPos = decalGroundPoint - viewDir * 1.1f + Vector3.up * 2.1f;

        CharacterController controller = playerTransform.GetComponent<CharacterController>();
        if (controller != null)
        {
            controller.enabled = false;
            playerTransform.position = standPos;
            controller.enabled = true;
        }
        else
        {
            playerTransform.position = standPos;
        }

        Vector3 poolUp = Vector3.up;
        RaycastHit pn;
        if (Physics.Raycast(decalGroundPoint + Vector3.up * 0.5f, Vector3.down, out pn, 2f, ~0, QueryTriggerInteraction.Ignore))
        {
            poolUp = pn.normal;
        }
        float[] hx = { 0.55f, -0.6f, 0.2f, -0.3f };
        float[] hz = { 0.5f, 0.35f, -0.6f, 0.62f };
        for (int i = 0; i < hx.Length; i++)
        {
            Vector3 hp = decalGroundPoint + new Vector3(hx[i], 0f, hz[i]);
            if (Physics.Raycast(hp + Vector3.up * 0.5f, Vector3.down, out RaycastHit hh, 2f, ~0, QueryTriggerInteraction.Ignore)
                && hh.normal.y >= 0.72f)
            {
                BloodDecalPool.SpawnHitNoThrottle(hh.point, hh.normal, viewDir);
            }
        }

        HideBloodyScreen(null);
        HoldDownAim();
        logBuf.AppendLine($"[Diag] decal aim groundPoint={decalGroundPoint} standPos={standPos}");
    }

    static void LogDecalInventory()
    {
        var projectors = UnityEngine.Object.FindObjectsByType<UnityEngine.Rendering.Universal.DecalProjector>(FindObjectsSortMode.None);
        int active = 0;
        float nearest = float.MaxValue;
        Vector3 nearestPos = Vector3.zero;
        string nearestMat = "";
        foreach (var p in projectors)
        {
            if (p == null || !p.gameObject.activeInHierarchy || !p.enabled) continue;
            active++;
            float d = Vector3.Distance(p.transform.position, decalGroundPoint);
            if (d < nearest) { nearest = d; nearestPos = p.transform.position; nearestMat = p.material != null ? p.material.name : "null"; }
        }
        Camera cam = Camera.main;
        Vector3 cp = cam != null ? cam.transform.position : Vector3.zero;
        Vector3 cf = cam != null ? cam.transform.forward : Vector3.zero;
        logBuf.AppendLine($"[Diag] activeDecalProjectors={active} totalFound={projectors.Length} nearestToAim={nearest:F2}m nearestPos={nearestPos} nearestMat={nearestMat}");
        logBuf.AppendLine($"[Diag] camPos={cp} camFwd={cf} aimGround={decalGroundPoint}");

        UnityEngine.Rendering.Universal.DecalProjector np = null;
        float nd = float.MaxValue;
        foreach (var p in projectors)
        {
            if (p == null || !p.gameObject.activeInHierarchy || !p.enabled) continue;
            float d = Vector3.Distance(p.transform.position, decalGroundPoint);
            if (d < nd) { nd = d; np = p; }
        }
        if (np != null)
        {
            Material m = np.material;
            string sh = m != null && m.shader != null ? m.shader.name : "NULL";
            bool hasTex = m != null && m.HasProperty("Base_Map") && m.GetTexture("Base_Map") != null;
            logBuf.AppendLine($"[Diag] nearest size={np.size} pivot={np.pivot} fade={np.fadeFactor} fwd={np.transform.forward} shader={sh} hasBaseMap={hasTex} scaleMode={np.scaleMode}");
        }
        var feats = new System.Text.StringBuilder();
        var urp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
        logBuf.AppendLine($"[Diag] activeRP={(urp != null ? urp.name : "null")}");
    }

    static void HoldDownAim()
    {
        if (playerTransform == null || !haveDecalGroundPoint) return;

        Camera cam = Camera.main;
        Vector3 camPos = cam != null ? cam.transform.position : playerTransform.position;

        Vector3 toTarget = decalGroundPoint - camPos;
        float yaw = Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg;
        float horizontalDist = new Vector2(toTarget.x, toTarget.z).magnitude;
        float pitch = -Mathf.Atan2(toTarget.y, horizontalDist) * Mathf.Rad2Deg;
        pitch = Mathf.Clamp(pitch, 20f, 80f);

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

    static float GetCurrentPitch()
    {
        float basePitch = 0f;
        if (playerTransform != null)
        {
            float px = playerTransform.localEulerAngles.x;
            if (px > 180f) px -= 360f;
            basePitch = px;
        }

        Camera cam = Camera.main;
        CameraRecoil recoil = cam != null ? cam.GetComponent<CameraRecoil>() : null;
        if (recoil == null) return basePitch;

        FieldInfo currentKickField = typeof(CameraRecoil).GetField("currentKick", BindingFlags.NonPublic | BindingFlags.Instance);
        if (currentKickField == null) return basePitch;

        Vector2 currentKick = (Vector2)currentKickField.GetValue(recoil);
        return basePitch + currentKick.x;
    }

    static float GetRecoilKickPitch()
    {
        Camera cam = Camera.main;
        CameraRecoil recoil = cam != null ? cam.GetComponent<CameraRecoil>() : null;
        if (recoil == null) return 0f;

        FieldInfo currentKickField = typeof(CameraRecoil).GetField("currentKick", BindingFlags.NonPublic | BindingFlags.Instance);
        if (currentKickField == null) return 0f;

        Vector2 currentKick = (Vector2)currentKickField.GetValue(recoil);
        return currentKick.x;
    }

    static void FireOnce()
    {
        FieldInfo readyToShootField = typeof(Weapon).GetField("readyToShoot", BindingFlags.Public | BindingFlags.Instance);
        FieldInfo bulletsLeftField = typeof(Weapon).GetField("bulletsLeft", BindingFlags.Public | BindingFlags.Instance);

        int bulletsLeft = (int)bulletsLeftField.GetValue(activeWeapon);
        if (bulletsLeft <= 0)
        {
            int magazineSize = (int)typeof(Weapon).GetField("magazineSize", BindingFlags.Public | BindingFlags.Instance).GetValue(activeWeapon);
            bulletsLeftField.SetValue(activeWeapon, magazineSize > 0 ? magazineSize : 30);
        }

        readyToShootField.SetValue(activeWeapon, true);

        MethodInfo fireMethod = typeof(Weapon).GetMethod("FireWeapon", BindingFlags.NonPublic | BindingFlags.Instance);
        fireMethod.Invoke(activeWeapon, null);
    }

    static void LogPitch(string label, float pitch)
    {
        logBuf.AppendLine($"[Pitch] {label}: {pitch:F2} deg (camera pitch, recoilKick={GetRecoilKickPitch():F2} deg)");
    }

    static void Finish()
    {
        EditorApplication.update -= Tick;
        Application.logMessageReceived -= OnLog;
        GameEvents.EnemyDied -= OnEnemyDied;
        System.IO.File.WriteAllText("Logs/combat_play.log", logBuf.ToString());
        SessionState.SetBool(Flag, false);
        EditorApplication.isPlaying = false;
    }
}
