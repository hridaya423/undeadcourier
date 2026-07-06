using UnityEngine;
using UnityEngine.Rendering.Universal;

public static class BloodDecalPool
{
    private static DecalProjector[] pool;
    private static Vector3[] positions;
    private static Material[] hitMaterials;
    private static Material streakMaterial;
    private static Material dropletMaterial;
    private static Material poolMaterial;
    private static Material poolCoreMaterial;
    private static int poolCount;
    private static int nextIndex;
    private static Transform poolParent;
    private static Vector3 lastPosition;
    private static float lastTime = -10f;
    private static bool loadFailed;

    private const float MinSpacing = 0.4f;
    private const float MinSpacingSqr = MinSpacing * MinSpacing;

    public static bool HasLastDecal { get; private set; }
    public static Vector3 LastDecalPosition { get; private set; }
    public static Vector3 LastPoolPosition { get; private set; }
    public static bool HasLastPool { get; private set; }

    private static readonly string[] HitMaterialPaths =
    {
        "Blood/GAME_BloodDecal_A",
        "Blood/GAME_BloodDecal_B",
        "Blood/GAME_BloodDecal_C"
    };

    private const string PoolMaterialPath = "Blood/GAME_BloodDecal_Pool";
    private const string PoolCoreMaterialPath = "Blood/GAME_BloodDecal_PoolCore";
    private const string StreakMaterialPath = "Blood/GAME_BloodDecal_Streak";
    private const string DropletMaterialPath = "Blood/GAME_BloodDecal_Droplets";

    private static void EnsureMaterials()
    {
        if (hitMaterials != null || loadFailed) return;

        var loaded = new System.Collections.Generic.List<Material>();
        for (int i = 0; i < HitMaterialPaths.Length; i++)
        {
            var m = Resources.Load<Material>(HitMaterialPaths[i]);
            if (m != null) loaded.Add(m);
        }

        if (loaded.Count == 0)
        {
            loadFailed = true;
            return;
        }

        hitMaterials = loaded.ToArray();
        poolMaterial = Resources.Load<Material>(PoolMaterialPath);
        if (poolMaterial == null) poolMaterial = hitMaterials[0];
        poolCoreMaterial = Resources.Load<Material>(PoolCoreMaterialPath);
        if (poolCoreMaterial == null) poolCoreMaterial = poolMaterial;
        streakMaterial = Resources.Load<Material>(StreakMaterialPath);
        if (streakMaterial == null) streakMaterial = hitMaterials[0];
        dropletMaterial = Resources.Load<Material>(DropletMaterialPath);
        if (dropletMaterial == null) dropletMaterial = hitMaterials[0];
    }

    private static void EnsurePool()
    {
        if (pool != null) return;

        pool = new DecalProjector[QualityTier.MaxDecals];
        positions = new Vector3[QualityTier.MaxDecals];

        GameObject parentObject = new GameObject("BloodDecalPool");
        Object.DontDestroyOnLoad(parentObject);
        poolParent = parentObject.transform;
    }

    public static void Spawn(Vector3 position, Vector3 normal)
    {
        Spawn(position, normal, Vector3.zero);
    }

    public static void Spawn(Vector3 position, Vector3 normal, Vector3 shotDirection)
    {
        EnsureMaterials();
        if (hitMaterials == null) return;
        EnsurePool();

        if (normal.y < 0.72f) return;
        if (Time.time - lastTime < 0.18f && Vector3.SqrMagnitude(position - lastPosition) < 0.64f) return;
        if (IsTooCrowded(position)) return;
        lastTime = Time.time;
        lastPosition = position;

        SpawnHitCore(position, normal, shotDirection);
        LastDecalPosition = position;
        HasLastDecal = true;
    }

    public static void SpawnHitNoThrottle(Vector3 position, Vector3 normal)
    {
        SpawnHitNoThrottle(position, normal, Vector3.zero);
    }

