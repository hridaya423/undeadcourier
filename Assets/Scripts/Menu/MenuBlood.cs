using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class MenuBlood : MonoBehaviour
{
    public Material ditherMaterial;
    public Camera viewCamera;
    public Animator[] zombies;
    public float firstEventDelay = 24f;
    public float intervalMin = 40f;
    public float intervalMax = 70f;
    public float fallDelay = 1.1f;
    public float spreadDuration = 14f;
    public float holdDuration = 12f;
    public float drainDuration = 10f;
    public float maxRadius = 0.46f;
    public float splatRadius = 0.14f;
    public float splatTime = 0.06f;
    public bool enableClickRipple = true;
    public float clickSplatRadius = 0.11f;
    public float clickSpreadRadius = 0.32f;
    public float clickSplatTime = 0.08f;
    public float clickSpreadDuration = 1.65f;
    public float clickHoldDuration = 0.6f;
    public float clickFadeDuration = 0.7f;

    static readonly int CenterId = Shader.PropertyToID("_BloodCenter");
    static readonly int RadiusId = Shader.PropertyToID("_BloodRadius");
    static readonly int AmountId = Shader.PropertyToID("_BloodAmount");
    static readonly int DrainCenterId = Shader.PropertyToID("_BloodDrainCenter");
    static readonly int DrainRadiusId = Shader.PropertyToID("_BloodDrainRadius");
    static readonly int EdgeId = Shader.PropertyToID("_BloodEdge");
    static readonly int SplatSeedId = Shader.PropertyToID("_BloodSplatSeed");

    static readonly Vector2[] Corners =
    {
        new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f)
    };

    Coroutine clickRippleRoutine;

    void Start()
    {
        if (ditherMaterial == null || viewCamera == null) return;
        firstEventDelay = Mathf.Min(firstEventDelay, 10f);
        intervalMin = Mathf.Min(intervalMin, 16f);
        intervalMax = Mathf.Min(intervalMax, 28f);
        maxRadius = Mathf.Min(maxRadius, 0.46f);
        splatRadius = Mathf.Min(splatRadius, 0.14f);
        clickSplatRadius = Mathf.Min(clickSplatRadius, 0.11f);
        clickSpreadRadius = Mathf.Min(clickSpreadRadius, 0.32f);
        ResetBlood();
        if (zombies != null && zombies.Length > 0) StartCoroutine(BloodLoop());
    }

    void Update()
    {
        if (!enableClickRipple || ditherMaterial == null || viewCamera == null) return;

        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
            Vector3 vp = viewCamera.ScreenToViewportPoint(Input.mousePosition);
            TriggerRippleAtViewport(new Vector2(vp.x, vp.y));
        }

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId)) return;
                Vector3 vp = viewCamera.ScreenToViewportPoint(touch.position);
                TriggerRippleAtViewport(new Vector2(vp.x, vp.y));
            }
        }
    }

    void OnDestroy()
    {
        ResetBlood();
    }

    void ResetBlood()
    {
        if (ditherMaterial == null) return;
        ditherMaterial.SetFloat(RadiusId, 0f);
        ditherMaterial.SetFloat(AmountId, 0f);
        ditherMaterial.SetFloat(DrainRadiusId, 0f);
        ditherMaterial.SetFloat(EdgeId, 0.075f);
    }

    public void TriggerRippleAtViewport(Vector2 viewportPoint)
    {
        if (clickRippleRoutine != null) StopCoroutine(clickRippleRoutine);
        clickRippleRoutine = StartCoroutine(ClickRippleRoutine(KeepSplatOnscreen(viewportPoint, clickSpreadRadius)));
    }

    Vector2 KeepSplatOnscreen(Vector2 viewportPoint, float radius)
    {
        float aspect = viewCamera != null ? viewCamera.aspect : 16f / 9f;
        float xMargin = Mathf.Min(0.48f, radius / Mathf.Max(aspect, 0.01f));
        float yMargin = Mathf.Min(0.48f, radius);
        return new Vector2(
            Mathf.Clamp(viewportPoint.x, xMargin, 1f - xMargin),
            Mathf.Clamp(viewportPoint.y, yMargin, 1f - yMargin));
    }

    IEnumerator BloodLoop()
    {
        yield return new WaitForSeconds(firstEventDelay);
        while (true)
        {
            var victim = zombies[Random.Range(0, zombies.Length)];
            if (victim != null && victim.isActiveAndEnabled)
                yield return BloodEvent(victim);
            yield return new WaitForSeconds(Random.Range(intervalMin, intervalMax));
        }
    }

    IEnumerator BloodEvent(Animator victim)
    {
        var shamble = victim.GetComponent<MenuZombieShamble>();
        if (shamble != null) shamble.enabled = false;
        victim.speed = 1f;
        victim.SetTrigger(Random.value < 0.5f ? "DIE1" : "DIE2");

        yield return new WaitForSeconds(fallDelay);

        Vector3 chest = victim.transform.position + Vector3.up * 0.4f;
        Vector3 vp = viewCamera.WorldToViewportPoint(chest);
        Vector2 center = KeepSplatOnscreen(new Vector2(vp.x, vp.y), maxRadius);
        ditherMaterial.SetVector(CenterId, new Vector4(center.x, center.y, 0f, 0f));
        ditherMaterial.SetFloat(SplatSeedId, Random.value * 1000f);
        ditherMaterial.SetFloat(DrainRadiusId, 0f);
        ditherMaterial.SetFloat(AmountId, 1f);

        ditherMaterial.SetFloat(EdgeId, 0.10f);
        float t = 0f;
        while (t < splatTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / splatTime);
            k = 1f - Mathf.Pow(1f - k, 4f);
            ditherMaterial.SetFloat(RadiusId, k * splatRadius);
            yield return null;
        }

        t = 0f;
        while (t < spreadDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / spreadDuration);
            k = 1f - (1f - k) * (1f - k);
            ditherMaterial.SetFloat(RadiusId, Mathf.Lerp(splatRadius, maxRadius, k));
            ditherMaterial.SetFloat(EdgeId, Mathf.Lerp(0.10f, 0.075f, Mathf.Clamp01(t / 2f)));
            yield return null;
        }

        yield return new WaitForSeconds(holdDuration);

        Vector2 corner = Corners[Random.Range(0, Corners.Length)];
        ditherMaterial.SetVector(DrainCenterId, new Vector4(corner.x, corner.y, 0f, 0f));
        t = 0f;
        while (t < drainDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / drainDuration);
            k = k * k * (3f - 2f * k);
            ditherMaterial.SetFloat(DrainRadiusId, k * 2.6f);
            yield return null;
        }
        ResetBlood();

        victim.Rebind();
        if (shamble != null)
        {
            shamble.enabled = true;
            shamble.ApplyWalkState();
        }
    }

    IEnumerator ClickRippleRoutine(Vector2 viewportPoint)
    {
        ditherMaterial.SetVector(CenterId, new Vector4(viewportPoint.x, viewportPoint.y, 0f, 0f));
        ditherMaterial.SetFloat(SplatSeedId, Random.value * 1000f);
        ditherMaterial.SetFloat(DrainRadiusId, 0f);
        ditherMaterial.SetFloat(AmountId, 1f);
        ditherMaterial.SetFloat(EdgeId, 0.08f);

        float t = 0f;
        while (t < clickSplatTime)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / clickSplatTime);
            k = 1f - Mathf.Pow(1f - k, 4f);
            ditherMaterial.SetFloat(RadiusId, Mathf.Lerp(0f, clickSplatRadius, k));
            yield return null;
        }

        t = 0f;
        while (t < clickSpreadDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / clickSpreadDuration);
            k = 1f - (1f - k) * (1f - k);
            ditherMaterial.SetFloat(RadiusId, Mathf.Lerp(clickSplatRadius, clickSpreadRadius, k));
            ditherMaterial.SetFloat(EdgeId, Mathf.Lerp(0.08f, 0.06f, k));
            ditherMaterial.SetFloat(AmountId, Mathf.Lerp(1f, 0.94f, k));
            yield return null;
        }

        if (clickHoldDuration > 0f) yield return new WaitForSecondsRealtime(clickHoldDuration);

        float fadeStartAmount = 1f;
        t = 0f;
        while (t < clickFadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / clickFadeDuration);
            ditherMaterial.SetFloat(AmountId, Mathf.Lerp(fadeStartAmount, 0f, k));
            yield return null;
        }

        ResetBlood();
        clickRippleRoutine = null;
    }
}
