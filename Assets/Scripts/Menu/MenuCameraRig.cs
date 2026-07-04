using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class MenuCameraRig : MonoBehaviour
{
    public float driftAmplitude = 2.5f;
    public float driftPeriod = 20f;
    public float parallaxAmplitude = 1.2f;
    public float parallaxSmoothing = 8f;

    Quaternion initialLocalRotation;
    Vector2 parallaxCurrent;
    Vector2 noiseSeed;

    float shakeAmplitude;
    float shakeDuration;
    float shakeTimer;

    void Awake()
    {
        initialLocalRotation = transform.localRotation;
        noiseSeed = new Vector2(Random.value * 1000f, Random.value * 1000f);
    }

    void Update()
    {
        float t = Time.time;

        float driftPitch = (Mathf.PerlinNoise(noiseSeed.x, t / Mathf.Max(0.01f, driftPeriod)) - 0.5f) * 2f * driftAmplitude;
        float driftYaw = (Mathf.PerlinNoise(noiseSeed.y, t / Mathf.Max(0.01f, driftPeriod)) - 0.5f) * 2f * driftAmplitude;

        Vector2 mouseNorm = GetMouseNormalized();
        Vector2 parallaxTarget = mouseNorm * parallaxAmplitude;
        parallaxCurrent = Vector2.Lerp(parallaxCurrent, parallaxTarget, 1f - Mathf.Exp(-parallaxSmoothing * Time.deltaTime));

        Quaternion shakeRot = Quaternion.identity;
        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.deltaTime;
            float decay = shakeDuration > 0f ? Mathf.Clamp01(shakeTimer / shakeDuration) : 0f;
            float mag = shakeAmplitude * decay;
            shakeRot = Quaternion.Euler(
                (Random.value - 0.5f) * 2f * mag,
                (Random.value - 0.5f) * 2f * mag,
                (Random.value - 0.5f) * 2f * mag * 0.5f);
        }

        Quaternion drift = Quaternion.Euler(driftPitch - parallaxCurrent.y, driftYaw + parallaxCurrent.x, 0f);
        transform.localRotation = initialLocalRotation * drift * shakeRot;
    }

    Vector2 GetMouseNormalized()
    {
        Vector2 mousePos;
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
            mousePos = Mouse.current.position.ReadValue();
        else
            mousePos = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
#else
        mousePos = Input.mousePosition;
#endif
        float nx = (mousePos.x / Mathf.Max(1f, Screen.width) - 0.5f) * 2f;
        float ny = (mousePos.y / Mathf.Max(1f, Screen.height) - 0.5f) * 2f;
        return new Vector2(Mathf.Clamp(nx, -1f, 1f), Mathf.Clamp(ny, -1f, 1f));
    }

    public void Shake(float amplitude, float duration)
    {
        shakeAmplitude = amplitude;
        shakeDuration = duration;
        shakeTimer = duration;
    }
}