    public static void SpawnHitNoThrottle(Vector3 position, Vector3 normal, Vector3 shotDirection)
    {
        EnsureMaterials();
        if (hitMaterials == null) return;
        EnsurePool();

        if (normal.y < 0.72f) return;
        if (IsTooCrowded(position)) return;

        SpawnHitCore(position, normal, shotDirection);
        LastDecalPosition = position;
        HasLastDecal = true;
    }

    public static void SpawnHitBlood(Vector3 position, Vector3 normal, Vector3 shotDirection, float bloodLossMl, bool headshot)
    {
        EnsureMaterials();
        if (hitMaterials == null) return;
        EnsurePool();

        if (normal.y < 0.72f) return;

        float severity = Mathf.Clamp01(bloodLossMl / 1200f);
        int marks = 1;
        if (bloodLossMl > 550f && Random.value < 0.75f) marks++;
        if (headshot || bloodLossMl > 1100f) marks++;

        Vector3 flow = ProjectOnPlane(shotDirection, normal);
        if (flow.sqrMagnitude < 0.0001f) flow = Vector3.forward;
        flow.Normalize();

        for (int i = 0; i < marks; i++)
        {
            Vector2 scatter = Random.insideUnitCircle * Mathf.Lerp(0.04f, 0.20f, severity);
            Vector3 right = Vector3.Cross(normal, flow).normalized;
            Vector3 offset = right * scatter.x + flow * (scatter.y + Random.Range(0.02f, 0.18f) * severity);
            SpawnHitCore(position + offset, normal, shotDirection, Mathf.Lerp(0.85f, 1.55f, severity) * (headshot ? 1.15f : 1f));
        }

        if (headshot || bloodLossMl > 700f)
        {
            int droplets = headshot ? Random.Range(2, 5) : Random.Range(1, 3);
            for (int i = 0; i < droplets; i++)
            {
                Vector2 scatter = Random.insideUnitCircle * Mathf.Lerp(0.12f, 0.34f, severity);
                Vector3 right = Vector3.Cross(normal, flow).normalized;
                Vector3 offset = right * scatter.x + flow * scatter.y;
                float size = Random.Range(0.025f, 0.07f) * Mathf.Lerp(0.9f, 1.4f, severity);
                PlaceYaw(position + offset, normal, dropletMaterial, size, size * Random.Range(0.8f, 1.25f), Random.Range(0f, 360f));
            }
        }

        LastDecalPosition = position;
        HasLastDecal = true;
    }

    private static void SpawnHitCore(Vector3 position, Vector3 normal, Vector3 shotDirection)
    {
        SpawnHitCore(position, normal, shotDirection, 1f);
    }

    private static void SpawnHitCore(Vector3 position, Vector3 normal, Vector3 shotDirection, float sizeScale)
    {
        Vector3 flow = ProjectOnPlane(shotDirection, normal);
        bool directional = flow.sqrMagnitude > 0.0001f && Random.value < 0.6f;

        if (directional)
        {
            flow.Normalize();
            float jitter = Random.Range(-15f, 15f);
            flow = Quaternion.AngleAxis(jitter, normal) * flow;
            float sizeX = Random.Range(0.08f, 0.14f) * sizeScale;
            float sizeY = sizeX * Random.Range(2.4f, 3.4f);
            PlaceDirectional(position, normal, flow, streakMaterial, sizeX, sizeY);
        }
        else
        {
            Material mat = hitMaterials[Random.Range(0, hitMaterials.Length)];
            float sizeX = Random.Range(0.09f, 0.22f) * sizeScale;
            float sizeY = sizeX * Random.Range(0.82f, 1.18f);
            PlaceYaw(position, normal, mat, sizeX, sizeY, Random.Range(0f, 360f));
        }
    }

    public static void SpawnDeathPool(Vector3 position, Vector3 normal)
    {
        SpawnDeathPool(position, normal, 1600f);
    }

