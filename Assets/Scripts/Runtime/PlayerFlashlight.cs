using UnityEngine;

public class PlayerFlashlight : MonoBehaviour
{
    public static PlayerFlashlight Active { get; private set; }

    [SerializeField] Light flashlightLight;
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip toggleClip;
    [SerializeField] float maxBattery = 100f;
    [SerializeField] float batteryDrainPerSecond = 6f;
    [SerializeField] float batteryRechargePerSecond = 2f;
    [SerializeField] float lowBatteryThreshold = 0.18f;
    [SerializeField] float lowBatteryFlickerSpeed = 18f;

    float battery;
    float baseIntensity;
    AudioSource buzzSource;
    float nextFlickerTickTime;

    public float Battery => battery;
    public float Battery01 => maxBattery > 0f ? battery / maxBattery : 0f;
    public bool IsOn => flashlightLight != null && flashlightLight.enabled && battery > 0f;

    void Awake()
    {
        Active = this;
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f;
    }

    void OnDisable()
    {
        if (Active == this) Active = null;
    }

    void Start()
    {
        battery = maxBattery;
        if (flashlightLight != null)
        {
            baseIntensity = flashlightLight.intensity;
            flashlightLight.enabled = true;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            ToggleFlashlight();
        }

        if (flashlightLight != null && flashlightLight.enabled)
        {
            battery = Mathf.Max(0f, battery - batteryDrainPerSecond * Time.deltaTime);
            UpdateFlicker();
            UpdateBuzz(true);
            if (battery <= 0f)
            {
                flashlightLight.enabled = false;
                SoundManager.Instance?.PlayFlashlightDead(audioSource);
            }
        }
        else
        {
            battery = Mathf.Min(maxBattery, battery + batteryRechargePerSecond * Time.deltaTime);
            if (flashlightLight != null) flashlightLight.intensity = baseIntensity;
            UpdateBuzz(false);
        }
    }

    void ToggleFlashlight()
    {
        if (flashlightLight == null) return;
        if (battery <= 0f)
        {
            SoundManager.Instance?.PlayFlashlightDead(audioSource);
            return;
        }

        flashlightLight.enabled = !flashlightLight.enabled;
        if (SoundManager.Instance != null)
        {
            if (flashlightLight.enabled) SoundManager.Instance.PlayFlashlightOn(audioSource);
            else SoundManager.Instance.PlayFlashlightOff(audioSource);
        }
        else if (toggleClip != null)
        {
            audioSource.PlayOneShot(toggleClip);
        }
    }

    void UpdateFlicker()
    {
        if (flashlightLight == null) return;

        if (Battery01 > lowBatteryThreshold)
        {
            flashlightLight.intensity = baseIntensity;
            return;
        }

        float flicker = Mathf.PerlinNoise(Time.time * lowBatteryFlickerSpeed, 0f);
        flashlightLight.intensity = baseIntensity * Mathf.Lerp(0.45f, 1f, flicker);
        if (Time.time >= nextFlickerTickTime && flicker < 0.28f)
        {
            nextFlickerTickTime = Time.time + Random.Range(0.18f, 0.5f);
            SoundManager.Instance?.PlayFlashlightFlicker(audioSource);
        }
    }

    void UpdateBuzz(bool on)
    {
        bool shouldBuzz = on && Battery01 <= lowBatteryThreshold && SoundManager.Instance != null && SoundManager.Instance.flashlightBuzzLoop != null;
        if (!shouldBuzz)
        {
            if (buzzSource != null) buzzSource.Stop();
            return;
        }

        if (buzzSource == null)
        {
            buzzSource = gameObject.AddComponent<AudioSource>();
            buzzSource.spatialBlend = 0f;
            buzzSource.loop = true;
            buzzSource.volume = 0.22f;
        }

        if (buzzSource.isPlaying) return;
        buzzSource.clip = SoundManager.Instance.flashlightBuzzLoop;
        buzzSource.Play();
    }
}
