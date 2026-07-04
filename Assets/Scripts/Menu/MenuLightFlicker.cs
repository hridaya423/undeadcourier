using UnityEngine;

[RequireComponent(typeof(Light))]
public class MenuLightFlicker : MonoBehaviour
{
    public float baseIntensity = 3.5f;
    public float flickerAmount = 0.6f;
    public float speed = 3f;

    Light lightSource;
    float seed;

    void Awake()
    {
        lightSource = GetComponent<Light>();
        seed = Random.value * 1000f;
    }

    void Update()
    {
        float n = Mathf.PerlinNoise(seed, Time.time * speed);
        lightSource.intensity = baseIntensity + (n - 0.5f) * 2f * flickerAmount;
    }
}
