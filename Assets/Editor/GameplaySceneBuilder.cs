using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class GameplaySceneBuilder
{
    const string ScenePath = "Assets/Scenes/SampleScene.unity";
    const string GeneratedPrefix = "GAME_";

    const float MoonElevationDeg = 18f;
    const float MoonAzimuthDeg = 55f;
    static readonly Color MoonColor = ColorUtil.Hex("#8FA3C8");
    const float MoonIntensity = 1.1f;
    const float MoonShadowStrength = 0.95f;
    static readonly Color MoonFillColor = ColorUtil.Hex("#7080A0");
    const float MoonFillIntensity = 0.25f;
    const float MoonFillElevationDeg = 40f;

    static readonly Color AmbientColor = ColorUtil.Hex("#202622");
    const string SkyExrPath = "Assets/Art/NightSkyHDRI003_4K-HDR.exr";
    const string SkyMatPath = "Assets/Art/Materials/GAME_NightSky.mat";
    const float SkyExposure = 0.3f;
    static readonly Color SkyTint = new Color(0.30f, 0.35f, 0.33f, 0.5f);

    const float CameraFarClip = 120f;

    const float FlashRange = 28f;
    const float FlashInnerAngle = 22f;
    const float FlashOuterAngle = 38f;
    const float FlashIntensityInner = 30f;
    const float FlashIntensityMid = 15f;
    const float FlashIntensityOuter = 8f;
    static readonly Color FlashColor = ColorUtil.Hex("#FFE8C4");
    const float HeadlampRange = 32f;
    const float HeadlampInnerAngle = 24f;
    const float HeadlampOuterAngle = 50f;
    const float HeadlampIntensity = 110f;
    static readonly Color HeadlampColor = ColorUtil.Hex("#FFDDB0");
    const float HeadlampPitchDeg = 12f;
    const string ViewmodelLayerName = "WeaponRender";

    const float FogDensity = 0.075f;
    static readonly Color FogColor = ColorUtil.Hex("#909C90");

    const string VolumeProfilePath = "Assets/Settings/GAME_GameplayVolumeProfile.asset";
    const int PostVolumePriority = 50;
    const float PostExposure = 0.5f;
    const float PostContrast = 10f;
    const float PostSaturation = -25f;
    static readonly Color PostColorFilter = ColorUtil.Hex("#C4CEC6");
    static readonly Vector4 PostShadowsTint = new Vector4(0.96f, 1.04f, 0.98f, 0.015f);
    const float VignetteIntensity = 0.32f;
    const float VignetteSmoothness = 0.45f;
    const float BloomIntensity = 0.45f;
    const float BloomThreshold = 0.9f;

    const string PCRendererPath = "Assets/Settings/PC_Renderer.asset";
    const string MobileRendererPath = "Assets/Settings/Mobile_Renderer.asset";
    const string PCRPAssetPath = "Assets/Settings/PC_RPAsset.asset";
    const string MobileRPAssetPath = "Assets/Settings/Mobile_RPAsset.asset";
    const string DitherFeatureName = "OrderedDither";
    const float DitherRestingIntensity = 0.3f;
    const float DitherRestingSteps = 8f;
    const float DitherRestingLuma = 1f;
    const float DitherRestingPixelSize = 2f;
    const float DitherFullIntensity = 0.9f;
    const float DitherFullSteps = 5f;
    const float DitherFullLuma = 0.6f;
    const float DitherFullPixelSize = 4f;
    const float DitherOffSteps = 6f;
    const float DitherOffLuma = 1f;
    const float DitherOffPixelSize = 1f;

    const float PracticalRange = 18f;
    const float PracticalIntensity = 10f;
    static readonly Color PracticalColor = ColorUtil.Hex("#FF9A3C");
    const string PFFlamePath = "Assets/Old Torch/Prefab/PF_Flame.prefab";
    static readonly string[] LandmarkKeywords = { "cabin", "tower", "temple", "teleport" };
    const string MistMatPath = "Assets/Art/Materials/GAME_Mist.mat";
    const string GlowMatPath = "Assets/Art/Materials/GAME_Glow.mat";
    const string FlameMatPath = "Assets/Art/Materials/GAME_FlameAdditive.mat";
    const string SmokeSheetPath = "Assets/UnityTechnologies/EffectExamples/FireExplosionEffects/Textures/SmokePuffParticleSheet.png";
    const string GlowTexPath = "Assets/UnityTechnologies/EffectExamples/Misc Effects/Textures/DustMoteParticle.png";
    const string FlameTexPath = "Assets/Old Torch/Texture/Flame_frame.png";
    const int MistSystemCount = 3;
    const float MistArea = 55f;
    const float MistHeight = 1.5f;
    const float MistSizeMin = 14f;
    const float MistSizeMax = 24f;
    const float MistLifetimeMin = 14f;
    const float MistLifetimeMax = 22f;
    const float MistRate = 0.6f;
    const int MistMaxParticles = 12;
    static readonly Color MistColor = new Color(0.40f, 0.46f, 0.42f, 0.14f);
    const float NearMistArea = 18f;
    const float NearMistForwardOffset = 6f;
    const float NearMistHeight = 1.15f;
    const float NearMistSizeMin = 3.5f;
    const float NearMistSizeMax = 6.5f;
    const float NearMistRate = 0.35f;
    const int NearMistMaxParticles = 5;
    static readonly Color NearMistColor = new Color(0.68f, 0.74f, 0.68f, 0.1f);
    const float GlowSizeMin = 0.9f;
    const float GlowSizeMax = 1.3f;
    static readonly Color GlowParticleColor = new Color(1f, 1f, 1f, 0.18f);
    static readonly Color GlowHdrTint = new Color(1.3f, 0.7f, 0.3f, 1f);
    static readonly Color FlameHdrTint = new Color(2f, 1.1f, 0.5f, 1f);

    static readonly List<string> Warnings = new List<string>();

    [MenuItem("Tools/Undead Courier/Rebuild Gameplay Scene")]
    public static void Build()
    {
        const string resultPath = "Logs/gameplay_builder_result.txt";
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            System.IO.File.WriteAllText(resultPath, "FAILED " + System.DateTime.Now + "\nCannot rebuild during play mode — exit play mode first.");
            return;
        }
        try
        {
            BuildInternal();
            System.IO.File.WriteAllText(resultPath, "OK " + System.DateTime.Now + "\nWarnings:\n" + string.Join("\n", Warnings));
        }
        catch (System.Exception e)
        {
            System.IO.File.WriteAllText(resultPath, "FAILED " + System.DateTime.Now + "\n" + e);
            throw;
        }
    }

    static void BuildInternal()
    {
        Warnings.Clear();

        Scene scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        CleanupGenerated(scene);

        BuildLighting();
        BuildFog();
        EnableDepthTextureForSoftParticles();
        BuildPostProcessing();
        BuildDitherPass();
        BuildPracticals();
        BuildMist();
        DisableWorldFlashlightPickups();
        BuildMinimapFogOverride();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[GameplaySceneBuilder] Gameplay scene rebuilt." +
                   (Warnings.Count > 0 ? $"\nWarnings ({Warnings.Count}):\n- " + string.Join("\n- ", Warnings) : " No warnings."));
    }


    static void CleanupGenerated(Scene scene)
    {
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(t => t.gameObject.scene == scene && t.name.StartsWith(GeneratedPrefix))
            .OrderByDescending(t => GetDepth(t)))
        {
            if (t != null) Object.DestroyImmediate(t.gameObject);
        }

        var roots = scene.GetRootGameObjects();
        foreach (var root in roots)
        {
            if (root.name.StartsWith(GeneratedPrefix))
                Object.DestroyImmediate(root);
        }
        foreach (var root in scene.GetRootGameObjects())
        {
            var toRemove = new List<Transform>();
            CollectGenerated(root.transform, toRemove);
            foreach (var t in toRemove)
                if (t != null) Object.DestroyImmediate(t.gameObject);
        }
    }

    static int GetDepth(Transform t)
    {
        int depth = 0;
        while (t.parent != null)
        {
            depth++;
            t = t.parent;
        }
        return depth;
    }

    static void CollectGenerated(Transform parent, List<Transform> results)
    {
        foreach (Transform child in parent)
        {
            if (child.name.StartsWith(GeneratedPrefix)) results.Add(child);
            else CollectGenerated(child, results);
        }
    }


    static void EnsureDir(string dir)
    {
        if (!AssetDatabase.IsValidFolder(dir))
        {
            string parent = System.IO.Path.GetDirectoryName(dir).Replace('\\', '/');
            string leaf = System.IO.Path.GetFileName(dir);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureDir(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }

    static class ColorUtil
    {
        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }
    }

    static void BuildLighting()
    {
        foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (light.type != LightType.Directional) continue;
            if (light.gameObject.name.StartsWith(GeneratedPrefix)) continue;
            if (!light.enabled || !light.gameObject.activeInHierarchy) continue;
            Warnings.Add($"Disabled existing directional light '{light.gameObject.name}'.");
            light.gameObject.SetActive(false);
        }

        var moonGO = new GameObject(GeneratedPrefix + "Moonlight");
        var moon = moonGO.AddComponent<Light>();
        moon.type = LightType.Directional;
        moon.color = MoonColor;
        moon.intensity = MoonIntensity;
        moon.shadows = LightShadows.Soft;
        moon.shadowStrength = MoonShadowStrength;
        moonGO.transform.rotation = Quaternion.Euler(MoonElevationDeg, MoonAzimuthDeg, 0f);
        RenderSettings.sun = moon;

        var fillGO = new GameObject(GeneratedPrefix + "MoonFill");
        var fill = fillGO.AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.color = MoonFillColor;
        fill.intensity = MoonFillIntensity;
        fill.shadows = LightShadows.None;
        fillGO.transform.rotation = Quaternion.Euler(MoonFillElevationDeg, MoonAzimuthDeg + 180f, 0f);

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = AmbientColor;

        BuildSkybox();

        Camera cam = FindPlayerCamera();
        if (cam != null)
        {
            cam.farClipPlane = CameraFarClip;
            cam.allowHDR = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = FogColor;
            var addData = cam.GetUniversalAdditionalCameraData();
            addData.renderPostProcessing = true;
            ConfigureViewmodelLayer(cam.transform);
            StartWithHeldFlashlight(cam.transform);
            BuildHeadlamp(cam.transform);
        }

        TuneFlashlight();
    }

    static void StartWithHeldFlashlight(Transform fallbackParent)
    {
        var flashlight = Object.FindFirstObjectByType<Flashlight>(FindObjectsInactive.Include);
        if (flashlight == null)
        {
            Warnings.Add("Scene Flashlight not found; held flashlight skipped.");
            return;
        }

        flashlight.gameObject.SetActive(true);
        flashlight.transform.SetParent(flashlight.flashlightSpawnPoint != null ? flashlight.flashlightSpawnPoint : fallbackParent, false);
        flashlight.transform.localPosition = flashlight.spawnPosition;
        flashlight.transform.localRotation = Quaternion.Euler(flashlight.spawnRotation);
        flashlight.isHeld = true;
        if (flashlight.lightGO != null) flashlight.lightGO.SetActive(false);
        foreach (var light in flashlight.GetComponentsInChildren<Light>(true))
            light.enabled = false;

        foreach (var outline in flashlight.GetComponentsInChildren<Outline>(true))
            Object.DestroyImmediate(outline);
        foreach (var col in flashlight.GetComponentsInChildren<Collider>(true))
            Object.DestroyImmediate(col);
        foreach (var rb in flashlight.GetComponentsInChildren<Rigidbody>(true))
            Object.DestroyImmediate(rb);
        foreach (var renderer in flashlight.GetComponentsInChildren<Renderer>(true))
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
        SetLayerRecursive(flashlight.transform, LayerMask.NameToLayer(ViewmodelLayerName));
    }

    static void ConfigureViewmodelLayer(Transform cameraTransform)
    {
        int layer = LayerMask.NameToLayer(ViewmodelLayerName);
        if (layer < 0) return;
        foreach (var renderer in cameraTransform.GetComponentsInChildren<Renderer>(true))
        {
            renderer.gameObject.layer = layer;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }

    static void SetLayerRecursive(Transform root, int layer)
    {
        root.gameObject.layer = layer;
        foreach (Transform child in root)
            SetLayerRecursive(child, layer);
    }

    static void BuildHeadlamp(Transform parent)
    {
        var headlampGO = new GameObject(GeneratedPrefix + "PlayerFlashlight");
        headlampGO.transform.SetParent(parent, false);
        headlampGO.transform.localPosition = new Vector3(0f, -0.15f, 0.1f);
        headlampGO.transform.localRotation = Quaternion.Euler(HeadlampPitchDeg, 0f, 0f);
        var lamp = headlampGO.AddComponent<Light>();
        lamp.type = LightType.Spot;
        lamp.range = HeadlampRange;
        lamp.spotAngle = HeadlampOuterAngle;
        lamp.innerSpotAngle = HeadlampInnerAngle;
        lamp.intensity = HeadlampIntensity;
        lamp.color = HeadlampColor;
        lamp.shadows = LightShadows.None;
        int viewmodelLayer = LayerMask.NameToLayer(ViewmodelLayerName);
        if (viewmodelLayer >= 0) lamp.cullingMask &= ~(1 << viewmodelLayer);

        var playerFlashlight = headlampGO.AddComponent<PlayerFlashlight>();
        var so = new SerializedObject(playerFlashlight);
        so.FindProperty("flashlightLight").objectReferenceValue = lamp;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void DisableWorldFlashlightPickups()
    {
        foreach (var flashlight in Object.FindObjectsByType<Flashlight>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var root = flashlight.gameObject;
            if (flashlight.isHeld) continue;
            if (!root.activeSelf) continue;
            root.SetActive(false);
            Warnings.Add($"Disabled world flashlight pickup '{root.name}' (player now starts with the flashlight).");
        }
    }

    static void BuildSkybox()
    {
        EnsureDir("Assets/Art/Materials");

        var skyShader = Shader.Find("Skybox/Panoramic");
        Material skyMat = AssetDatabase.LoadAssetAtPath<Material>(SkyMatPath);
        if (skyMat == null)
        {
            skyMat = new Material(skyShader) { name = "GAME_NightSky" };
            AssetDatabase.CreateAsset(skyMat, SkyMatPath);
        }
        else if (skyShader != null)
        {
            skyMat.shader = skyShader;
        }

        var hdri = AssetDatabase.LoadAssetAtPath<Texture>(SkyExrPath);
        if (hdri == null) Warnings.Add("Missing sky HDRI: " + SkyExrPath);
        else if (skyMat.HasProperty("_MainTex")) skyMat.SetTexture("_MainTex", hdri);
        if (skyMat.HasProperty("_Exposure")) skyMat.SetFloat("_Exposure", SkyExposure);
        if (skyMat.HasProperty("_Tint")) skyMat.SetColor("_Tint", SkyTint);
        EditorUtility.SetDirty(skyMat);

        RenderSettings.skybox = skyMat;
    }

    static Camera FindPlayerCamera()
    {
        var cam = Camera.main;
        if (cam == null)
        {
            var playerGO = GameObject.Find("Player");
            if (playerGO != null) cam = playerGO.GetComponentInChildren<Camera>(true);
        }
        if (cam == null) Warnings.Add("Player camera not found; camera tuning skipped.");
        return cam;
    }

    static void TuneFlashlight()
    {
        var flashlight = Object.FindFirstObjectByType<Flashlight>(FindObjectsInactive.Include);
        if (flashlight == null || flashlight.lightGO == null)
        {
            Warnings.Add("Flashlight component or lightGO not found; hero-light tuning skipped.");
            return;
        }

        var lights = flashlight.lightGO.GetComponentsInChildren<Light>(true);
        if (lights.Length == 0)
        {
            Warnings.Add("Flashlight lightGO has no Light components; hero-light tuning skipped.");
            return;
        }

        foreach (var l in lights)
        {
            l.type = LightType.Spot;
            l.range = FlashRange;
            l.color = FlashColor;
            l.shadows = LightShadows.None;

            switch (l.gameObject.name)
            {
                case "LightInner":
                    l.spotAngle = FlashInnerAngle;
                    l.intensity = FlashIntensityInner;
                    l.shadows = LightShadows.Soft;
                    break;
                case "LightMid":
                    l.spotAngle = (FlashInnerAngle + FlashOuterAngle) * 0.5f;
                    l.intensity = FlashIntensityMid;
                    break;
                case "LightOuter":
                    l.spotAngle = FlashOuterAngle;
                    l.intensity = FlashIntensityOuter;
                    break;
                default:
                    l.spotAngle = FlashOuterAngle;
                    l.intensity = FlashIntensityMid;
                    break;
            }
        }
    }


    static void BuildFog()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = FogDensity;
        RenderSettings.fogColor = FogColor;
    }

    static void BuildMinimapFogOverride()
    {
        var minimapGO = GameObject.FindGameObjectWithTag("MinimapCamera");
        if (minimapGO == null)
        {
            Warnings.Add("MinimapCamera tag not found in scene; minimap fog override skipped.");
            return;
        }
        var cam = minimapGO.GetComponent<Camera>();
        if (cam == null)
        {
            Warnings.Add("GameObject tagged 'MinimapCamera' has no Camera component; minimap fog override skipped.");
            return;
        }
        if (minimapGO.GetComponent<CameraFogOverride>() == null)
            minimapGO.AddComponent<CameraFogOverride>();
    }

    static void BuildPostProcessing()
    {
        EnsureDir("Assets/Settings");

        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
        if (profile != null) AssetDatabase.DeleteAsset(VolumeProfilePath);
        profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile, VolumeProfilePath);

        var tonemapping = profile.Add<Tonemapping>(true);
        AssetDatabase.AddObjectToAsset(tonemapping, profile);
        tonemapping.mode.value = TonemappingMode.ACES;

        var colorAdjustments = profile.Add<ColorAdjustments>(true);
        AssetDatabase.AddObjectToAsset(colorAdjustments, profile);
        colorAdjustments.postExposure.value = PostExposure;
        colorAdjustments.contrast.value = PostContrast;
        colorAdjustments.saturation.value = PostSaturation;
        colorAdjustments.colorFilter.value = PostColorFilter;
        colorAdjustments.colorFilter.overrideState = true;

        var shadowsMidHi = profile.Add<ShadowsMidtonesHighlights>(true);
        AssetDatabase.AddObjectToAsset(shadowsMidHi, profile);
        shadowsMidHi.shadows.value = PostShadowsTint;

        var vignette = profile.Add<Vignette>(true);
        AssetDatabase.AddObjectToAsset(vignette, profile);
        vignette.intensity.value = VignetteIntensity;
        vignette.smoothness.value = VignetteSmoothness;

        var bloom = profile.Add<Bloom>(true);
        AssetDatabase.AddObjectToAsset(bloom, profile);
        bloom.intensity.value = BloomIntensity;
        bloom.threshold.value = BloomThreshold;

        EditorUtility.SetDirty(profile);

        foreach (var v in Object.FindObjectsByType<Volume>(FindObjectsSortMode.None))
        {
            if (!v.isGlobal) continue;
            if (v.gameObject.name.StartsWith(GeneratedPrefix)) continue;
            if (!v.enabled || !v.gameObject.activeInHierarchy) continue;
            Warnings.Add($"Disabled existing global Volume '{v.gameObject.name}'.");
            v.gameObject.SetActive(false);
        }

        var volumeGO = new GameObject(GeneratedPrefix + "PostVolume");
        var volume = volumeGO.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = PostVolumePriority;
        volume.sharedProfile = profile;
    }


    static void BuildDitherPass()
    {
        Material mat = FindDitherMaterial();
        if (mat == null)
        {
            Warnings.Add("OrderedDither material not found on PC or Mobile renderer; dither controller not created.");
            return;
        }

        var go = new GameObject(GeneratedPrefix + "DitherController");
        var controller = go.AddComponent<DitherController>();
        controller.ditherMaterial = mat;

        var resting = new DitherController.DitherState
        {
            intensity = DitherRestingIntensity,
            steps = DitherRestingSteps,
            lumaInfluence = DitherRestingLuma,
            pixelSize = DitherRestingPixelSize,
            duotone = 0f
        };
        controller.resting = resting;
        controller.full = new DitherController.DitherState
        {
            intensity = DitherFullIntensity,
            steps = DitherFullSteps,
            lumaInfluence = DitherFullLuma,
            pixelSize = DitherFullPixelSize,
            duotone = 0f
        };
        controller.gameplay = new DitherController.DitherState
        {
            intensity = 0f,
            steps = DitherOffSteps,
            lumaInfluence = DitherOffLuma,
            pixelSize = DitherOffPixelSize,
            duotone = 0f
        };
    }

    static Material FindDitherMaterial()
    {
        var mat = FindDitherMaterialOnRenderer(PCRendererPath);
        if (mat != null) return mat;
        return FindDitherMaterialOnRenderer(MobileRendererPath);
    }

    static Material FindDitherMaterialOnRenderer(string rendererPath)
    {
        var rendererData = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(rendererPath);
        if (rendererData == null)
        {
            Warnings.Add("Renderer data not found: " + rendererPath);
            return null;
        }

        var feature = rendererData.rendererFeatures.FirstOrDefault(f => f != null && f.name == DitherFeatureName) as FullScreenPassRendererFeature;
        if (feature == null)
        {
            Warnings.Add($"OrderedDither feature not found on {rendererPath}.");
            return null;
        }
        return feature.passMaterial;
    }

    static void BuildPracticals()
    {
        Scene scene = EditorSceneManager.GetActiveScene();
        var flamePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PFFlamePath);
        if (flamePrefab == null) Warnings.Add("Missing PF_Flame prefab at " + PFFlamePath + "; landmarks will use bare lights only.");

        Material flameMat = EnsureParticleMaterial(FlameMatPath, FlameTexPath, true, FlameHdrTint);
        Material glowMat = EnsureParticleMaterial(GlowMatPath, GlowTexPath, true, GlowHdrTint);

        foreach (var keyword in LandmarkKeywords)
        {
            var matches = new List<Transform>();
            foreach (var root in scene.GetRootGameObjects())
                CollectByNameContains(root.transform, keyword, matches);

            if (matches.Count == 0)
            {
                Warnings.Add($"Landmark '{keyword}' not found in scene; no practical light placed.");
                continue;
            }

            for (int i = 0; i < matches.Count; i++)
                PlacePractical(ComputeLandmarkBeaconPosition(matches[i]), keyword, i, flamePrefab, flameMat, glowMat);
        }
    }

    static void CollectByNameContains(Transform t, string keyword, List<Transform> results)
    {
        if (t.name.StartsWith(GeneratedPrefix)) return;
        if (t.name.ToLowerInvariant().Contains(keyword))
        {
            results.Add(t);
            return;
        }
        foreach (Transform child in t)
            CollectByNameContains(child, keyword, results);
    }

    static void PlacePractical(Vector3 pos, string keyword, int index, GameObject flamePrefab, Material flameMat, Material glowMat)
    {
        var lightGO = new GameObject(GeneratedPrefix + "Practical_" + keyword + "_" + index);
        lightGO.transform.position = pos;
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = PracticalColor;
        light.range = PracticalRange;
        light.intensity = PracticalIntensity;
        light.shadows = LightShadows.None;

        if (flamePrefab != null)
        {
            var flame = (GameObject)PrefabUtility.InstantiatePrefab(flamePrefab, lightGO.transform);
            flame.name = "Flame";
            flame.transform.localPosition = Vector3.zero;
            if (flameMat != null)
                foreach (var r in flame.GetComponentsInChildren<ParticleSystemRenderer>(true))
                    r.sharedMaterial = flameMat;
        }

        if (glowMat != null)
            AddGlowBillboard(lightGO.transform, glowMat);
    }

    static void AddGlowBillboard(Transform parent, Material glowMat)
    {
        var go = new GameObject("Glow");
        go.transform.SetParent(parent, false);
        var ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.loop = true;
        main.prewarm = true;
        main.startLifetime = 6f;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(GlowSizeMin, GlowSizeMax);
        main.startColor = GlowParticleColor;
        main.maxParticles = 4;

        var emission = ps.emission;
        emission.rateOverTime = 0.5f;

        var shape = ps.shape;
        shape.enabled = false;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
        col.color = g;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = glowMat;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    static void BuildMist()
    {
        Material mistMat = EnsureParticleMaterial(MistMatPath, SmokeSheetPath, false, Color.white);
        if (mistMat == null)
        {
            Warnings.Add("Mist material could not be created; ground mist skipped.");
            return;
        }

        Transform player = FindPlayerRoot();
        if (player == null) Warnings.Add("Player not found; mist anchored at origin.");
        Vector3 center = player != null ? player.position : Vector3.zero;
        Vector3 forward = player != null ? player.forward : Vector3.forward;

        for (int i = 0; i < MistSystemCount; i++)
        {
            var go = new GameObject(GeneratedPrefix + "Mist_" + i);
            if (i == 0)
            {
                go.transform.position = center + forward * NearMistForwardOffset + Vector3.up * NearMistHeight;
                if (player != null) go.transform.SetParent(player, true);
                ConfigureMist(go.AddComponent<ParticleSystem>(), mistMat, NearMistArea, NearMistSizeMin, NearMistSizeMax, NearMistRate, NearMistMaxParticles, NearMistColor);
                continue;
            }

            Vector3 offset = i == 1 ? new Vector3(38f, 0f, 22f) : new Vector3(-32f, 0f, -26f);
            go.transform.position = center + offset + Vector3.up * MistHeight;
            ConfigureMist(go.AddComponent<ParticleSystem>(), mistMat, MistArea, MistSizeMin, MistSizeMax, MistRate, MistMaxParticles, MistColor);
        }
    }

    static void ConfigureMist(ParticleSystem ps, Material mat, float area, float sizeMin, float sizeMax, float rate, int maxParticles, Color color)
    {
        var main = ps.main;
        main.loop = true;
        main.prewarm = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(MistLifetimeMin, MistLifetimeMax);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.35f);
        main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = color;
        main.maxParticles = maxParticles;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = rate;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(area, 0.4f, area);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.35f, 0.35f), new GradientAlphaKey(0.2f, 0.65f), new GradientAlphaKey(0f, 1f) });
        col.color = g;

        var sheet = ps.textureSheetAnimation;
        sheet.enabled = true;
        sheet.numTilesX = 5;
        sheet.numTilesY = 5;
        sheet.frameOverTime = new ParticleSystem.MinMaxCurve(0f, 1f);

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = mat;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.sortMode = ParticleSystemSortMode.None;
    }

    static Transform FindPlayerRoot()
    {
        var go = GameObject.Find("Player");
        if (go != null) return go.transform;
        var cam = Camera.main;
        return cam != null ? cam.transform.root : null;
    }

    static Material EnsureParticleMaterial(string path, string texPath, bool additive, Color baseColor)
    {
        EnsureDir("Assets/Art/Materials");
        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
        {
            Warnings.Add("URP Particles/Unlit shader not found; " + path + " skipped.");
            return null;
        }

        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(shader) { name = System.IO.Path.GetFileNameWithoutExtension(path) };
            AssetDatabase.CreateAsset(mat, path);
        }
        else
        {
            mat.shader = shader;
        }

        var tex = AssetDatabase.LoadAssetAtPath<Texture>(texPath);
        if (tex == null) Warnings.Add("Missing particle texture: " + texPath);
        mat.SetTexture("_BaseMap", tex);
        mat.SetColor("_BaseColor", baseColor);
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend", additive ? 2f : 0f);
        if (!additive && mat.HasProperty("_SoftParticlesEnabled"))
        {
            mat.SetFloat("_SoftParticlesEnabled", 1f);
            mat.EnableKeyword("_SOFTPARTICLES_ON");
        }
        if (!additive && mat.HasProperty("_SoftParticlesNearFadeDistance")) mat.SetFloat("_SoftParticlesNearFadeDistance", 0f);
        if (!additive && mat.HasProperty("_SoftParticlesFarFadeDistance")) mat.SetFloat("_SoftParticlesFarFadeDistance", 1.5f);
        mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", additive
            ? (float)UnityEngine.Rendering.BlendMode.One
            : (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_ZWrite", 0f);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        mat.SetShaderPassEnabled("ShadowCaster", false);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static void EnableDepthTextureForSoftParticles()
    {
        EnableDepthTexture(PCRPAssetPath);
        EnableDepthTexture(MobileRPAssetPath);
    }

    static void EnableDepthTexture(string path)
    {
        var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
        if (asset == null) return;
        var so = new SerializedObject(asset);
        var prop = so.FindProperty("m_RequireDepthTexture");
        if (prop == null) return;
        prop.boolValue = true;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);
    }

    static Vector3 ComputeLandmarkBeaconPosition(Transform landmark)
    {
        var renderers = landmark.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return landmark.position + Vector3.up * 2f;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        return bounds.center + Vector3.up * (bounds.extents.y * 0.5f + 1f);
    }
}