    public static void SpawnDeathPool(Vector3 position, Vector3 normal, float bloodLossMl)
    {
        EnsureMaterials();
        if (hitMaterials == null) return;
        EnsurePool();

        if (normal.y < 0.72f) return;

        float severity = Mathf.Clamp01(bloodLossMl / 3500f);
        float baseSize = Random.Range(0.42f, 0.68f) * Mathf.Lerp(0.85f, 1.75f, severity);
        float sizeY = baseSize * Random.Range(0.58f, 0.88f);
        float poolYaw = Random.Range(0f, 360f);
        PlaceYaw(position, normal, poolMaterial, baseSize, sizeY, poolYaw);

        Vector2 coreOffset = Random.insideUnitCircle * (baseSize * 0.18f);
        Vector3 corePos = position + new Vector3(coreOffset.x, 0f, coreOffset.y);
        float coreSize = Random.Range(0.18f, 0.32f) * Mathf.Lerp(0.9f, 1.45f, severity);
        PlaceYaw(corePos, normal, poolCoreMaterial, coreSize, coreSize * Random.Range(0.85f, 1.1f), Random.Range(0f, 360f));

        int droplets = Mathf.RoundToInt(Mathf.Lerp(3f, 10f, severity));
        for (int i = 0; i < droplets; i++)
        {
            float ang = Random.Range(0f, Mathf.PI * 2f);
            float dist = baseSize * Random.Range(0.5f, 1.1f);
            Vector3 dp = position + new Vector3(Mathf.Cos(ang) * dist, 0f, Mathf.Sin(ang) * dist);
            float ds = Random.Range(0.025f, 0.075f) * Mathf.Lerp(1f, 0.55f, dist / (baseSize * 1.1f));
            PlaceYaw(dp, normal, dropletMaterial, ds, ds, Random.Range(0f, 360f));
        }

        LastPoolPosition = position;
        HasLastPool = true;
        LastDecalPosition = position;
        HasLastDecal = true;
    }

    private static Vector3 ProjectOnPlane(Vector3 v, Vector3 n)
    {
        n = n.normalized;
        return v - n * Vector3.Dot(v, n);
    }

    private static bool IsTooCrowded(Vector3 position)
    {
        for (int i = 0; i < poolCount; i++)
        {
            if (pool[i] == null || !pool[i].gameObject.activeSelf) continue;
            if (Vector3.SqrMagnitude(positions[i] - position) < MinSpacingSqr) return true;
        }
        return false;
    }

    private static void PlaceYaw(Vector3 position, Vector3 normal, Material mat, float sizeX, float sizeY, float yawDeg)
    {
        Vector3 down = -normal.normalized;
        Vector3 up = Mathf.Abs(down.y) > 0.98f ? Vector3.forward : Vector3.up;
        Quaternion look = Quaternion.LookRotation(down, up) * Quaternion.Euler(0f, 0f, yawDeg);
        Place(position, normal, look, mat, sizeX, sizeY);
    }

    private static void PlaceDirectional(Vector3 position, Vector3 normal, Vector3 flow, Material mat, float sizeX, float sizeY)
    {
        Vector3 down = -normal.normalized;
        Quaternion look = Quaternion.LookRotation(down, flow.normalized);
        Place(position, normal, look, mat, sizeX, sizeY);
    }

    private static void Place(Vector3 position, Vector3 normal, Quaternion look, Material mat, float sizeX, float sizeY)
    {
        DecalProjector projector;
        int index;

        if (poolCount < pool.Length)
        {
            index = poolCount;
            poolCount++;

            var go = new GameObject("BloodDecal");
            go.transform.SetParent(poolParent);
            projector = go.AddComponent<DecalProjector>();
            pool[index] = projector;
        }
        else
        {
            index = nextIndex;
            nextIndex = (nextIndex + 1) % pool.Length;
            projector = pool[index];
        }

        projector.material = mat;


        Vector3 place = position + normal * 0.05f;
        projector.transform.SetPositionAndRotation(place, look);
        positions[index] = position;

        projector.size = new Vector3(sizeX, sizeY, 0.6f);
        projector.pivot = new Vector3(0f, 0f, 0.3f);
        projector.fadeFactor = 1f;

        projector.gameObject.SetActive(true);
        projector.enabled = true;
    }
}
