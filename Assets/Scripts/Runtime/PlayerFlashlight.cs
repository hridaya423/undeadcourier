using UnityEngine;

public class PlayerFlashlight : MonoBehaviour
{
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

    public float Battery => battery;
    public float Battery01 => maxBattery > 0f ? battery / maxBattery : 0f;

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
            if (flashlightLight != null && battery > 0f) flashlightLight.enabled = !flashlightLight.enabled;
            if (audioSource != null && toggleClip != null) audioSource.PlayOneShot(toggleClip);
        }

        if (flashlightLight != null && flashlightLight.enabled)
        {
            battery = Mathf.Max(0f, battery - batteryDrainPerSecond * Time.deltaTime);
            UpdateFlicker();
            if (battery <= 0f) flashlightLight.enabled = false;
        }
        else
        {
            battery = Mathf.Min(maxBattery, battery + batteryRechargePerSecond * Time.deltaTime);
            if (flashlightLight != null) flashlightLight.intensity = baseIntensity;
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
    }
}
