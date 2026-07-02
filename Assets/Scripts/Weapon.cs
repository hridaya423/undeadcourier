using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class    Weapon : MonoBehaviour
{
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

    private void Awake()
    {
        readyToShoot = true;
        currentBurst = bulletsPerBurst;
        animator = GetComponent<Animator>();
        bulletsLeft = magazineSize;
        spreadIntensity = hipSpreadIntensity;
    }
    
    void Update()
    {
        if (isActiveWeapon)
        {

            foreach (Transform child in transform)
            {
                child.gameObject.layer = LayerMask.NameToLayer("WeaponRender");
            }

            if (Input.GetMouseButtonDown(1))
            {
                EnterADS();
            }

            if (Input.GetMouseButtonUp(1))
            {
                ExitADS();
            }


            var outline = GetComponent<Outline>();
            if (outline != null)
            {
                outline.enabled = false;
            }

            if (bulletsLeft == 0 && isShooting)
            {
                SoundManager.Instance.emptymag.Play();
            }
            if (currentshootingMode == ShootingMode.Auto)
            {
                isShooting = Input.GetKey(KeyCode.Mouse0);
            }
            else if (currentshootingMode == ShootingMode.Burst || currentshootingMode == ShootingMode.Single)
            {
                isShooting = Input.GetKeyDown(KeyCode.Mouse0);
            }

            if (Input.GetKeyDown(KeyCode.R) && bulletsLeft < magazineSize && !isReloading && WeaponManager.Instance.CheckAmmoLeftFor(thisWeaponModel) > 0)
            {
                Reload();
            }
            if (readyToShoot && isShooting && bulletsLeft > 0)
            {
                currentBurst = bulletsPerBurst;
                FireWeapon();
            }
            else
            {
                foreach (Transform child in transform)
                {
                    child.gameObject.layer = LayerMask.NameToLayer("Default");
                }
            }
           

        }
    }

    private void EnterADS()
    {
        animator.SetTrigger("enterADS");
        isADS = true;
        HUDManager.Instance.middleDot.SetActive(false);
        spreadIntensity = ADSSpreadIntensity;
    }

    private void ExitADS()
    {
        animator.SetTrigger("exitADS");
        isADS = false;
        HUDManager.Instance.middleDot.SetActive(true);
        spreadIntensity = hipSpreadIntensity;
    }

    private void FireWeapon()
    {

        if (bulletsLeft <= 0) return;

        bulletsLeft--;

        if (muzzleEffect != null && muzzleEffect.TryGetComponent(out ParticleSystem muzzleParticles))
        {
            muzzleParticles.Play();
        }
        if (isADS)
        {
            animator.SetTrigger("ADS_RECOIL");
            isADS = true;
            
        }
        else
        {
            animator.SetTrigger("RECOIL");
            isADS = false;
        }
        

        SoundManager.Instance.PlayShootingSound(thisWeaponModel);

        readyToShoot = false;

        Vector3 shootingDirection = CalculateDirectionAndSpread().normalized;
        if (bulletPrefab == null || bulletSpawn == null) return;

        GameObject bullet = Instantiate(bulletPrefab, bulletSpawn.position, bulletSpawn.rotation);

        Bullet bul = bullet.GetComponent<Bullet>();
        if (bul != null)
        {
            bul.bulletDamage = weaponDamage;
        }

        bullet.transform.forward = shootingDirection;

        if (bullet.TryGetComponent(out Rigidbody bulletBody))
        {
            bulletBody.AddForce(shootingDirection * bulletSpeed, ForceMode.Impulse);
        }

        StartCoroutine(DestroyBulletAfterTime(bullet, bulletPrefabLifetime));

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

    private void Reload()
    {
        animator.SetTrigger("RELOAD");
        SoundManager.Instance.PlayReloadSound(thisWeaponModel);
        isReloading = true;
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
    }

    private void ResetShot()
    {
        readyToShoot = true;
        allowReset = true;
    }


    private Vector3 CalculateDirectionAndSpread()
    {
        if (Camera.main == null || bulletSpawn == null)
        {
            return transform.forward;
        }

        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;
        Vector3 targetPoint;
        if (Physics.Raycast(ray, out hit))
        {
            targetPoint = hit.point;
        }
        else
        {
            targetPoint = ray.GetPoint(1000);
        }

        Vector3 direction = targetPoint - bulletSpawn.position;

        float x = UnityEngine.Random.Range(-spreadIntensity, spreadIntensity);

        float y = UnityEngine.Random.Range(-spreadIntensity, spreadIntensity);

        return direction + new Vector3(x, y, 0);
    }

    private IEnumerator DestroyBulletAfterTime(GameObject bullet, float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(bullet);
    }
}

