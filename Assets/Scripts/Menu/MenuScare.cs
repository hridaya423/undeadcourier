using System.Collections;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class MenuScare : MonoBehaviour
{
    public Animator zombie;
    public Transform zombieRoot;
    public MenuCameraRig cameraRig;
    public AudioSource stingSource;
    public AudioClip stingClip;
    public float idleTrigger = 45f;
    [Range(0f, 1f)] public float loadChance = 0.125f;
    public float approachSpeed = 4f;
    public float stopDistance = 3f;
    public Transform cameraTransform;

    bool scareUsed;
    bool scarePending;
    float idleTimer;
    Vector3 lastMousePos;

    Vector3 originalPosition;
    Quaternion originalRotation;
    bool originallyActive;

    void Start()
    {
        if (zombieRoot != null)
        {
            originalPosition = zombieRoot.position;
            originalRotation = zombieRoot.rotation;
            originallyActive = zombieRoot.gameObject.activeSelf;
        }

        if (Random.value < loadChance)
        {
            scarePending = true;
            StartCoroutine(DelayedScare(Random.Range(4f, 8f)));
        }
    }

    void Update()
    {
        if (scareUsed || scarePending) return;

        if (InputActiveThisFrame())
        {
            idleTimer = 0f;
        }
        else
        {
            idleTimer += Time.deltaTime;
            if (idleTimer >= idleTrigger)
            {
                scarePending = true;
                StartCoroutine(ScareSequence());
            }
        }
    }

    bool InputActiveThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        bool moved = false;
        if (Mouse.current != null)
        {
            Vector2 pos = Mouse.current.position.ReadValue();
            moved = ((Vector3)pos - lastMousePos).sqrMagnitude > 0.01f;
            lastMousePos = pos;
        }
        bool pressed = (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            || (Mouse.current != null && (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame));
        return moved || pressed;
#else
        bool moved = (Input.mousePosition - lastMousePos).sqrMagnitude > 0.01f;
        lastMousePos = Input.mousePosition;
        return moved || Input.anyKeyDown;
#endif
    }

    IEnumerator DelayedScare(float delay)
    {
        yield return new WaitForSeconds(delay);
        yield return ScareSequence();
    }

    IEnumerator ScareSequence()
    {
        if (scareUsed || zombieRoot == null || zombie == null) yield break;
        scareUsed = true;

        Transform cam = cameraTransform != null ? cameraTransform : (Camera.main != null ? Camera.main.transform : null);
        if (cam == null) yield break;

        zombieRoot.gameObject.SetActive(true);

        Vector3 flatForward = cam.forward;
        flatForward.y = 0f;
        if (flatForward.sqrMagnitude < 0.0001f) flatForward = Vector3.forward;
        flatForward.Normalize();

        Vector3 startPos = cam.position + flatForward * 14f;
        startPos.y = originalPosition.y;
        zombieRoot.position = startPos;
        zombieRoot.rotation = Quaternion.LookRotation(-flatForward, Vector3.up);

        zombie.SetBool("isChasing", true);
        zombie.speed = 1f;

        while (true)
        {
            Vector3 toCam = cam.position - zombieRoot.position;
            toCam.y = 0f;
            float dist = toCam.magnitude;
            if (dist <= stopDistance) break;

            Vector3 dir = toCam.normalized;
            zombieRoot.position += dir * approachSpeed * Time.deltaTime;
            zombieRoot.rotation = Quaternion.LookRotation(dir, Vector3.up);
            yield return null;
        }

        zombie.speed = 0.2f;
        DitherController.Instance?.Spike(0.15f, 0.4f, 1.2f);
        if (stingSource != null && stingClip != null) stingSource.PlayOneShot(stingClip);
        cameraRig?.Shake(0.4f, 0.5f);

        yield return new WaitForSeconds(1.5f);

        zombieRoot.rotation *= Quaternion.Euler(0f, 180f, 0f);
        zombie.SetBool("isChasing", true);
        zombie.speed = 0.35f;

        Vector3 walkDir = zombieRoot.forward;
        float walked = 0f;
        while (walked < 10f)
        {
            float step = 0.35f * approachSpeed * Time.deltaTime * 0.6f;
            zombieRoot.position += walkDir * step;
            walked += step;
            yield return null;
        }

        zombieRoot.position = originalPosition;
        zombieRoot.rotation = originalRotation;
        zombie.speed = 1f;
        zombie.SetBool("isChasing", false);
        if (!originallyActive) zombieRoot.gameObject.SetActive(false);
    }
}
