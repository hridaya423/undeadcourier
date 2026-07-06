using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public static class BloodDecalSetup
{
    const string PCRendererPath = "Assets/Settings/PC_Renderer.asset";
    const string MobileRendererPath = "Assets/Settings/Mobile_Renderer.asset";
    const string DecalFeatureName = "BloodDecals";

    const string DecalShaderName = "Shader Graphs/Decal";
    const string SourceSplatPath = "Assets/Art/blood-png-1.png";

    const int Res = 512;

    enum Kind { DerivedSplat, Pool, Streak, Droplets }

    struct Variant
    {
        public string alphaPath;
        public string matPath;
        public int seed;
        public Kind kind;
        public float srcCropX, srcCropY, srcCropS, srcRot;
    }

    static readonly Variant[] Variants =
    {
        new Variant { alphaPath = "Assets/Resources/Blood/GAME_BloodAlpha_A.png", matPath = "Assets/Resources/Blood/GAME_BloodDecal_A.mat", seed = 1731, kind = Kind.DerivedSplat, srcCropX = 0.24f, srcCropY = 0.20f, srcCropS = 0.44f, srcRot = 0f },
        new Variant { alphaPath = "Assets/Resources/Blood/GAME_BloodAlpha_B.png", matPath = "Assets/Resources/Blood/GAME_BloodDecal_B.mat", seed = 6042, kind = Kind.DerivedSplat, srcCropX = 0.18f, srcCropY = 0.14f, srcCropS = 0.54f, srcRot = 118f },
        new Variant { alphaPath = "Assets/Resources/Blood/GAME_BloodAlpha_C.png", matPath = "Assets/Resources/Blood/GAME_BloodDecal_C.mat", seed = 2287, kind = Kind.DerivedSplat, srcCropX = 0.30f, srcCropY = 0.26f, srcCropS = 0.38f, srcRot = 241f },
        new Variant { alphaPath = "Assets/Resources/Blood/GAME_BloodPool.png", matPath = "Assets/Resources/Blood/GAME_BloodDecal_Pool.mat", seed = 9153, kind = Kind.Pool },
        new Variant { alphaPath = "Assets/Resources/Blood/GAME_BloodPoolCore.png", matPath = "Assets/Resources/Blood/GAME_BloodDecal_PoolCore.mat", seed = 4471, kind = Kind.Pool },
        new Variant { alphaPath = "Assets/Resources/Blood/GAME_BloodStreak.png", matPath = "Assets/Resources/Blood/GAME_BloodDecal_Streak.mat", seed = 3390, kind = Kind.Streak },
        new Variant { alphaPath = "Assets/Resources/Blood/GAME_BloodDroplets.png", matPath = "Assets/Resources/Blood/GAME_BloodDecal_Droplets.mat", seed = 7715, kind = Kind.Droplets },
    };

    static readonly Color ClotCore = SRGB(0x16, 0x01, 0x02);
    static readonly Color WetBody = SRGB(0x3A, 0x02, 0x05);
    static readonly Color FreshRim = SRGB(0x68, 0x04, 0x08);
    static readonly Color FreshHot = SRGB(0x92, 0x08, 0x0D);

    static Color SRGB(int r, int g, int b) => new Color(r / 255f, g / 255f, b / 255f, 1f);

    static float Smooth(float e0, float e1, float x)
    {
        float t = Mathf.Clamp01((x - e0) / (e1 - e0));
        return t * t * (3f - 2f * t);
    }

    [MenuItem("Tools/Undead Courier/Setup Blood Decals")]
    public static void Setup()
    {
        Shader shader = Shader.Find(DecalShaderName);
        if (shader == null)
        {
            Debug.LogError("[BloodDecalSetup] decal shader not found: " + DecalShaderName);
            return;
        }

        Texture2D src = LoadReadable(SourceSplatPath);
        if (src == null)
            Debug.LogWarning("[BloodDecalSetup] source splat unreadable, splats will be procedural.");

        foreach (Variant v in Variants)
        {
            Generate(v, src);
            BuildMaterial(v.matPath, shader, v.alphaPath);
        }

        if (src != null) UnityEngine.Object.DestroyImmediate(src);

        int addedPC = EnsureFeature(PCRendererPath);
        int addedMobile = EnsureFeature(MobileRendererPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[BloodDecalSetup] done. PC featureAdded={addedPC} Mobile featureAdded={addedMobile}");
    }

    static Texture2D LoadReadable(string path)
    {
        try
        {
            byte[] bytes = System.IO.File.ReadAllBytes(path);
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
            if (ImageConversion.LoadImage(tex, bytes, false)) return tex;
            UnityEngine.Object.DestroyImmediate(tex);
        }
        catch { }
        return null;
    }

    static void Generate(Variant v, Texture2D src)
    {
        var rng = new System.Random(v.seed);
        var pixels = new Color32[Res * Res];

        float[] alpha = new float[Res * Res];
        float[] wet = new float[Res * Res];

        switch (v.kind)
        {
            case Kind.DerivedSplat: BuildDerivedSplat(v, src, rng, alpha, wet); break;
            case Kind.Pool: BuildPool(v, rng, alpha, wet); break;
            case Kind.Streak: BuildStreak(v, rng, alpha, wet); break;
            case Kind.Droplets: BuildDroplets(v, rng, alpha, wet); break;
        }

        for (int i = 0; i < Res * Res; i++)
        {
            float a = Mathf.Clamp01(alpha[i]);
            float w = Mathf.Clamp01(wet[i]);

            Color rgb;
            if (w < 0.5f) rgb = Color.Lerp(ClotCore, WetBody, w * 2f);
            else rgb = Color.Lerp(WetBody, FreshRim, (w - 0.5f) * 2f);

            int px = i % Res, py = i / Res;
            float grain = Mathf.PerlinNoise(px * 0.09f + v.seed * 0.13f, py * 0.09f - v.seed * 0.07f);
            if (grain > 0.86f) rgb = Color.Lerp(rgb, FreshHot, (grain - 0.86f) * 1.5f);
            rgb *= Mathf.Lerp(0.88f, 1.08f, grain);

            pixels[i] = new Color32(
                (byte)Mathf.RoundToInt(Mathf.Clamp01(rgb.r) * 255f),
                (byte)Mathf.RoundToInt(Mathf.Clamp01(rgb.g) * 255f),
                (byte)Mathf.RoundToInt(Mathf.Clamp01(rgb.b) * 255f),
                (byte)Mathf.RoundToInt(a * 255f));
        }

        WritePng(v.alphaPath, pixels);
    }

    static float SampleSrcAlpha(Texture2D src, Variant v, float u, float vv)
    {
        float cx = v.srcCropX + u * v.srcCropS;
        float cy = v.srcCropY + vv * v.srcCropS;
        float ang = v.srcRot * Mathf.Deg2Rad;
        float rx = cx - (v.srcCropX + 0.5f * v.srcCropS);
        float ry = cy - (v.srcCropY + 0.5f * v.srcCropS);
        float rcx = rx * Mathf.Cos(ang) - ry * Mathf.Sin(ang) + (v.srcCropX + 0.5f * v.srcCropS);
        float rcy = rx * Mathf.Sin(ang) + ry * Mathf.Cos(ang) + (v.srcCropY + 0.5f * v.srcCropS);
        rcx = Mathf.Clamp01(rcx);
        rcy = Mathf.Clamp01(rcy);
        Color c = src.GetPixelBilinear(rcx, rcy);
        float nonRed = Mathf.Max(c.g, c.b);
        float redDominance = c.r - nonRed * 1.2f;
        return Smooth(0.04f, 0.38f, c.r) * Smooth(0.03f, 0.16f, redDominance);
    }

    static void BuildDerivedSplat(Variant v, Texture2D src, System.Random rng, float[] alpha, float[] wet)
    {
        float feather = 2.4f / Res;

        int fingerCount = 5 + rng.Next(4);
        var fingers = new Vector4[fingerCount];
        for (int i = 0; i < fingerCount; i++)
        {
            float ang = (float)(rng.NextDouble() * Math.PI * 2.0);
            float reach = 0.24f + (float)rng.NextDouble() * 0.16f;
            float width = 0.010f + (float)rng.NextDouble() * 0.020f;
            fingers[i] = new Vector4(ang, reach, width, (float)rng.NextDouble());
        }

        int satCount = 14 + rng.Next(10);
        var sats = new Vector3[satCount];
        for (int i = 0; i < satCount; i++)
        {
            float ang = (float)(rng.NextDouble() * Math.PI * 2.0);
            float dist = 0.34f + (float)rng.NextDouble() * 0.12f;
            float rad = 0.006f + (float)rng.NextDouble() * 0.018f;
            sats[i] = new Vector3(0.5f + Mathf.Cos(ang) * dist, 0.5f + Mathf.Sin(ang) * dist, rad);
        }

        int mHarm = 5;
        var mAmp = new float[mHarm];
        var mPha = new float[mHarm];
        for (int k = 0; k < mHarm; k++)
        {
            mAmp[k] = (float)rng.NextDouble() * 0.14f / (k + 1);
            mPha[k] = (float)(rng.NextDouble() * Math.PI * 2.0);
        }
        float maskR = 0.30f + (float)rng.NextDouble() * 0.04f;
        float mcx = 0.5f + (float)(rng.NextDouble() - 0.5) * 0.06f;
        float mcy = 0.5f + (float)(rng.NextDouble() - 0.5) * 0.06f;

        for (int y = 0; y < Res; y++)
        {
            float ny = (y + 0.5f) / Res;
            for (int x = 0; x < Res; x++)
            {
                float nx = (x + 0.5f) / Res;
                int idx = y * Res + x;

                float body = 0f;
                if (src != null)
                {
                    float raw = SampleSrcAlpha(src, v, nx, ny);
                    body = Smooth(0.24f, 0.62f, raw);
                }
                else
                {
                    float d = Mathf.Sqrt((nx - 0.5f) * (nx - 0.5f) + (ny - 0.5f) * (ny - 0.5f));
                    float lobe = 0.30f + 0.05f * Mathf.Sin(Mathf.Atan2(ny - 0.5f, nx - 0.5f) * 4f);
                    body = 1f - Smooth(lobe - feather * 4f, lobe, d);
                }

                float a = body;

                for (int i = 0; i < fingerCount; i++)
                {
                    Vector4 fg = fingers[i];
                    float ca = Mathf.Cos(fg.x), sa = Mathf.Sin(fg.x);
                    float pxc = nx - 0.5f, pyc = ny - 0.5f;
                    float along = pxc * ca + pyc * sa;
                    float perp = -pxc * sa + pyc * ca;
                    if (along > 0f && along < fg.y)
                    {
                        float taper = 1f - along / fg.y;
                        float w = fg.z * taper * (0.6f + 0.6f * Mathf.Sin(along * 55f + fg.w * 12f));
                        float ff = (1f - Smooth(w * 0.5f, w, Mathf.Abs(perp))) * taper;
                        if (ff > a) a = ff;
                    }
                }

                float mdx = nx - mcx, mdy = ny - mcy;
                float md = Mathf.Sqrt(mdx * mdx + mdy * mdy);
                float mth = Mathf.Atan2(mdy, mdx);
                float mwob = 0f;
                for (int k = 0; k < mHarm; k++) mwob += mAmp[k] * Mathf.Cos((k + 1) * mth + mPha[k]);
                float edgeNoise = Mathf.PerlinNoise(nx * 22f + v.seed * 0.3f, ny * 22f - v.seed * 0.5f) - 0.5f;
                float mEff = maskR * (1f + mwob) + edgeNoise * 0.05f;
                float island = 1f - Smooth(mEff - 0.006f, mEff + 0.028f, md);
                a *= island;

                float satF = 0f;
                for (int i = 0; i < satCount; i++)
                {
                    Vector3 s = sats[i];
                    float d = Mathf.Sqrt((nx - s.x) * (nx - s.x) + (ny - s.y) * (ny - s.y));
                    float sf = 1f - Smooth(s.z - feather, s.z, d);
                    if (sf > satF) satF = sf;
                }
                a = Mathf.Max(a, satF);

                float breakup = Mathf.PerlinNoise(nx * 34f + v.seed * 0.19f, ny * 34f - v.seed * 0.11f);
                if (a < 0.72f) a *= Mathf.Lerp(0.38f, 1f, breakup);
                alpha[idx] = Mathf.Pow(Mathf.Clamp01(a), 1.18f);

                float cd = Mathf.Sqrt((nx - 0.5f) * (nx - 0.5f) + (ny - 0.5f) * (ny - 0.5f));
                float edgeBoost = Mathf.Clamp01((cd - 0.14f) / 0.16f);
                float clotNoise = Mathf.PerlinNoise(nx * 7f + v.seed, ny * 7f - v.seed);
                float w2 = 0.62f + edgeBoost * 0.38f - (clotNoise < 0.32f ? 0.5f : 0f);
                if (satF > a - 0.001f && satF > 0.1f) w2 = 0.72f;
                wet[idx] = Mathf.Clamp01(w2);
            }
        }
    }

    static void BuildPool(Variant v, System.Random rng, float[] alpha, float[] wet)
    {
        bool core = v.alphaPath.Contains("Core");
        float feather = 2.2f / Res;

        float aspect = core ? (0.75f + (float)rng.NextDouble() * 0.2f) : (0.6f + (float)rng.NextDouble() * 0.25f);
        float rot = (float)(rng.NextDouble() * Math.PI);
        int harmonics = 6;
        var amp = new float[harmonics];
        var pha = new float[harmonics];
        for (int k = 0; k < harmonics; k++)
        {
            amp[k] = (float)rng.NextDouble() * (core ? 0.14f : 0.28f) / (k + 1);
            pha[k] = (float)(rng.NextDouble() * Math.PI * 2.0);
        }
        float baseR = core ? 0.28f : 0.34f;

        int satCount = core ? 4 : 9;
        var sats = new Vector3[satCount];
        for (int i = 0; i < satCount; i++)
        {
            float ang = (float)(rng.NextDouble() * Math.PI * 2.0);
            float dist = baseR + 0.03f + (float)rng.NextDouble() * 0.10f;
            float rad = 0.006f + (float)rng.NextDouble() * 0.022f;
            sats[i] = new Vector3(0.5f + Mathf.Cos(ang) * dist, 0.5f + Mathf.Sin(ang) * dist, rad);
        }

        for (int y = 0; y < Res; y++)
        {
            float ny = (y + 0.5f) / Res;
            for (int x = 0; x < Res; x++)
            {
                float nx = (x + 0.5f) / Res;
                int idx = y * Res + x;

                float dx = nx - 0.5f, dy = ny - 0.5f;
                float rdx = dx * Mathf.Cos(rot) - dy * Mathf.Sin(rot);
                float rdy = (dx * Mathf.Sin(rot) + dy * Mathf.Cos(rot)) / aspect;
                float d = Mathf.Sqrt(rdx * rdx + rdy * rdy);
                float theta = Mathf.Atan2(rdy, rdx);
                float wob = 0f;
                for (int k = 0; k < harmonics; k++) wob += amp[k] * Mathf.Cos((k + 1) * theta + pha[k]);
                float rEff = baseR * (1f + wob);
                float body = 1f - Smooth(rEff - 0.035f, rEff + 0.012f, d);

                float satF = 0f;
                for (int i = 0; i < satCount; i++)
                {
                    Vector3 s = sats[i];
                    float sd = Mathf.Sqrt((nx - s.x) * (nx - s.x) + (ny - s.y) * (ny - s.y));
                    float sf = 1f - Smooth(s.z - feather, s.z, sd);
                    if (sf > satF) satF = sf;
                }

                float a = Mathf.Max(body, satF);
                float rimBreak = Mathf.PerlinNoise(nx * 28f + v.seed * 0.17f, ny * 28f - v.seed * 0.21f);
                if (body < 0.92f) a *= Mathf.Lerp(0.35f, 1f, rimBreak);
                alpha[idx] = Mathf.Pow(Mathf.Clamp01(a), core ? 1.08f : 1.18f);

                float rn = Mathf.Clamp01(d / rEff);
                float w2 = core ? Mathf.Lerp(0.45f, 0.82f, rn) : Mathf.Lerp(0.32f, 0.78f, rn);
                float clotNoise = Mathf.PerlinNoise(nx * 6f + v.seed, ny * 6f - v.seed);
                if (!core && clotNoise < 0.34f && rn < 0.55f) w2 -= 0.45f;
                if (satF > 0.1f) w2 = 0.72f;
                wet[idx] = Mathf.Clamp01(w2);
            }
        }
    }

    static void BuildStreak(Variant v, System.Random rng, float[] alpha, float[] wet)
    {
        float feather = 2.2f / Res;
        int strands = 3 + rng.Next(3);
        var strand = new Vector4[strands];
        for (int i = 0; i < strands; i++)
        {
            float off = -0.10f + (float)rng.NextDouble() * 0.20f;
            float w0 = 0.05f + (float)rng.NextDouble() * 0.05f;
            float len = 0.72f + (float)rng.NextDouble() * 0.18f;
            strand[i] = new Vector4(off, w0, len, (float)rng.NextDouble());
        }

        int satCount = 10;
        var sats = new Vector3[satCount];
        for (int i = 0; i < satCount; i++)
        {
            float along = 0.55f + (float)rng.NextDouble() * 0.42f;
            float side = -0.12f + (float)rng.NextDouble() * 0.24f;
            float rad = 0.005f + (float)rng.NextDouble() * 0.016f;
            sats[i] = new Vector3(along, 0.5f + side, rad);
        }

        for (int y = 0; y < Res; y++)
        {
            float ny = (y + 0.5f) / Res;
            for (int x = 0; x < Res; x++)
            {
                float nx = (x + 0.5f) / Res;
                int idx = y * Res + x;

                float head = nx;
                float a = 0f;
                for (int i = 0; i < strands; i++)
                {
                    Vector4 st = strand[i];
                    float center = 0.5f + st.x * (1f - Mathf.Clamp01((nx - 0.1f) / 0.9f));
                    float taper = Mathf.Clamp01((st.z - nx) / st.z);
                    float bulge = nx < 0.22f ? Mathf.Lerp(1.6f, 1f, nx / 0.22f) : 1f;
                    float w = st.y * taper * bulge * (0.7f + 0.4f * Mathf.Sin(nx * 40f + st.w * 9f));
                    if (nx < st.z && nx > 0.02f && w > 0f)
                    {
                        float ff = 1f - Smooth(w * 0.55f, w, Mathf.Abs(ny - center));
                        ff *= Mathf.Clamp01(taper * 1.3f);
                        if (ff > a) a = ff;
                    }
                }

                float satF = 0f;
                for (int i = 0; i < satCount; i++)
                {
                    Vector3 s = sats[i];
                    float sd = Mathf.Sqrt((nx - s.x) * (nx - s.x) + (ny - s.y) * (ny - s.y));
                    float sf = 1f - Smooth(s.z - feather, s.z, sd);
                    if (sf > satF) satF = sf;
                }
                a = Mathf.Max(a, satF);
                alpha[idx] = Mathf.Pow(Mathf.Clamp01(a), 1.16f);

                float w2 = Mathf.Lerp(0.9f, 0.55f, Mathf.Clamp01(head));
                if (satF > 0.1f) w2 = 0.92f;
                wet[idx] = Mathf.Clamp01(w2);
            }
        }
    }

    static void BuildDroplets(Variant v, System.Random rng, float[] alpha, float[] wet)
    {
        float feather = 1.8f / Res;
        int n = 22 + rng.Next(14);
        var dots = new Vector3[n];
        for (int i = 0; i < n; i++)
        {
            float ang = (float)(rng.NextDouble() * Math.PI * 2.0);
            float dist = (float)rng.NextDouble();
            dist = dist * dist * 0.46f;
            float rad = 0.006f + (float)rng.NextDouble() * (dist < 0.15f ? 0.03f : 0.016f);
            dots[i] = new Vector3(0.5f + Mathf.Cos(ang) * dist, 0.5f + Mathf.Sin(ang) * dist, rad);
        }

        for (int y = 0; y < Res; y++)
        {
            float ny = (y + 0.5f) / Res;
            for (int x = 0; x < Res; x++)
            {
                float nx = (x + 0.5f) / Res;
                int idx = y * Res + x;
                float a = 0f;
                for (int i = 0; i < n; i++)
                {
                    Vector3 dt = dots[i];
                    float ex = (nx - dt.x);
                    float ey = (ny - dt.y) / (0.6f + 0.5f * Mathf.Cos(dt.x * 20f));
                    float d = Mathf.Sqrt(ex * ex + ey * ey);
                    float sf = 1f - Smooth(dt.z - feather, dt.z, d);
                    if (sf > a) a = sf;
                }
                alpha[idx] = Mathf.Pow(Mathf.Clamp01(a), 1.2f);
                wet[idx] = 0.62f;
            }
        }
    }

    static void WritePng(string path, Color32[] pixels)
    {
        var tex = new Texture2D(Res, Res, TextureFormat.RGBA32, false);
        tex.SetPixels32(pixels);
        tex.Apply();
        System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Default;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = true;
            importer.mipMapsPreserveCoverage = true;
            importer.alphaTestReferenceValue = 0.5f;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.sRGBTexture = true;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }
    }

    static void BuildMaterial(string path, Shader shader, string texPath)
    {
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);

        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.shader = shader;

        if (tex != null)
        {
            if (mat.HasProperty("Base_Map")) mat.SetTexture("Base_Map", tex);
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            mat.mainTexture = tex;
        }

        EditorUtility.SetDirty(mat);
    }

    static int EnsureFeature(string rendererPath)
    {
        var rendererData = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(rendererPath);
        if (rendererData == null)
        {
            Debug.LogError("[BloodDecalSetup] renderer not found: " + rendererPath);
            return -1;
        }

        foreach (var f in rendererData.rendererFeatures)
        {
            if (f != null && f is DecalRendererFeature) return 0;
        }

        var feature = ScriptableObject.CreateInstance<DecalRendererFeature>();
        feature.name = DecalFeatureName;
        feature.hideFlags = HideFlags.HideInHierarchy;

        AssetDatabase.AddObjectToAsset(feature, rendererData);

        var so = new SerializedObject(rendererData);
        var featuresProp = so.FindProperty("m_RendererFeatures");
        var mapProp = so.FindProperty("m_RendererFeatureMap");

        featuresProp.arraySize += 1;
        featuresProp.GetArrayElementAtIndex(featuresProp.arraySize - 1).objectReferenceValue = feature;

        mapProp.arraySize += 1;
        long localId = 0;
        if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out _, out localId))
        {
            mapProp.GetArrayElementAtIndex(mapProp.arraySize - 1).longValue = localId;
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(rendererData);
        AssetDatabase.SaveAssets();
        return 1;
    }
}
