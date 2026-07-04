using System.Collections;
using UnityEngine;

public class MenuAmbience : MonoBehaviour
{
    public AudioSource windSource;
    public AudioClip[] groanClips;
    public float intervalMin = 8f;
    public float intervalMax = 20f;
    public Transform listenerCamera;

    AudioSource oneShotSource;

    void Start()
    {
        if (windSource != null && windSource.clip != null)
        {
            windSource.loop = true;
            windSource.Play();
        }

        GameObject go = new GameObject("MENU_GroanOneShot");
        go.transform.SetParent(transform, false);
        oneShotSource = go.AddComponent<AudioSource>();
        oneShotSource.spatialBlend = 1f;
        oneShotSource.playOnAwake = false;

        StartCoroutine(GroanLoop());
    }

    IEnumerator GroanLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(intervalMin, intervalMax));
            PlayRandomGroan();
        }
    }

    void PlayRandomGroan()
    {
        if (groanClips == null || groanClips.Length == 0 || oneShotSource == null) return;
        AudioClip clip = groanClips[Random.Range(0, groanClips.Length)];
        if (clip == null) return;

        Transform cam = listenerCamera != null ? listenerCamera : Camera.main != null ? Camera.main.transform : transform;
        float azimuth = Random.Range(0f, 360f);
        float distance = Random.Range(8f, 15f);
        Vector3 offset = Quaternion.Euler(0f, azimuth, 0f) * Vector3.forward * distance;
        oneShotSource.transform.position = cam.position + offset;

        oneShotSource.volume = Random.Range(0.25f, 0.5f);
        oneShotSource.pitch = Random.Range(0.9f, 1.1f);
        oneShotSource.PlayOneShot(clip);
    }
}
