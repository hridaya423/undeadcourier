using System;
using TMPro;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    public event Action<Weapon> Fired;
    public event Action<Weapon, bool> AdsChanged;
    public event Action<Weapon> ReloadStarted;
    public event Action<Weapon> ReloadCompleted;

    public int weaponDamage;
    public bool isActiveWeapon;
    public bool isShooting, readyToShoot;
    bool allowReset = true;
    public float shootingDelay = 0.2f;
    public int bulletsPerBurst = 3;
    public int currentBurst;
    public float spreadIntensity;
    public float hipSpreadIntensity;
    public float ADSSpreadIntensity;

    public GameObject bulletPrefab;
    public Transform bulletSpawn;
    public float bulletSpeed = 300;
    public float bulletPrefabLifetime = 3f;

    public GameObject muzzleEffect;
    internal Animator animator;

    public float reloadTime;
    public int magazineSize, bulletsLeft;
    public bool isReloading;

    public Vector3 spawnPosition;
    public Vector3 spawnRotation;

    bool isADS;

    const float MaxRaycastDistance = 120f;

    CameraRecoil cameraRecoil;
    WeaponKick weaponKick;
    MuzzleLight muzzleLight;
    Vector3 muzzleEffectLocalPosition;
    Quaternion muzzleEffectLocalRotation;
    bool hasMuzzleEffectTransform;
    bool playedEmptyMagClick;

    public enum WeaponModel
    {
        M1911,
        AK74,
        Shotgun,
        Uzi
    }

    public WeaponModel thisWeaponModel;

    public enum ShootingMode
    {
        Single,
        Burst,
        Auto
    }

    public ShootingMode currentshootingMode;
    public bool IsADS => isADS;

    private void Awake()
    {
        WeaponData config = WeaponConfig.Get(thisWeaponModel);
        weaponDamage = config.damage;
        shootingDelay = config.shotDelay;
        hipSpreadIntensity = config.hipSpread;
        ADSSpreadIntensity = config.adsSpread;

        readyToShoot = true;
        currentBurst = bulletsPerBurst;
        animator = GetComponent<Animator>();
        bulletsLeft = magazineSize;
        spreadIntensity = hipSpreadIntensity;
        if (muzzleEffect != null)
        {
            muzzleEffectLocalPosition = muzzleEffect.transform.localPosition;
            muzzleEffectLocalRotation = muzzleEffect.transform.localRotation;
            hasMuzzleEffectTransform = true;
        }

    }

    void Update()
    {
        if (isActiveWeapon)
        {
            if (Input.GetMouseButtonDown(1))
            {
                EnterADS();
            }

            if (Input.GetMouseButtonUp(1))
            {
                ExitADS();
            }

            if (currentshootingMode == ShootingMode.Auto)
            {
                isShooting = Input.GetKey(KeyCode.Mouse0);
            }
            else if (currentshootingMode == ShootingMode.Burst || currentshootingMode == ShootingMode.Single)
            {
                isShooting = Input.GetKeyDown(KeyCode.Mouse0);
            }

            if (bulletsLeft == 0 && isShooting)
            {
                if (!playedEmptyMagClick && SoundManager.Instance != null && SoundManager.Instance.emptymag != null)
                {
                    SoundManager.Instance.emptymag.Play();
                }
                playedEmptyMagClick = true;
            }
            else if (!isShooting)
            {
                playedEmptyMagClick = false;
            }

            if (Input.GetKeyDown(KeyCode.R) && bulletsLeft < magazineSize && !isReloading && WeaponManager.Instance.CheckAmmoLeftFor(thisWeaponModel) > 0)
            {
                Reload();
            }
            if (readyToShoot && isShooting && bulletsLeft > 0 && !isReloading)
            {
                currentBurst = bulletsPerBurst;
                FireWeapon();
            }
        }
    }

    public void SetRenderLayer(bool weaponRender)
    {
        int layer = LayerMask.NameToLayer("Default");
        SetLayerRecursive(transform, layer);

        if (TryGetComponent(out Collider pickupCollider))
        {
            pickupCollider.enabled = !weaponRender;
        }

        if (weaponRender)
        {
            var outline = GetComponent<Outline>();
            if (outline != null)
            {
                outline.enabled = false;
            }
        }
    }

    private static void SetLayerRecursive(Transform root, int layer)
    {
        root.gameObject.layer = layer;
        foreach (Transform child in root)
        {
            child.gameObject.layer = layer;
            SetLayerRecursive(child, layer);
        }
    }

    private void EnterADS()
    {
        if (animator != null) animator.SetTrigger("enterADS");
        isADS = true;
        if (HUDManager.Instance != null && HUDManager.Instance.middleDot != null) HUDManager.Instance.middleDot.SetActive(false);
        spreadIntensity = ADSSpreadIntensity;
        AdsChanged?.Invoke(this, true);
    }

    private void ExitADS()
    {
        if (animator != null) animator.SetTrigger("exitADS");
        isADS = false;
        if (HUDManager.Instance != null && HUDManager.Instance.middleDot != null) HUDManager.Instance.middleDot.SetActive(true);
        spreadIntensity = hipSpreadIntensity;
        AdsChanged?.Invoke(this, false);
    }

    private void FireWeapon()
    {

        if (bulletsLeft <= 0) return;

        bulletsLeft--;

        Camera shotCamera = Camera.main;
        Vector3 muzzlePoint = GetVisualMuzzlePosition(shotCamera);
        if (isADS && muzzleEffect != null)
        {
            muzzleEffect.transform.position = muzzlePoint;
            if (shotCamera != null) muzzleEffect.transform.rotation = Quaternion.LookRotation(shotCamera.transform.forward, shotCamera.transform.up);
        }
        else if (hasMuzzleEffectTransform && muzzleEffect != null)
        {
            muzzleEffect.transform.localPosition = muzzleEffectLocalPosition;
            muzzleEffect.transform.localRotation = muzzleEffectLocalRotation;
        }

        if (muzzleEffect != null && muzzleEffect.TryGetComponent(out ParticleSystem muzzleParticles))
        {
            muzzleParticles.Play();
        }
        if (isADS)
        {
            if (animator != null) animator.SetTrigger("ADS_RECOIL");
            isADS = true;

        }
        else
        {
            if (animator != null) animator.SetTrigger("RECOIL");
            isADS = false;
        }


        if (SoundManager.Instance != null) SoundManager.Instance.PlayShootingSound(thisWeaponModel);

        readyToShoot = false;

        WeaponData config = WeaponConfig.Get(thisWeaponModel);

        for (int i = 0; i < config.pelletCount; i++)
        {
            FirePellet(config, shotCamera, muzzlePoint);
        }

        GameEvents.RaiseNoiseEmitted(muzzlePoint, config.noiseRadius);

        GetMuzzleLight()?.FlashAt(muzzlePoint);

        float recoilScale = isADS ? 0.5f : 1f;
        GetCameraRecoil()?.Kick(
            config.recoilPitchMin * recoilScale,
            config.recoilPitchMax * recoilScale,
            config.recoilYawMin * recoilScale,
            config.recoilYawMax * recoilScale,
            config.recoilSnapSpeed,
            config.recoilRecoverSpeed);

        GetWeaponKick()?.Kick(
            new Vector3(0f, 0f, -0.02f * config.recoilPitchMax * recoilScale),
            new Vector3(-0.6f * config.recoilPitchMax * recoilScale, 0f, 0f),
            config.recoilRecoverSpeed);

        Fired?.Invoke(this);

        if (allowReset)
        {
            Invoke("ResetShot", shootingDelay);
            allowReset = false;
        }

        if (currentshootingMode == ShootingMode.Burst)
        {
            currentBurst--;
            if (currentBurst > 0 && bulletsLeft > 0)
            {
                Invoke("FireWeapon", shootingDelay);
            }
        }
    }

    private void FirePellet(WeaponData config, Camera shotCamera, Vector3 muzzlePoint)
    {
        if (shotCamera == null) return;

        Transform camTransform = shotCamera.transform;
        float spreadDegrees = isADS ? config.adsSpread : config.hipSpread;

        Vector2 jitter = UnityEngine.Random.insideUnitCircle * spreadDegrees;
        Vector3 direction = Quaternion.AngleAxis(jitter.y, camTransform.up) * Quaternion.AngleAxis(jitter.x, camTransform.right) * camTransform.forward;

        Vector3 rayOrigin = camTransform.position;

        if (Physics.Raycast(rayOrigin, direction, out RaycastHit hit, MaxRaycastDistance))
        {
            HandleHit(hit, rayOrigin, direction, muzzlePoint, config);
        }
        else
        {
            Vector3 missPoint = rayOrigin + direction * MaxRaycastDistance;
            FireTracer(muzzlePoint, missPoint, config.tracerWidth);
        }
    }

    private void HandleHit(RaycastHit hit, Vector3 rayOrigin, Vector3 direction, Vector3 muzzlePoint, WeaponData config)
    {
        FireTracer(muzzlePoint, hit.point, config.tracerWidth);

        HeadHitbox headHitbox = hit.collider.GetComponentInParent<HeadHitbox>();
        if (headHitbox != null && headHitbox.owner != null)
        {
            int headshotDamage = Mathf.RoundToInt(weaponDamage * WeaponConfig.HeadshotMultiplier);
            float bloodLossMl = headHitbox.owner.RegisterBloodHit(headshotDamage, true, thisWeaponModel);
            headHitbox.owner.RegisterHitContext(hit.point, direction, hit.collider, true);
            headHitbox.owner.TakeDamage(headshotDamage);

            SpawnBloodSpray(hit.point, hit.normal, 1.5f);
            SpawnGroundDecal(hit.point, direction, bloodLossMl, true);
            return;
        }

        Enemy enemy = hit.collider.GetComponentInParent<Enemy>();
        if (enemy != null)
        {
            float bloodLossMl = enemy.RegisterBloodHit(weaponDamage, false, thisWeaponModel);
            enemy.RegisterHitContext(hit.point, direction, hit.collider, false);
            enemy.TakeDamage(weaponDamage);

            SpawnBloodSpray(hit.point, hit.normal, 1f);
            SpawnGroundDecal(hit.point, direction, bloodLossMl, false);
            return;
        }

        if (GlobalReferences.Instance != null && GlobalReferences.Instance.bulletImpactEffectprefab != null)
        {
            Instantiate(GlobalReferences.Instance.bulletImpactEffectprefab, hit.point, Quaternion.LookRotation(hit.normal));
        }
    }

    private void SpawnBloodSpray(Vector3 point, Vector3 normal, float scale)
    {
        if (GlobalReferences.Instance == null || GlobalReferences.Instance.bloodSprayEffect == null) return;

        GameObject spray = Instantiate(GlobalReferences.Instance.bloodSprayEffect, point, Quaternion.LookRotation(normal));
        spray.transform.localScale *= scale;
    }

    const float GroundDecalMaxDistance = 7f;
    const float GroundDecalMinNormalY = 0.72f;

    private void FireTracer(Vector3 muzzlePoint, Vector3 endPoint, float width)
    {
        float hitDistance = Vector3.Distance(muzzlePoint, endPoint);
        if (hitDistance < 0.6f) return;

        Vector3 direction = (endPoint - muzzlePoint).normalized;
        float length = Mathf.Min(2.4f, hitDistance);
        Vector3 start = muzzlePoint + direction * 0.04f;
        Vector3 end = muzzlePoint + direction * length;
        TracerPool.Fire(start, end, width);
    }

    private Vector3 GetVisualMuzzlePosition(Camera shotCamera)
    {
        Vector3 fallback = bulletSpawn != null ? bulletSpawn.position : transform.position;
        if (!isADS || shotCamera == null) return fallback;

        Transform cam = shotCamera.transform;
        Vector3 toSpawn = fallback - cam.position;
        float depth = Mathf.Max(0.15f, Vector3.Dot(toSpawn, cam.forward));
        return cam.position + cam.forward * depth;
    }

    private void SpawnGroundDecal(Vector3 hitPoint, Vector3 shotDirection, float bloodLossMl, bool headshot)
    {
        Vector3 rayOrigin = hitPoint + Vector3.up * 0.25f;
        RaycastHit[] hits = Physics.RaycastAll(rayOrigin, Vector3.down, GroundDecalMaxDistance, ~0, QueryTriggerInteraction.Ignore);

        RaycastHit? best = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit candidate = hits[i];

            if (candidate.collider.GetComponentInParent<Enemy>() != null) continue;
            if (candidate.collider.GetComponentInParent<PlayerMovement>() != null) continue;
            if (candidate.normal.y < GroundDecalMinNormalY) continue;

            if (candidate.distance < bestDistance)
            {
                bestDistance = candidate.distance;
                best = candidate;
            }
        }

        if (best.HasValue)
        {
            BloodDecalPool.SpawnHitBlood(best.Value.point, best.Value.normal, shotDirection, bloodLossMl, headshot);
        }
    }

    private CameraRecoil GetCameraRecoil()
    {
        if (cameraRecoil != null) return cameraRecoil;
        if (Camera.main == null) return null;

        cameraRecoil = Camera.main.GetComponent<CameraRecoil>();
        if (cameraRecoil == null)
        {
            cameraRecoil = Camera.main.gameObject.AddComponent<CameraRecoil>();
        }
        return cameraRecoil;
    }

    private WeaponKick GetWeaponKick()
    {
        if (weaponKick != null) return weaponKick;

        weaponKick = GetComponent<WeaponKick>();
        if (weaponKick == null)
        {
            weaponKick = gameObject.AddComponent<WeaponKick>();
        }
        return weaponKick; 
    }

    private MuzzleLight GetMuzzleLight()
    {
        if (muzzleLight != null) return muzzleLight;

        GameObject host = bulletSpawn != null ? bulletSpawn.gameObject : gameObject;
        muzzleLight = host.GetComponent<MuzzleLight>();
        if (muzzleLight == null)
        {
            muzzleLight = host.AddComponent<MuzzleLight>();
        }
        return muzzleLight;
    }

    private void Reload()
    {
        if (animator != null) animator.SetTrigger("RELOAD");
        if (SoundManager.Instance != null) SoundManager.Instance.PlayReloadSound(thisWeaponModel);
        isReloading = true;
        ReloadStarted?.Invoke(this);
        Invoke("ReloadFinished", reloadTime);
    }

    private void ReloadFinished()
    {
        int bulletsNeeded = magazineSize - bulletsLeft;
        int availableAmmo = WeaponManager.Instance.CheckAmmoLeftFor(thisWeaponModel);
        int bulletsToReload = Math.Min(bulletsNeeded, availableAmmo);

        bulletsLeft += bulletsToReload;

        WeaponManager.Instance.DecreaseTotalAmmo(bulletsToReload, thisWeaponModel);

        isReloading = false;
        ReloadCompleted?.Invoke(this);
    }

    private void ResetShot()
    {
        readyToShoot = true;
        allowReset = true;
    }


}
