using UnityEngine;

public class ViewmodelMotionDriver : MonoBehaviour
{
    [Header("Motion")]
    [SerializeField] float idleBreathAmplitude = 0.004f;
    [SerializeField] float idleBreathFrequency = 1.35f;
    [SerializeField] float walkBobAmplitude = 0.015f;
    [SerializeField] float walkBobFrequency = 8f;
    [SerializeField] float sprintBobMultiplier = 1.4f;
    [SerializeField] float lookSwayPosition = 0.015f;
    [SerializeField] float lookSwayRotation = 4f;
    [SerializeField] float strafeTilt = 3.5f;
    [SerializeField] float landingDipDistance = 0.055f;
    [SerializeField] float sprintDropDistance = 0.085f;
    [SerializeField] float motionSmoothing = 10f;

    Transform motionRoot;
    Vector3 basePosition;
    Quaternion baseRotation;
    PlayerMovement playerMovement;
    MouseMovement mouseMovement;
    WeaponManager weaponManager;
    float bobTime;

    void Awake()
    {
        motionRoot = transform;
        basePosition = transform.localPosition;
        baseRotation = transform.localRotation;
    }

    void Start()
    {
        playerMovement = FindAnyObjectByType<PlayerMovement>();
        mouseMovement = FindAnyObjectByType<MouseMovement>();
        weaponManager = WeaponManager.Instance;
    }

    void LateUpdate()
    {
        if (playerMovement == null || mouseMovement == null) return;

        Weapon activeWeapon = weaponManager != null ? weaponManager.ActiveWeapon : null;
        bool isAds = activeWeapon != null && activeWeapon.IsADS;

        float moveAmount = playerMovement.HorizontalSpeed01;
        bool sprinting = playerMovement.IsSprinting && !isAds;
        float adsDamp = isAds ? 0.35f : 1f;

        bobTime += Time.deltaTime * Mathf.Lerp(0f, walkBobFrequency * (sprinting ? sprintBobMultiplier : 1f), moveAmount);

        Vector3 targetPosition = basePosition;
        targetPosition.y += Mathf.Sin(Time.time * idleBreathFrequency) * idleBreathAmplitude * adsDamp;

        if (moveAmount > 0.01f)
        {
            float bobAmount = walkBobAmplitude * moveAmount * adsDamp * (sprinting ? sprintBobMultiplier : 1f);
            targetPosition.x += Mathf.Sin(bobTime * 0.5f) * bobAmount;
            targetPosition.y += Mathf.Abs(Mathf.Cos(bobTime)) * bobAmount;
        }

        targetPosition.z -= playerMovement.LandingStrength * landingDipDistance * adsDamp;

        if (sprinting)
        {
            targetPosition.y -= sprintDropDistance;
            targetPosition.x += 0.03f;
        }

        Vector2 lookDelta = mouseMovement.LookDelta;
        targetPosition.x -= lookDelta.x * lookSwayPosition * adsDamp;
        targetPosition.y += lookDelta.y * lookSwayPosition * adsDamp;

        Quaternion targetRotation = baseRotation * Quaternion.Euler(
            -lookDelta.y * lookSwayRotation * adsDamp,
            lookDelta.x * lookSwayRotation * adsDamp,
            -playerMovement.MoveInput.x * strafeTilt * adsDamp - (sprinting ? 7f : 0f));

        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, Time.deltaTime * motionSmoothing);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, Time.deltaTime * motionSmoothing);
    }
}
