using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MenuSceneBuilder
{
    const string ScenePath = "Assets/Scenes/MainMenu.unity";
    const string GeneratedPrefix = "MENU_";

    static readonly List<string> Warnings = new List<string>();

    [MenuItem("Tools/Undead Courier/Rebuild Main Menu")]
    public static void Build()
    {
        const string resultPath = "Logs/menu_builder_result.txt";
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

        Camera cam = BuildCamera();
        Transform diorama = BuildDiorama(cam, out Animator[] dioramaZombies);
        BuildLightingAndSky();
        Volume volume = BuildPostProcessing();
        Material ditherMat = BuildDitherPass();
        GameObject ditherControllerGO = BuildDitherControllerObject(ditherMat);

        UIRefs ui = BuildUI(cam);

        WireUp(cam, diorama, ditherControllerGO, ui, ditherMat, dioramaZombies);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[MenuSceneBuilder] Main menu rebuilt. Diorama, lighting, post-fx, dither pass and UI created/updated." +
                   (Warnings.Count > 0 ? $"\nWarnings ({Warnings.Count}):\n- " + string.Join("\n- ", Warnings) : " No warnings."));
    }

    struct UIRefs
    {
        public Image fadePanel;
        public LogoFlicker logo;
        public MenuButton[] buttons;
        public TMP_Text statText;
        public MenuSequencer sequencer;
    }


    static void CleanupGenerated(Scene scene)
    {
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

    static void CollectGenerated(Transform parent, List<Transform> results)
    {
        foreach (Transform child in parent)
        {
            if (child.name.StartsWith(GeneratedPrefix)) results.Add(child);
            else CollectGenerated(child, results);
        }
    }


    static T Load<T>(string path) where T : Object
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null) Warnings.Add($"Missing asset: {path}");
        return asset;
    }

    static GameObject InstantiatePrefabAt(string path, Transform parent)
    {
        var prefab = Load<GameObject>(path);
        if (prefab == null) return null;
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        return go;
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


    static readonly string[] TreePaths =
    {
        "Assets/Dry_Trees/Prefab/Dry7509.prefab",
        "Assets/Dry_Trees/Prefab/Dry4195.prefab",
        "Assets/Dry_Trees/Prefab/Dry5818.prefab",
        "Assets/Dry_Trees/Prefab/Dry6524.prefab",
        "Assets/Dry_Trees/Prefab/Dry3333.prefab",
    };

    const string ZombiePrefabPath = "Assets/Prefab/Zombie.prefab";
    const string MudMaterialPath = "Assets/Materials/mud_cracked_dry_03_4k.blend/New Material.mat";
    const string GroundMatPath = "Assets/Art/Materials/MENU_Ground.mat";
    const string RoadMatPath = "Assets/Art/Materials/MENU_Road.mat";
    const string FogTexturePath = "Assets/Art/Materials/MENU_FogSoft.png";
    const string FogMatPath = "Assets/Art/Materials/MENU_Fog.mat";
    const string MailboxMatPath = "Assets/Art/Materials/MENU_Mailbox.mat";

    const int CompositionSeed = 20260703;

    const float MoonYawDeg = 19f;
    static Vector3 RoadDir => Quaternion.Euler(0f, MoonYawDeg, 0f) * Vector3.forward;
    static Vector3 Right => Quaternion.Euler(0f, MoonYawDeg, 0f) * Vector3.right;

    static Transform BuildDiorama(Camera cam, out Animator[] dioramaZombieAnimators)
    {
        Random.InitState(CompositionSeed);
        EnsureDir("Assets/Art/Materials");
        var zombieAnimators = new List<Animator>();

        var root = new GameObject(GeneratedPrefix + "DIORAMA").transform;

        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.SetParent(root, false);
        ground.transform.localScale = new Vector3(12f, 1f, 12f);
        ground.transform.localPosition = new Vector3(0f, 0f, 30f);
        var mud = Load<Material>(MudMaterialPath);
        Material groundMat = mud;
        if (mud != null)
        {
            groundMat = new Material(mud) { name = "MENU_Ground" };
            groundMat.mainTextureScale = new Vector2(24f, 24f);
            if (groundMat.HasProperty("_BaseMap")) groundMat.SetTextureScale("_BaseMap", new Vector2(24f, 24f));
            if (groundMat.HasProperty("_BaseColor")) groundMat.SetColor("_BaseColor", new Color(0.30f, 0.29f, 0.27f));
            if (AssetDatabase.LoadAssetAtPath<Material>(GroundMatPath) != null) AssetDatabase.DeleteAsset(GroundMatPath);
            AssetDatabase.CreateAsset(groundMat, GroundMatPath);
        }
        ground.GetComponent<MeshRenderer>().sharedMaterial = groundMat;
        Object.DestroyImmediate(ground.GetComponent<Collider>());

        var road = GameObject.CreatePrimitive(PrimitiveType.Plane);
        road.name = "Road";
        road.transform.SetParent(root, false);
        road.transform.localScale = new Vector3(0.35f, 1f, 9.5f);
        road.transform.rotation = Quaternion.Euler(0f, MoonYawDeg, 0f);
        road.transform.position = RoadDir * 47f + Vector3.up * 0.02f;
        Object.DestroyImmediate(road.GetComponent<Collider>());
        if (mud != null)
        {
            var roadMat = new Material(mud) { name = "MENU_Road" };
            roadMat.mainTextureScale = new Vector2(3f, 30f);
            if (roadMat.HasProperty("_BaseMap")) roadMat.SetTextureScale("_BaseMap", new Vector2(3f, 30f));
            if (roadMat.HasProperty("_BaseColor")) roadMat.SetColor("_BaseColor", new Color(0.62f, 0.60f, 0.57f));
            if (AssetDatabase.LoadAssetAtPath<Material>(RoadMatPath) != null) AssetDatabase.DeleteAsset(RoadMatPath);
            AssetDatabase.CreateAsset(roadMat, RoadMatPath);
            road.GetComponent<MeshRenderer>().sharedMaterial = roadMat;
        }

        var treePrefabs = TreePaths.Select(p => AssetDatabase.LoadAssetAtPath<GameObject>(p)).Where(p => p != null).ToArray();
        if (treePrefabs.Length == 0) Warnings.Add("No dead tree prefabs found under Assets/Dry_Trees/Prefab.");
        else
        {
            var treesRoot = new GameObject("Trees").transform;
            treesRoot.SetParent(root, false);
            Vector3 camFlat = cam != null ? new Vector3(cam.transform.position.x, 0f, cam.transform.position.z) : Vector3.zero;
            int count = 30;
            for (int i = 0; i < count; i++)
            {
                var prefab = treePrefabs[Random.Range(0, treePrefabs.Length)];
                float side = (i % 2 == 0) ? -1f : 1f;
                if (i % 4 == 0) side = -1f;

                Vector3 pos = Vector3.zero;
                float lateral;
                for (int attempt = 0; attempt < 20; attempt++)
                {
                    float z = Random.Range(4f, 70f);
                    lateral = side * Random.Range(4.5f, 20f) * (1f + z / 70f);
                    pos = RoadDir * z + Right * lateral;

                    if (Mathf.Abs(lateral) < 4.5f) continue;

                    if (side > 0f && z > 40f)
                    {
                        Vector3 toTreeFlat = new Vector3(pos.x, 0f, pos.z) - camFlat;
                        if (Vector3.Angle(RoadDir, toTreeFlat) < 6f) continue;
                    }
                    break;
                }

                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, treesRoot);
                go.transform.localPosition = pos;
                go.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                float scale = Random.Range(0.85f, 1.6f);
                go.transform.localScale = Vector3.one * scale;
            }

            var framingPrefab = treePrefabs[0];
            Vector3[] framingPositions =
            {
                RoadDir * 45f + Right * 8f,
                RoadDir * 50f + Right * 11f,
                RoadDir * 7f + Right * 6f,
                RoadDir * 12f + Right * 8f,
                RoadDir * 18f + Right * 11f,
            };
            float[] framingScales = { 1.9f, 1.7f, 1.3f, 1.5f, 1.8f };
            float[] framingYaw = { 210f, 150f, 80f, 305f, 25f };
            for (int i = 0; i < framingPositions.Length; i++)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(framingPrefab, treesRoot);
                go.transform.localPosition = framingPositions[i];
                go.transform.localRotation = Quaternion.Euler(0f, framingYaw[i], 0f);
                go.transform.localScale = Vector3.one * framingScales[i];
            }
        }

        var zombiePrefab = Load<GameObject>(ZombiePrefabPath);
        if (zombiePrefab != null)
        {
            var zombiesRoot = new GameObject("Zombies").transform;
            zombiesRoot.SetParent(root, false);
            float[] roadDistances = { 9f, 15f, 22f, 30f, 42f };
            for (int i = 0; i < roadDistances.Length; i++)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(zombiePrefab, zombiesRoot);
                go.name = "Zombie_" + i;
                float z = roadDistances[i];
                float lateralSign = (i % 2 == 0) ? 1f : -1f;
                go.transform.localPosition = RoadDir * z + Right * (lateralSign * Random.Range(0.5f, 1.5f));
                go.transform.localRotation = Quaternion.LookRotation(-RoadDir) * Quaternion.Euler(0f, Random.Range(-15f, 15f), 0f);
                go.transform.localScale = Vector3.one * Random.Range(0.95f, 1.15f);
                StripZombieGameplayComponents(go);
                var shamble = go.AddComponent<MenuZombieShamble>();
                shamble.killDistance = 8f;
                shamble.spawnPosition = RoadDir * (52f + i * 1.5f) + Right * (-lateralSign * Random.Range(0.5f, 1.5f));
                var animator = go.GetComponentInChildren<Animator>(true);
                if (animator != null) zombieAnimators.Add(animator);
            }
        }
        else
        {
            Warnings.Add("Zombie prefab not found at " + ZombiePrefabPath);
        }

        var fogMat = BuildFogMaterial();
        BuildFogSystems(root, fogMat);

        BuildMoon(root, cam);
        BuildHorizonBand(root, cam);
        BuildMailbox(root);

        dioramaZombieAnimators = zombieAnimators.ToArray();
        return root;
    }

    static void StripZombieGameplayComponents(GameObject go)
    {
        foreach (var agent in go.GetComponentsInChildren<UnityEngine.AI.NavMeshAgent>(true))
            Object.DestroyImmediate(agent);
        foreach (var col in go.GetComponentsInChildren<Collider>(true))
            Object.DestroyImmediate(col);
        var enemy = go.GetComponentInChildren<Enemy>(true);
        if (enemy != null) Object.DestroyImmediate(enemy);
        var zombie = go.GetComponentInChildren<Zombie>(true);
        if (zombie != null) Object.DestroyImmediate(zombie);
        var hand = go.GetComponentInChildren<ZombieHand>(true);
        if (hand != null) Object.DestroyImmediate(hand);
    }

    static class ColorUtil
    {
        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }
    }


    const string ScrimTexturePath = "Assets/Art/Materials/MENU_Scrim.png";

    static Texture2D BuildScrimTexture()
    {
        const int w = 256, h = 8;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float k = x / (float)(w - 1);
                float alpha = Mathf.SmoothStep(0.85f, 0f, k);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();

        byte[] png = tex.EncodeToPNG();
        Object.DestroyImmediate(tex);
        System.IO.File.WriteAllBytes(ScrimTexturePath, png);
        AssetDatabase.ImportAsset(ScrimTexturePath, ImportAssetOptions.ForceUpdate);

        var importer = (TextureImporter)AssetImporter.GetAtPath(ScrimTexturePath);
        importer.textureType = TextureImporterType.Default;
        importer.filterMode = FilterMode.Bilinear;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Texture2D>(ScrimTexturePath);
    }

    static Texture2D BuildFogTexture()
    {
        const int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var center = new Vector2(size / 2f, size / 2f);
        float maxDist = size / 2f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center) / maxDist;
                float alpha = Mathf.SmoothStep(1f, 0f, Mathf.Clamp01(dist));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();

        byte[] png = tex.EncodeToPNG();
        Object.DestroyImmediate(tex);
        System.IO.File.WriteAllBytes(FogTexturePath, png);
        AssetDatabase.ImportAsset(FogTexturePath, ImportAssetOptions.ForceUpdate);

        var importer = (TextureImporter)AssetImporter.GetAtPath(FogTexturePath);
        importer.filterMode = FilterMode.Bilinear;
        importer.mipmapEnabled = false;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency = true;
        importer.textureType = TextureImporterType.Default;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Texture2D>(FogTexturePath);
    }

    static Material BuildFogMaterial()
    {
        var tex = BuildFogTexture();
        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) Warnings.Add("Shader 'Universal Render Pipeline/Particles/Unlit' not found.");

        if (AssetDatabase.LoadAssetAtPath<Material>(FogMatPath) != null) AssetDatabase.DeleteAsset(FogMatPath);
        var mat = new Material(shader) { name = "MENU_Fog" };
        AssetDatabase.CreateAsset(mat, FogMatPath);

        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend", 0f);
        if (mat.HasProperty("_SoftParticlesEnabled"))
        {
            mat.SetFloat("_SoftParticlesEnabled", 1f);
            mat.EnableKeyword("_SOFTPARTICLES_ON");
        }
        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
        if (mat.HasProperty("_BaseColor"))
        {
            var c = ColorUtil.Hex("#39424C");
            c.a = 1f;
            mat.SetColor("_BaseColor", c);
        }
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static void BuildFogSystems(Transform root, Material fogMat)
    {
        BuildFogLayer(root, fogMat, "GroundFogFar", new Vector3(44f, 1f, 50f), new Vector3(0f, 0.5f, 22f),
            sizeMin: 8f, sizeMax: 16f, alpha: 0.13f, maxParticles: 48, emissionRate: 2.0f);
        BuildFogLayer(root, fogMat, "GroundFogNear", new Vector3(30f, 1f, 12f), new Vector3(0f, 0.5f, 6f),
            sizeMin: 12f, sizeMax: 20f, alpha: 0.09f, maxParticles: 18, emissionRate: 1.2f);
    }

    static void BuildFogLayer(Transform root, Material fogMat, string name, Vector3 boxSize, Vector3 position,
        float sizeMin, float sizeMax, float alpha, int maxParticles, float emissionRate = 1.2f)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root, false);
        go.transform.localPosition = position;

        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 25f;
        main.prewarm = true;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.maxParticles = maxParticles;
        main.loop = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var startColor = ColorUtil.Hex("#39424C");
        startColor.a = alpha;
        main.startColor = startColor;

        var emission = ps.emission;
        emission.rateOverTime = emissionRate;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = boxSize;

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.x = new ParticleSystem.MinMaxCurve(-0.15f, 0.15f);
        vel.y = new ParticleSystem.MinMaxCurve(0f, 0f);
        vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        var rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.z = new ParticleSystem.MinMaxCurve(-2f * Mathf.Deg2Rad, 2f * Mathf.Deg2Rad);

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(0f, 1f) });
        colorOverLifetime.color = grad;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sharedMaterial = fogMat;
        renderer.sortingFudge = 10f;
    }

    static void BuildFogCards(Transform root, Material fogMat)
    {
        var cardsRoot = new GameObject("FogCards").transform;
        cardsRoot.SetParent(root, false);
        float[] depths = { 12f, 20f, 30f, 40f };
        foreach (var z in depths)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "FogCard_" + z;
            quad.transform.SetParent(cardsRoot, false);
            quad.transform.localPosition = new Vector3(0f, 3f, z);
            quad.transform.localScale = new Vector3(20f, 6f, 1f);
            Object.DestroyImmediate(quad.GetComponent<Collider>());

            var renderer = quad.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = fogMat;

            var mpb = new MaterialPropertyBlock();
            var baseColor = fogMat.HasProperty("_BaseColor") ? fogMat.GetColor("_BaseColor") : Color.white;
            baseColor.a = 0.05f;
            mpb.SetColor("_BaseColor", baseColor);
            renderer.SetPropertyBlock(mpb);
        }
    }


    const string MoonDiscTexPath = "Assets/Art/Materials/MENU_MoonDisc.png";
    const string MoonHaloTexPath = "Assets/Art/Materials/MENU_MoonHalo.png";
    const string MoonDiscMatPath = "Assets/Art/Materials/MENU_MoonDisc.mat";
    const string MoonHaloMatPath = "Assets/Art/Materials/MENU_MoonHalo.mat";

    static Texture2D BuildMoonDiscTexture()
    {
        const int size = 512;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var center = new Vector2(size / 2f, size / 2f);
        float maxDist = size / 2f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float r = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center) / maxDist;
                float alpha = Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(0.57f, 0.60f, r));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();

        byte[] png = tex.EncodeToPNG();
        Object.DestroyImmediate(tex);
        System.IO.File.WriteAllBytes(MoonDiscTexPath, png);
        AssetDatabase.ImportAsset(MoonDiscTexPath, ImportAssetOptions.ForceUpdate);

        var importer = (TextureImporter)AssetImporter.GetAtPath(MoonDiscTexPath);
        importer.textureType = TextureImporterType.Default;
        importer.filterMode = FilterMode.Bilinear;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Texture2D>(MoonDiscTexPath);
    }

    static Texture2D BuildMoonHaloTexture()
    {
        const int size = 512;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var center = new Vector2(size / 2f, size / 2f);
        float maxDist = size / 2f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float r = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center) / maxDist;
                float alpha = 0.45f * Mathf.Pow(Mathf.Clamp01(1f - r), 2f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();

        byte[] png = tex.EncodeToPNG();
        Object.DestroyImmediate(tex);
        System.IO.File.WriteAllBytes(MoonHaloTexPath, png);
        AssetDatabase.ImportAsset(MoonHaloTexPath, ImportAssetOptions.ForceUpdate);

        var importer = (TextureImporter)AssetImporter.GetAtPath(MoonHaloTexPath);
        importer.textureType = TextureImporterType.Default;
        importer.filterMode = FilterMode.Bilinear;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Texture2D>(MoonHaloTexPath);
    }

    static void BuildMoon(Transform root, Camera cam)
    {
        var discTex = BuildMoonDiscTexture();
        var haloTex = BuildMoonHaloTexture();
        var shader = Shader.Find("Sprites/Default");

        if (AssetDatabase.LoadAssetAtPath<Material>(MoonHaloMatPath) != null) AssetDatabase.DeleteAsset(MoonHaloMatPath);
        var haloMat = new Material(shader) { name = "MENU_MoonHalo" };
        haloMat.mainTexture = haloTex;
        haloMat.renderQueue = 2940;
        AssetDatabase.CreateAsset(haloMat, MoonHaloMatPath);

        if (AssetDatabase.LoadAssetAtPath<Material>(MoonDiscMatPath) != null) AssetDatabase.DeleteAsset(MoonDiscMatPath);
        var discMat = new Material(shader) { name = "MENU_MoonDisc" };
        discMat.mainTexture = discTex;
        discMat.renderQueue = 2950;
        AssetDatabase.CreateAsset(discMat, MoonDiscMatPath);

        Vector3 moonPos = RoadDir * 90f + Vector3.up * 9.5f;
        Vector3 camPos = cam != null ? cam.transform.position : Vector3.zero;

        var halo = GameObject.CreatePrimitive(PrimitiveType.Quad);
        halo.name = "MoonHalo";
        halo.transform.SetParent(root, false);
        halo.transform.position = moonPos;
        halo.transform.localScale = new Vector3(50f, 50f, 1f);
        halo.transform.rotation = Quaternion.LookRotation(moonPos - camPos);
        Object.DestroyImmediate(halo.GetComponent<Collider>());
        var haloRenderer = halo.GetComponent<MeshRenderer>();
        haloRenderer.sharedMaterial = haloMat;
        haloRenderer.shadowCastingMode = ShadowCastingMode.Off;
        haloRenderer.receiveShadows = false;

        Vector3 discPos = moonPos - RoadDir * 0.5f;
        var disc = GameObject.CreatePrimitive(PrimitiveType.Quad);
        disc.name = "MoonDisc";
        disc.transform.SetParent(root, false);
        disc.transform.position = discPos;
        disc.transform.localScale = new Vector3(16f, 16f, 1f);
        disc.transform.rotation = Quaternion.LookRotation(discPos - camPos);
        Object.DestroyImmediate(disc.GetComponent<Collider>());
        var discRenderer = disc.GetComponent<MeshRenderer>();
        discRenderer.sharedMaterial = discMat;
        discRenderer.shadowCastingMode = ShadowCastingMode.Off;
        discRenderer.receiveShadows = false;
    }


    const string HorizonBandTexPath = "Assets/Art/Materials/MENU_HorizonBand.png";
    const string HorizonBandMatPath = "Assets/Art/Materials/MENU_HorizonBand.mat";

    static Texture2D BuildHorizonBandTexture()
    {
        const int w = 8, h = 64;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++)
        {
            float alpha = 0.9f * Mathf.Sin(Mathf.PI * (y / 63f));
            for (int x = 0; x < w; x++)
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
        }
        tex.Apply();

        byte[] png = tex.EncodeToPNG();
        Object.DestroyImmediate(tex);
        System.IO.File.WriteAllBytes(HorizonBandTexPath, png);
        AssetDatabase.ImportAsset(HorizonBandTexPath, ImportAssetOptions.ForceUpdate);

        var importer = (TextureImporter)AssetImporter.GetAtPath(HorizonBandTexPath);
        importer.textureType = TextureImporterType.Default;
        importer.filterMode = FilterMode.Bilinear;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Texture2D>(HorizonBandTexPath);
    }

    static void BuildHorizonBand(Transform root, Camera cam)
    {
        var tex = BuildHorizonBandTexture();
        var shader = Shader.Find("Sprites/Default");

        if (AssetDatabase.LoadAssetAtPath<Material>(HorizonBandMatPath) != null) AssetDatabase.DeleteAsset(HorizonBandMatPath);
        var mat = new Material(shader) { name = "MENU_HorizonBand" };
        mat.mainTexture = tex;
        var c = ColorUtil.Hex("#46505C");
        c.a = 0.20f;
        mat.color = c;
        mat.renderQueue = 2960;
        AssetDatabase.CreateAsset(mat, HorizonBandMatPath);

        Vector3 pos = RoadDir * 65f + Vector3.up * 2.5f;
        Vector3 camPos = cam != null ? cam.transform.position : Vector3.zero;

        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "HorizonBand";
        quad.transform.SetParent(root, false);
        quad.transform.position = pos;
        quad.transform.localScale = new Vector3(140f, 9f, 1f);
        quad.transform.rotation = Quaternion.LookRotation(pos - camPos);
        Object.DestroyImmediate(quad.GetComponent<Collider>());
        var renderer = quad.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = mat;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }


    static void BuildMailbox(Transform root)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (AssetDatabase.LoadAssetAtPath<Material>(MailboxMatPath) != null) AssetDatabase.DeleteAsset(MailboxMatPath);
        var mat = new Material(shader) { name = "MENU_Mailbox" };
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", new Color(0.05f, 0.05f, 0.055f));
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.1f);
        AssetDatabase.CreateAsset(mat, MailboxMatPath);

        var mailboxGO = new GameObject("Mailbox");
        mailboxGO.transform.SetParent(root, false);
        mailboxGO.transform.localPosition = RoadDir * 8f + Right * 2.0f;
        mailboxGO.transform.localRotation = Quaternion.Euler(0f, -30f + MoonYawDeg, 6f);
        mailboxGO.transform.localScale = Vector3.one * 1.25f;

        var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        post.name = "Post";
        post.transform.SetParent(mailboxGO.transform, false);
        post.transform.localPosition = new Vector3(0f, 0.55f, 0f);
        post.transform.localScale = new Vector3(0.07f, 0.55f, 0.07f);
        Object.DestroyImmediate(post.GetComponent<Collider>());
        post.GetComponent<MeshRenderer>().sharedMaterial = mat;

        var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = "Box";
        box.transform.SetParent(mailboxGO.transform, false);
        box.transform.localPosition = new Vector3(0f, 1.15f, 0f);
        box.transform.localScale = new Vector3(0.30f, 0.30f, 0.45f);
        Object.DestroyImmediate(box.GetComponent<Collider>());
        box.GetComponent<MeshRenderer>().sharedMaterial = mat;

        var flap = GameObject.CreatePrimitive(PrimitiveType.Cube);
        flap.name = "DoorFlap";
        flap.transform.SetParent(mailboxGO.transform, false);
        flap.transform.localPosition = new Vector3(0f, 1.02f, 0.28f);
        flap.transform.localRotation = Quaternion.Euler(35f, 0f, 0f);
        flap.transform.localScale = new Vector3(0.28f, 0.02f, 0.24f);
        Object.DestroyImmediate(flap.GetComponent<Collider>());
        flap.GetComponent<MeshRenderer>().sharedMaterial = mat;

        var flagArm = GameObject.CreatePrimitive(PrimitiveType.Cube);
        flagArm.name = "FlagArm";
        flagArm.transform.SetParent(mailboxGO.transform, false);
        flagArm.transform.localPosition = new Vector3(0.17f, 1.32f, -0.1f);
        flagArm.transform.localRotation = Quaternion.Euler(0f, 0f, 20f);
        flagArm.transform.localScale = new Vector3(0.03f, 0.18f, 0.03f);
        Object.DestroyImmediate(flagArm.GetComponent<Collider>());
        flagArm.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }


    const string SkyMatPath = "Assets/Art/Materials/MENU_NightSky.mat";
    const string SkyExrPath = "Assets/Art/NightSkyHDRI003_4K-HDR.exr";

    static void BuildLightingAndSky()
    {
        EnsureDir("Assets/Art/Materials");

        var skyShader = Shader.Find("Skybox/Panoramic");
        Material skyMat = AssetDatabase.LoadAssetAtPath<Material>(SkyMatPath);
        if (skyMat == null)
        {
            skyMat = new Material(skyShader) { name = "MENU_NightSky" };
            AssetDatabase.CreateAsset(skyMat, SkyMatPath);
        }
        else
        {
            skyMat.shader = skyShader;
        }
        var hdri = Load<Texture>(SkyExrPath);
        if (hdri != null && skyMat.HasProperty("_Tex")) skyMat.SetTexture("_Tex", hdri);
        if (skyMat.HasProperty("_Exposure")) skyMat.SetFloat("_Exposure", 0.75f);
        EditorUtility.SetDirty(skyMat);

        RenderSettings.skybox = skyMat;
        RenderSettings.fog = true;
        RenderSettings.fogColor = ColorUtil.Hex("#17202C");
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = 0.013f;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.030f, 0.040f, 0.058f);

        var moonGO = new GameObject(GeneratedPrefix + "Moonlight");
        var moon = moonGO.AddComponent<Light>();
        moon.type = LightType.Directional;
        moon.color = ColorUtil.Hex("#8FA3C8");
        moon.intensity = 2.6f;
        moon.shadows = LightShadows.Soft;
        moon.shadowStrength = 0.85f;
        moonGO.transform.rotation = Quaternion.Euler(10f, 199f, 0f);

        var fillGO = new GameObject(GeneratedPrefix + "FrontFill");
        var fill = fillGO.AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.color = ColorUtil.Hex("#7080A0");
        fill.intensity = 0.15f;
        fill.shadows = LightShadows.None;
        fillGO.transform.rotation = Quaternion.Euler(12f, 8f, 0f);
    }


    static Camera BuildCamera()
    {
        var cam = Camera.main;
        if (cam == null)
        {
            var camGO = GameObject.Find("Main Camera");
            if (camGO != null) cam = camGO.GetComponent<Camera>();
        }
        if (cam == null)
        {
            Warnings.Add("No Main Camera found in scene; created a new one.");
            var camGO = new GameObject("Main Camera") { tag = "MainCamera" };
            cam = camGO.AddComponent<Camera>();
        }

        cam.transform.position = new Vector3(0f, 1.15f, 0f);
        cam.transform.rotation = Quaternion.Euler(-1.5f, 12f, 0f);
        cam.fieldOfView = 50f;
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.farClipPlane = Mathf.Max(cam.farClipPlane, 150f);

        var rig = cam.GetComponent<MenuCameraRig>();
        if (rig == null) rig = cam.gameObject.AddComponent<MenuCameraRig>();
        rig.driftAmplitude = 1.1f;
        rig.parallaxAmplitude = 0.8f;

        var addData = cam.GetUniversalAdditionalCameraData();
        addData.renderPostProcessing = true;

        return cam;
    }


    const string VolumeProfilePath = "Assets/Settings/MenuVolumeProfile.asset";

    static Volume BuildPostProcessing()
    {
        EnsureDir("Assets/Settings");

        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
        if (profile != null)
        {
            AssetDatabase.DeleteAsset(VolumeProfilePath);
        }
        profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile, VolumeProfilePath);

        var bloom = profile.Add<Bloom>(true);
        AssetDatabase.AddObjectToAsset(bloom, profile);
        bloom.intensity.value = 1.2f;
        bloom.threshold.value = 0.85f;

        var vignette = profile.Add<Vignette>(true);
        AssetDatabase.AddObjectToAsset(vignette, profile);
        vignette.intensity.value = 0.38f;
        vignette.smoothness.value = 0.5f;

        var colorAdjustments = profile.Add<ColorAdjustments>(true);
        AssetDatabase.AddObjectToAsset(colorAdjustments, profile);
        colorAdjustments.postExposure.value = -0.3f;
        colorAdjustments.saturation.value = -12f;
        colorAdjustments.contrast.value = 8f;

        var tonemapping = profile.Add<Tonemapping>(true);
        AssetDatabase.AddObjectToAsset(tonemapping, profile);
        tonemapping.mode.value = TonemappingMode.ACES;


        var liftGammaGain = profile.Add<LiftGammaGain>(true);
        AssetDatabase.AddObjectToAsset(liftGammaGain, profile);
        liftGammaGain.lift.value = new Vector4(0.95f, 0.97f, 1.05f, 0f);

        EditorUtility.SetDirty(profile);

        var volumeGO = new GameObject(GeneratedPrefix + "Volume");
        var volume = volumeGO.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.sharedProfile = profile;
        return volume;
    }


    const string DitherShaderName = "UndeadCourier/OrderedDither";
    const string DitherMatPath = "Assets/Art/Materials/MENU_Dither.mat";
    const string PCRendererPath = "Assets/Settings/PC_Renderer.asset";
    const string MobileRendererPath = "Assets/Settings/Mobile_Renderer.asset";
    const string FeatureName = "OrderedDither";
    const float RestingPixelSize = 1f;

    static Material BuildDitherPass()
    {
        var shader = Shader.Find(DitherShaderName);
        if (shader == null)
        {
            Warnings.Add($"Shader '{DitherShaderName}' not found; dither material may render pink.");
        }

        EnsureDir("Assets/Art/Materials");
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(DitherMatPath);
        if (mat == null)
        {
            mat = new Material(shader) { name = "MENU_Dither" };
            AssetDatabase.CreateAsset(mat, DitherMatPath);
        }
        else if (shader != null)
        {
            mat.shader = shader;
        }

        if (mat.HasProperty("_Intensity")) mat.SetFloat("_Intensity", 0f);
        if (mat.HasProperty("_Steps")) mat.SetFloat("_Steps", 6f);
        if (mat.HasProperty("_LumaInfluence")) mat.SetFloat("_LumaInfluence", 0f);
        if (mat.HasProperty("_PixelSize")) mat.SetFloat("_PixelSize", 1f);
        if (mat.HasProperty("_DitherSpread")) mat.SetFloat("_DitherSpread", 1f);
        if (mat.HasProperty("_Duotone")) mat.SetFloat("_Duotone", 0f);
        if (mat.HasProperty("_ShadowColor")) mat.SetColor("_ShadowColor", new Color(0.02f, 0.024f, 0.038f));
        if (mat.HasProperty("_LightColor")) mat.SetColor("_LightColor", new Color(0.63f, 0.63f, 0.59f));
        if (mat.HasProperty("_DuoBlack")) mat.SetFloat("_DuoBlack", 0.02f);
        if (mat.HasProperty("_DuoWhite")) mat.SetFloat("_DuoWhite", 0.20f);
        if (mat.HasProperty("_BloodEdge")) mat.SetFloat("_BloodEdge", 0.075f);
        if (mat.HasProperty("_BloodRadius")) mat.SetFloat("_BloodRadius", 0f);
        if (mat.HasProperty("_BloodAmount")) mat.SetFloat("_BloodAmount", 0f);
        if (mat.HasProperty("_BloodSplatSeed")) mat.SetFloat("_BloodSplatSeed", 0f);
        if (mat.HasProperty("_BloodDrainRadius")) mat.SetFloat("_BloodDrainRadius", 0f);
        if (mat.HasProperty("_BloodColorDark")) mat.SetColor("_BloodColorDark", new Color(0.16f, 0.006f, 0.012f));
        if (mat.HasProperty("_BloodColorMid")) mat.SetColor("_BloodColorMid", new Color(0.46f, 0.020f, 0.026f));
        if (mat.HasProperty("_BloodColorLight")) mat.SetColor("_BloodColorLight", new Color(0.86f, 0.070f, 0.045f));
        EditorUtility.SetDirty(mat);

        AddDitherFeatureToRenderer(PCRendererPath, mat);
        AddDitherFeatureToRenderer(MobileRendererPath, mat);

        AssetDatabase.SaveAssets();
        return mat;
    }

    static void AddDitherFeatureToRenderer(string rendererPath, Material mat)
    {
        var rendererData = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(rendererPath);
        if (rendererData == null)
        {
            Warnings.Add("Renderer data not found: " + rendererPath);
            return;
        }

        if (rendererData.rendererFeatures.Any(f => f != null && f.name == FeatureName))
            return;

        var feature = ScriptableObject.CreateInstance<FullScreenPassRendererFeature>();
        feature.name = FeatureName;
        feature.passMaterial = mat;
        feature.injectionPoint = FullScreenPassRendererFeature.InjectionPoint.AfterRenderingPostProcessing;
        feature.requirements = ScriptableRenderPassInput.Color;

        AssetDatabase.AddObjectToAsset(feature, rendererData);

        var so = new SerializedObject(rendererData);
        var featuresProp = so.FindProperty("m_RendererFeatures");
        int index = featuresProp.arraySize;
        featuresProp.arraySize++;
        featuresProp.GetArrayElementAtIndex(index).objectReferenceValue = feature;

        var mapProp = so.FindProperty("m_RendererFeatureMap");
        mapProp.arraySize = featuresProp.arraySize;
        for (int i = 0; i < featuresProp.arraySize; i++)
        {
            var obj = featuresProp.GetArrayElementAtIndex(i).objectReferenceValue;
            long localId = 0;
            if (obj != null && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out _, out long id))
                localId = id;
            mapProp.GetArrayElementAtIndex(i).longValue = localId;
        }

        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(rendererData);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(rendererPath, ImportAssetOptions.ForceUpdate);
    }

    static GameObject BuildDitherControllerObject(Material ditherMat)
    {
        var go = new GameObject(GeneratedPrefix + "DitherController");
        var controller = go.AddComponent<DitherController>();
        controller.ditherMaterial = ditherMat;
        controller.resting = new DitherController.DitherState { intensity = 1f, steps = 6f, lumaInfluence = 0f, pixelSize = RestingPixelSize, duotone = 1f };
        controller.full = new DitherController.DitherState { intensity = 1f, steps = 6f, lumaInfluence = 0f, pixelSize = 7f, duotone = 1f };
        controller.gameplay = new DitherController.DitherState { intensity = 0f, steps = 6f, lumaInfluence = 1f, pixelSize = 1f, duotone = 0f };
        return go;
    }


    const string FontWordmarkTtfPath = "Assets/FONTS/SpecialElite-Regular.ttf";
    const string FontButtonTtfPath = "Assets/FONTS/IBMPlexMono-SemiBold.ttf";
    const string FontStatTtfPath = "Assets/FONTS/IBMPlexMono-Regular.ttf";
    const string FontWordmarkPath = "Assets/FONTS/MENU_SpecialElite SDF.asset";
    const string FontButtonPath = "Assets/FONTS/MENU_PlexMono-SemiBold SDF.asset";
    const string FontStatPath = "Assets/FONTS/MENU_PlexMono-Regular SDF.asset";

    static TMP_FontAsset GetOrCreateFontAsset(string ttfPath, string assetPath)
    {
        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
        if (existing != null) return existing;

        var ttf = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
        if (ttf == null)
        {
            Warnings.Add("TTF not found: " + ttfPath);
            return null;
        }

        var fontAsset = TMP_FontAsset.CreateFontAsset(
            ttf, 90, 9, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 512, 512,
            AtlasPopulationMode.Dynamic, true);
        fontAsset.name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        AssetDatabase.CreateAsset(fontAsset, assetPath);
        if (fontAsset.atlasTextures != null && fontAsset.atlasTextures.Length > 0 && fontAsset.atlasTextures[0] != null)
        {
            fontAsset.atlasTextures[0].name = fontAsset.name + " Atlas";
            AssetDatabase.AddObjectToAsset(fontAsset.atlasTextures[0], fontAsset);
        }
        if (fontAsset.material != null)
        {
            fontAsset.material.name = fontAsset.name + " Material";
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
        }
        AssetDatabase.SaveAssets();
        return fontAsset;
    }
    const string ConfirmClipPath = "Assets/sfx/ui/ui_confirm.wav";
    const string FlickerClipPath = "Assets/sfx/ui/ui_flicker.wav";
    const string WindClipPath = "Assets/sfx/ui/ui_wind.wav";
    const string StingClipPath = "Assets/sfx/ui/ui_sting.wav";

    static readonly Color ColorParchment = new Color(0.847f, 0.824f, 0.769f);
    static readonly Color ColorBlood = new Color(0.757f, 0.071f, 0.114f);
    static readonly Color ColorCourierRed = new Color(0.851f, 0.137f, 0.114f);
    static readonly Color ColorMuted = new Color(0.541f, 0.522f, 0.482f);

    static UIRefs BuildUI(Camera cam)
    {
        var canvasGO = GameObject.Find("Canvas");
        if (canvasGO == null)
        {
            Warnings.Add("Canvas not found in scene; UI not built.");
            return default;
        }

        var canvas = canvasGO.GetComponent<Canvas>();
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 1f;
        }
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        var existingMainMenu = Object.FindFirstObjectByType<MainMenu>();
        Image existingFadeRef = existingMainMenu != null ? existingMainMenu.fadePanel : null;

        Image fadePanel = null;
        var toDelete = new List<GameObject>();
        foreach (Transform child in canvasGO.transform)
        {
            var img = child.GetComponent<Image>();
            bool looksLikeFade = img != null &&
                                  (img == existingFadeRef || (child.GetComponent<CanvasGroup>() == null && child.name.ToLower().Contains("fade")));
            if (looksLikeFade)
            {
                fadePanel = img;
                continue;
            }
            toDelete.Add(child.gameObject);
        }
        foreach (var go in toDelete) Object.DestroyImmediate(go);

        if (fadePanel == null)
        {
            var fadeGO = new GameObject("FadePanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fadeGO.transform.SetParent(canvasGO.transform, false);
            fadePanel = fadeGO.GetComponent<Image>();
            fadePanel.color = Color.black;
        }
        StretchFull(fadePanel.rectTransform);
        fadePanel.raycastTarget = false;
        fadePanel.transform.SetAsLastSibling();

        var fontWordmark = GetOrCreateFontAsset(FontWordmarkTtfPath, FontWordmarkPath);
        var fontButton = GetOrCreateFontAsset(FontButtonTtfPath, FontButtonPath);
        var fontStat = GetOrCreateFontAsset(FontStatTtfPath, FontStatPath);
        var confirmClip = Load<AudioClip>(ConfirmClipPath);
        var flickerClip = Load<AudioClip>(FlickerClipPath);
        var windClip = Load<AudioClip>(WindClipPath);
        var stingClip = Load<AudioClip>(StingClipPath);

        var uiAudioGO = new GameObject(GeneratedPrefix + "UIAudio");
        uiAudioGO.transform.SetParent(canvasGO.transform, false);
        var uiAudioSource = uiAudioGO.AddComponent<AudioSource>();
        uiAudioSource.playOnAwake = false;
        uiAudioSource.spatialBlend = 0f;

        var scrimTex = BuildScrimTexture();
        var scrimGO = new GameObject(GeneratedPrefix + "Scrim", typeof(RectTransform), typeof(CanvasRenderer));
        scrimGO.transform.SetParent(canvasGO.transform, false);
        var scrimRect = (RectTransform)scrimGO.transform;
        scrimRect.anchorMin = new Vector2(0f, 0f);
        scrimRect.anchorMax = new Vector2(0f, 1f);
        scrimRect.pivot = new Vector2(0f, 0.5f);
        scrimRect.anchoredPosition = Vector2.zero;
        scrimRect.sizeDelta = new Vector2(760f, 0f);
        if (scrimTex != null)
        {
            var scrimRaw = scrimGO.AddComponent<RawImage>();
            scrimRaw.texture = scrimTex;
            scrimRaw.color = new Color(0f, 0f, 0f, 1f);
            scrimRaw.raycastTarget = false;
        }
        else
        {
            Warnings.Add("Scrim texture failed to load; falling back to flat Image scrim.");
            var scrimImg = scrimGO.AddComponent<Image>();
            scrimImg.color = new Color(0f, 0f, 0f, 0.55f);
            scrimImg.raycastTarget = false;
        }

        var logoGO = new GameObject(GeneratedPrefix + "Logo", typeof(RectTransform), typeof(CanvasGroup));
        logoGO.transform.SetParent(canvasGO.transform, false);
        var logoRect = (RectTransform)logoGO.transform;
        AnchorTopLeft(logoRect, new Vector2(120f, -100f), new Vector2(700f, 300f));

        var undeadText = CreateTMP("Undead", logoGO.transform, "UNDEAD", fontWordmark, 100f, ColorParchment, TextAlignmentOptions.TopLeft);
        AnchorTopLeft((RectTransform)undeadText.transform, new Vector2(0f, 0f), new Vector2(700f, 120f));
        undeadText.characterSpacing = 2f;

        var overlayCanvasGO = new GameObject(GeneratedPrefix + "OverlayCanvas", typeof(Canvas), typeof(CanvasScaler));
        var overlayCanvas = overlayCanvasGO.GetComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = 100;
        var overlayScaler = overlayCanvasGO.GetComponent<CanvasScaler>();
        overlayScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        overlayScaler.referenceResolution = new Vector2(1920f, 1080f);
        overlayScaler.matchWidthOrHeight = 0.5f;

        var courierText = CreateTMP("Courier", overlayCanvasGO.transform, "COURIER", fontWordmark, 100f, ColorCourierRed, TextAlignmentOptions.TopLeft);
        AnchorTopLeft((RectTransform)courierText.transform, new Vector2(120f, -196f), new Vector2(700f, 120f));
        courierText.characterSpacing = 2f;
        var courierGroup = courierText.gameObject.AddComponent<CanvasGroup>();

        var logoFlicker = logoGO.AddComponent<LogoFlicker>();
        logoFlicker.group = logoGO.GetComponent<CanvasGroup>();
        logoFlicker.secondaryGroup = courierGroup;
        logoFlicker.jitterTarget = logoRect;
        logoFlicker.audioSource = uiAudioSource;
        logoFlicker.buzzClip = flickerClip;

        var statText = CreateTMP(GeneratedPrefix + "Stat", canvasGO.transform, string.Empty, fontStat, 15f, ColorMuted, TextAlignmentOptions.TopLeft);
        AnchorTopLeft((RectTransform)statText.transform, new Vector2(120f, -430f), new Vector2(700f, 40f));
        statText.characterSpacing = 2f;

        var buttonsRoot = new GameObject(GeneratedPrefix + "Buttons", typeof(RectTransform));
        buttonsRoot.transform.SetParent(canvasGO.transform, false);
        AnchorTopLeft((RectTransform)buttonsRoot.transform, new Vector2(120f, -470f), new Vector2(400f, 320f));

        string[] labels = { "PLAY", "LEADERBOARD", "LINK ACCOUNT", "QUIT" };
        var buttons = new MenuButton[labels.Length];
        Selectable prevSelectable = null;
        for (int i = 0; i < labels.Length; i++)
        {
            var btnGO = new GameObject("Button_" + labels[i], typeof(RectTransform), typeof(CanvasGroup));
            btnGO.transform.SetParent(buttonsRoot.transform, false);
            var rect = (RectTransform)btnGO.transform;
            AnchorTopLeft(rect, new Vector2(0f, -i * 72f), new Vector2(380f, 70f));

            var bgGO = new GameObject("BG", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bgGO.transform.SetParent(btnGO.transform, false);
            StretchFull((RectTransform)bgGO.transform);
            var bgImg = bgGO.GetComponent<Image>();
            bgImg.color = new Color(1f, 1f, 1f, 0.001f);
            bgImg.raycastTarget = true;

            var button = bgGO.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = bgImg;

            var labelText = CreateTMP("Label", btnGO.transform, labels[i], fontWordmark, 30f, ColorParchment, TextAlignmentOptions.MidlineLeft);
            AnchorTopLeft((RectTransform)labelText.transform, new Vector2(0f, 0f), new Vector2(380f, 70f));
            labelText.characterSpacing = 2.5f;

            var underlineGO = new GameObject("Underline", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            underlineGO.transform.SetParent(btnGO.transform, false);
            var underlineRect = (RectTransform)underlineGO.transform;
            underlineRect.anchorMin = new Vector2(0f, 0f);
            underlineRect.anchorMax = new Vector2(0f, 0f);
            underlineRect.pivot = new Vector2(0f, 0.5f);
            underlineRect.sizeDelta = new Vector2(320f, 2f);
            underlineRect.anchoredPosition = new Vector2(0f, 6f);
            underlineRect.localScale = new Vector3(0f, 1f, 1f);
            underlineGO.GetComponent<Image>().color = ColorBlood;

            var menuButton = btnGO.AddComponent<MenuButton>();
            menuButton.label = labelText;
            menuButton.underline = underlineRect;
            menuButton.normalColor = new Color(0.663f, 0.643f, 0.600f);
            menuButton.accentColor = ColorBlood;
            menuButton.hoverLabelColor = new Color(0.949f, 0.933f, 0.890f);
            menuButton.hoverOffsetX = 20f;
            menuButton.hoverJitterX = 0.9f;
            menuButton.hoverJitterY = 0.35f;
            menuButton.hoverJitterSpeed = 10f;
            menuButton.hoverLiftY = -2f;
            menuButton.hoverScale = 1.04f;
            menuButton.audioSource = uiAudioSource;
            menuButton.confirmClip = confirmClip;

            if (prevSelectable != null)
            {
                var nav = prevSelectable.navigation;
                nav.mode = Navigation.Mode.Explicit;
                nav.selectOnDown = button;
                prevSelectable.navigation = nav;

                var thisNav = button.navigation;
                thisNav.mode = Navigation.Mode.Explicit;
                thisNav.selectOnUp = prevSelectable;
                button.navigation = thisNav;
            }
            prevSelectable = button;

            buttons[i] = menuButton;

            if (labels[i] == "QUIT") btnGO.AddComponent<WebGLHide>();
        }

        var footerGO = new GameObject(GeneratedPrefix + "Footer", typeof(RectTransform), typeof(CanvasGroup));
        footerGO.transform.SetParent(canvasGO.transform, false);
        var footerGroup = footerGO.GetComponent<CanvasGroup>();
        footerGroup.alpha = 0f;
        StretchFull((RectTransform)footerGO.transform);

        var footerLeft = CreateTMP("FooterLeft", footerGO.transform, string.Empty, fontStat, 12f, ColorMuted, TextAlignmentOptions.BottomLeft);
        var footerLeftRect = (RectTransform)footerLeft.transform;
        footerLeftRect.anchorMin = new Vector2(0f, 0f);
        footerLeftRect.anchorMax = new Vector2(0f, 0f);
        footerLeftRect.pivot = new Vector2(0f, 0f);
        footerLeftRect.anchoredPosition = new Vector2(48f, 36f);
        footerLeftRect.sizeDelta = new Vector2(600f, 24f);
        footerLeft.characterSpacing = 4f;

        var footerRight = CreateTMP("FooterRight", footerGO.transform, string.Empty, fontStat, 12f, ColorMuted, TextAlignmentOptions.BottomRight);
        var footerRightRect = (RectTransform)footerRight.transform;
        footerRightRect.anchorMin = new Vector2(1f, 0f);
        footerRightRect.anchorMax = new Vector2(1f, 0f);
        footerRightRect.pivot = new Vector2(1f, 0f);
        footerRightRect.anchoredPosition = new Vector2(-48f, 36f);
        footerRightRect.sizeDelta = new Vector2(600f, 24f);
        footerRight.characterSpacing = 4f;

        var sequencer = canvasGO.GetComponent<MenuSequencer>();
        if (sequencer == null) sequencer = canvasGO.AddComponent<MenuSequencer>();
        sequencer.fadePanel = fadePanel;
        sequencer.logo = logoFlicker;
        sequencer.buttons = buttons;
        sequencer.statText = statText;
        sequencer.footer = footerGroup;
        sequencer.statLabelFormat = "MOST WAVES SURVIVED — {0}";

        var mainMenu = Object.FindFirstObjectByType<MainMenu>();
        var saveLoad = Object.FindFirstObjectByType<SaveLoadManager>();
        if (mainMenu == null) Warnings.Add("MainMenu component not found in scene; PLAY/QUIT buttons unwired.");
        if (saveLoad == null) Warnings.Add("SaveLoadManager component not found in scene; LEADERBOARD/LINK ACCOUNT buttons unwired.");

        WireButton(buttons[0], mainMenu != null ? (UnityEngine.Object)mainMenu : null, "StartNewGame");
        WireButton(buttons[1], saveLoad != null ? (UnityEngine.Object)saveLoad : null, "OpenPlayerProfile");
        WireButton(buttons[2], saveLoad != null ? (UnityEngine.Object)saveLoad : null, "GenerateVerificationCode");
        WireButton(buttons[3], mainMenu != null ? (UnityEngine.Object)mainMenu : null, "ExitApplication");

        if (mainMenu != null)
        {
            mainMenu.sequencer = sequencer;
            mainMenu.highScoreUI = statText;
            mainMenu.fadePanel = fadePanel;
        }

        var ambienceGO = new GameObject(GeneratedPrefix + "Ambience");
        ambienceGO.transform.SetParent(canvasGO.transform, false);
        var ambience = ambienceGO.AddComponent<MenuAmbience>();
        var windSourceGO = new GameObject("WindSource");
        windSourceGO.transform.SetParent(ambienceGO.transform, false);
        var windSource = windSourceGO.AddComponent<AudioSource>();
        windSource.clip = windClip;
        windSource.loop = true;
        windSource.volume = 0.35f;
        windSource.spatialBlend = 0f;
        windSource.playOnAwake = false;
        ambience.windSource = windSource;
        ambience.groanClips = LoadTybugGroans();
        ambience.listenerCamera = cam != null ? cam.transform : null;

        var scareGO = new GameObject(GeneratedPrefix + "Scare");
        var scare = scareGO.AddComponent<MenuScare>();
        var scareZombie = InstantiatePrefabAt(ZombiePrefabPath, scareGO.transform);
        if (scareZombie != null)
        {
            scareZombie.name = "ScareZombie";
            StripZombieGameplayComponents(scareZombie);
            scareZombie.transform.localPosition = new Vector3(20f, 0f, 20f);
            scareZombie.SetActive(false);
            scare.zombie = scareZombie.GetComponentInChildren<Animator>(true);
            scare.zombieRoot = scareZombie.transform;
        }
        else
        {
            Warnings.Add("Could not instantiate scare zombie instance.");
        }
        scare.cameraRig = cam != null ? cam.GetComponent<MenuCameraRig>() : null;
        scare.cameraTransform = cam != null ? cam.transform : null;
        var stingSourceGO = new GameObject("StingSource");
        stingSourceGO.transform.SetParent(scareGO.transform, false);
        var stingSource = stingSourceGO.AddComponent<AudioSource>();
        stingSource.playOnAwake = false;
        stingSource.spatialBlend = 0f;
        scare.stingSource = stingSource;
        scare.stingClip = stingClip;

        return new UIRefs
        {
            fadePanel = fadePanel,
            logo = logoFlicker,
            buttons = buttons,
            statText = statText,
            sequencer = sequencer
        };
    }

    static AudioClip[] LoadTybugGroans()
    {
        string[] paths =
        {
            "Assets/Tybug Studios/Zombie Voice Pack - Free/Zombie Moan/zombie_moan_001.wav",
            "Assets/Tybug Studios/Zombie Voice Pack - Free/Zombie Growl/zombie_growl_023.wav",
            "Assets/Tybug Studios/Zombie Voice Pack - Free/Zombie Growl/zombie_growl_010.wav",
            "Assets/Tybug Studios/Zombie Voice Pack - Free/Zombie Grunt/zombie_grunt_006.wav",
        };
        var clips = new List<AudioClip>();
        foreach (var p in paths)
        {
            var c = AssetDatabase.LoadAssetAtPath<AudioClip>(p);
            if (c != null) clips.Add(c);
            else Warnings.Add("Missing groan clip: " + p);
        }
        return clips.ToArray();
    }

    static void WireButton(MenuButton menuButton, Object target, string methodName)
    {
        var button = menuButton.GetComponentInChildren<Button>();
        if (button == null || target == null) return;
        UnityEventTools.AddPersistentListener(button.onClick, System.Delegate.CreateDelegate(
            typeof(UnityEngine.Events.UnityAction), target, methodName) as UnityEngine.Events.UnityAction);
    }

    static TMP_Text CreateTMP(string name, Transform parent, string text, TMP_FontAsset font, float size, Color color, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        if (font != null) tmp.font = font;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = align;
        tmp.raycastTarget = false;
        return tmp;
    }

    static void AnchorTopLeft(RectTransform rect, Vector2 anchoredPos, Vector2 size)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;
    }

    static void StretchFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }


    static void WireUp(Camera cam, Transform diorama, GameObject ditherControllerGO, UIRefs ui, Material ditherMat, Animator[] dioramaZombies)
    {

        var bloodGO = new GameObject(GeneratedPrefix + "Blood");
        var blood = bloodGO.AddComponent<MenuBlood>();
        blood.ditherMaterial = ditherMat;
        blood.viewCamera = cam;
        blood.zombies = dioramaZombies;
        blood.firstEventDelay = 8f;
        blood.intervalMin = 14f;
        blood.intervalMax = 24f;
        blood.spreadDuration = 14f;
        blood.holdDuration = 12f;
        blood.drainDuration = 10f;
        blood.maxRadius = 0.46f;
        blood.splatRadius = 0.14f;
        blood.clickSplatRadius = 0.11f;
        blood.clickSpreadRadius = 0.32f;
        blood.clickSplatTime = 0.08f;
        blood.clickSpreadDuration = 1.65f;
        blood.clickHoldDuration = 0.6f;
        blood.clickFadeDuration = 0.7f;
        if (dioramaZombies == null || dioramaZombies.Length == 0)
            Warnings.Add("No diorama zombie Animators found to wire into MenuBlood.");
    }
}
