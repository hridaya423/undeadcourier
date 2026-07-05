using UnityEngine;

public class PlayerFlashlight : MonoBehaviour
{
    [SerializeField] Light flashlightLight;
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip toggleClip;

    void Start()
    {
        if (flashlightLight != null) flashlightLight.enabled = true;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (flashlightLight != null) flashlightLight.enabled = !flashlightLight.enabled;
            if (audioSource != null && toggleClip != null) audioSource.PlayOneShot(toggleClip);
        }
    }
}
