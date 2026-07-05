using System;
using System.Collections.Generic;
using UnityEngine;
using static Weapon;

public class WeaponManager : MonoBehaviour
{
    public static WeaponManager Instance { get; set; }

    public List<GameObject> weaponSlots;

    public GameObject activeWeaponSlot;

    [Header("Ammo")]
    public int totalRifleAmmo = 0;
    public int totalPistolAmmo = 0;
    public int totalSMGAmmo = 0;
    public int totalShotgunAmmo = 0;

    public float throwForce = 30f;
    public GameObject grenadePrefab;
    public GameObject throwableSpawn;
    public float forceMultiplier = 0;
    public float forceMultiplierLimit = 3;

    public int lethalsCount = 0;
    public Throwable.ThrowableType equippedLethalType;

    public int tacticalsCount = 0;
    public Throwable.ThrowableType equippedTacticalType;
    public GameObject smokeGrenadePrefab;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (weaponSlots != null && weaponSlots.Count > 0)
        {
            activeWeaponSlot = weaponSlots[0];
        }
        equippedLethalType = Throwable.ThrowableType.None;
        equippedTacticalType = Throwable.ThrowableType.None;
    }

    private void Update()
    {
        if (weaponSlots == null || weaponSlots.Count == 0 || activeWeaponSlot == null) return;

        foreach (GameObject weaponSlot in weaponSlots)
        {
            if (weaponSlot == activeWeaponSlot)
            {
                weaponSlot.SetActive(true);
            }
            else
            {
                weaponSlot.SetActive(false);
            }

        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SwitchActiveSlot(0);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SwitchActiveSlot(1);
        }

        if (Input.GetKey(KeyCode.G) || Input.GetKey(KeyCode.T)) 
        {
            forceMultiplier += Time.deltaTime;

            if (forceMultiplier > forceMultiplierLimit)
            {
                forceMultiplier = forceMultiplierLimit;
            } 
        }

        if (Input.GetKeyUp(KeyCode.G))
        {
            if (lethalsCount > 0)
            {
                ThrowLethal();
            }

            forceMultiplier = 0;
        }

        if (Input.GetKeyUp(KeyCode.T))
        {
            if (tacticalsCount > 0)
            {
                ThrowTactical();
            }

            forceMultiplier = 0;
        }
    }

    
    public void PickupWeapon(GameObject weapon)
    {
        AddWeaponIntoActiveSlot(weapon);
    }

    private void AddWeaponIntoActiveSlot(GameObject weapon)
    {

        if (weapon == null || activeWeaponSlot == null) return;

        DropCurrentWeapon(weapon);
        weapon.transform.SetParent(activeWeaponSlot.transform, false);

        Weapon eweapon = weapon.GetComponent<Weapon>();
        if (eweapon == null) return;

        weapon.transform.localPosition = new Vector3(eweapon.spawnPosition.x, eweapon.spawnPosition.y, eweapon.spawnPosition.z);
        weapon.transform.localRotation = Quaternion.Euler(eweapon.spawnRotation.x, eweapon.spawnRotation.y, eweapon.spawnRotation.z);
        
        eweapon.isActiveWeapon = true;
        eweapon.animator.enabled = true;
        eweapon.SetRenderLayer(true);

    }

    private void DropCurrentWeapon(GameObject weapon)
    {
        if (activeWeaponSlot == null) return;

        if (activeWeaponSlot.transform.childCount > 0)
        {
            var weaponToDrop = activeWeaponSlot.transform.GetChild(0).gameObject;

            Weapon droppedWeapon = weaponToDrop.GetComponent<Weapon>();
            droppedWeapon.isActiveWeapon = false;
            droppedWeapon.SetRenderLayer(false);
            droppedWeapon.enabled = false;

            weaponToDrop.transform.SetParent(weapon.transform.parent);
            weaponToDrop.transform.localPosition = weapon.transform.localPosition;
            weaponToDrop.transform.localRotation = weapon.transform.localRotation;


        }
    }

    public void SwitchActiveSlot(int slotNumber)
    {
        if (weaponSlots == null || slotNumber < 0 || slotNumber >= weaponSlots.Count || weaponSlots[slotNumber] == null)
        {
            return;
        }

        if (activeWeaponSlot != null && activeWeaponSlot.transform.childCount > 0)
        {
            Weapon currentWeapon = activeWeaponSlot.transform.GetChild(0).GetComponent<Weapon>();
            currentWeapon.isActiveWeapon = false;
            currentWeapon.SetRenderLayer(false);

        }

        activeWeaponSlot = weaponSlots[slotNumber];

        if (activeWeaponSlot.transform.childCount > 0)
        {
            Weapon newWeapon = activeWeaponSlot.transform.GetChild(0).GetComponent<Weapon>();
            newWeapon.isActiveWeapon = true;
            newWeapon.SetRenderLayer(true);
        }
    }

    internal void PickupAmmo(AmmoBox ammo)
    {
        switch (ammo.ammoType)
        {
            case AmmoBox.AmmoType.PistolAmmo:
                totalPistolAmmo += ammo.ammoAmount;
                break;
            case AmmoBox.AmmoType.RifleAmmo:
                totalRifleAmmo += ammo.ammoAmount;
                break;
            case AmmoBox.AmmoType.SMGAmmo:
                totalSMGAmmo += ammo.ammoAmount;
                break;
            case AmmoBox.AmmoType.ShotgunAmmo:
                totalShotgunAmmo += ammo.ammoAmount;
                break;
        }
    }



    internal void DecreaseTotalAmmo(int bulletsToReload, Weapon.WeaponModel thisWeaponModel)
    {
        switch (thisWeaponModel)
        {
            case Weapon.WeaponModel.M1911:
                totalPistolAmmo -= bulletsToReload;
                break;
            case Weapon.WeaponModel.AK74:
                totalRifleAmmo -= bulletsToReload;
                break;
            case Weapon.WeaponModel.Uzi:
                 totalSMGAmmo -= bulletsToReload;
                break;
            case Weapon.WeaponModel.Shotgun:
                totalShotgunAmmo -= bulletsToReload;
                break;
        }
    }

    public int CheckAmmoLeftFor(Weapon.WeaponModel thisWeaponModel)
    {
        switch (thisWeaponModel)
        {
            case Weapon.WeaponModel.M1911:
                return WeaponManager.Instance.totalPistolAmmo;
            case Weapon.WeaponModel.AK74:
                return WeaponManager.Instance.totalRifleAmmo;
            case Weapon.WeaponModel.Uzi:
                return WeaponManager.Instance.totalSMGAmmo;
            case Weapon.WeaponModel.Shotgun:
                return WeaponManager.Instance.totalShotgunAmmo;
            default:
                return 0;
        }
    }

    internal void PickupThrowable(Throwable throwable)
    {
        switch (throwable.throwableType)
        {
            case Throwable.ThrowableType.Grenade:
                PickupThrowableAsLethal(Throwable.ThrowableType.Grenade);
                break;
            case Throwable.ThrowableType.Smoke:
                PickupThrowableAsTactical(Throwable.ThrowableType.Smoke);
                break;
        }
    }

    private void PickupThrowableAsTactical(Throwable.ThrowableType tactical)
    {
        if (equippedTacticalType == tactical || equippedTacticalType == Throwable.ThrowableType.None)
        {
            equippedTacticalType = tactical;

            if (tacticalsCount < 2)
            {
                tacticalsCount += 1;
                Destroy(InteractionManager.Instance.hoveredThrowable.gameObject);
                HUDManager.Instance.UpdateThrowables();
            }
            else
            {
                print("Tactical Limit reached");
            }
        }
        else
        {
            print("You already have a tactical equipped");
        }
    }

    private void PickupThrowableAsLethal(Throwable.ThrowableType lethal)
    {
        if (equippedLethalType == lethal || equippedLethalType == Throwable.ThrowableType.None)
        {
            equippedLethalType = lethal;

            if (lethalsCount < 2)
            {
                lethalsCount += 1;
                Destroy(InteractionManager.Instance.hoveredThrowable.gameObject);
                HUDManager.Instance.UpdateThrowables();
            }
            else
            {
                print("Lethals Limit reached");
            }
        }
        else
        {
            print("You already have a lethal equipped");
        }
    }

    private void ThrowLethal()
    {

        GameObject lethalPrefab = GetThrowablePrefab(equippedLethalType);
        GameObject throwable = Instantiate(lethalPrefab, throwableSpawn.transform.position, Camera.main.transform.rotation);

        Rigidbody rb = throwable.GetComponent<Rigidbody>();
        rb.AddForce(Camera.main.transform.forward * (throwForce * forceMultiplier), ForceMode.Impulse);

        throwable.GetComponent<Throwable>().hasBeenThrown = true;

        lethalsCount -= 1;

        if (lethalsCount <= 0)
        {
            equippedLethalType = Throwable.ThrowableType.None;
        }
        HUDManager.Instance.UpdateThrowables();

    }

    private GameObject GetThrowablePrefab(Throwable.ThrowableType equippedType)
    {
        switch (equippedType)
        {
            case Throwable.ThrowableType.Grenade:
                return grenadePrefab;

            case Throwable.ThrowableType.Smoke:
                return smokeGrenadePrefab;
        }

        return new();
    }

    private void ThrowTactical()
    {
        GameObject tacticalPrefab = GetThrowablePrefab(equippedTacticalType);
        GameObject throwable = Instantiate(tacticalPrefab, throwableSpawn.transform.position, Camera.main.transform.rotation);

        Rigidbody rb = throwable.GetComponent<Rigidbody>();
        rb.AddForce(Camera.main.transform.forward * (throwForce * forceMultiplier), ForceMode.Impulse);

        throwable.GetComponent<Throwable>().hasBeenThrown = true;

        tacticalsCount -= 1;

        if (tacticalsCount <= 0)
        {
            equippedTacticalType = Throwable.ThrowableType.None;
        }
        HUDManager.Instance.UpdateThrowables();
    }

}

