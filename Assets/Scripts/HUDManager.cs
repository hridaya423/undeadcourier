using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

public class HUDManager : MonoBehaviour
{

    [Header("Ammo")]
    public TextMeshProUGUI magazineAmmoUI;
    public TextMeshProUGUI totalAmmoUI;
    public Image ammoTypeUI;

    [Header("Weapon")]
    public Image activeWeaponUI;
    public Image unActiveWeaponUI;

    [Header("Throwables")]
    public Image lethalUI;
    public TextMeshProUGUI lethalAmountUI;
    
    public Image tacticalUI;
    public TextMeshProUGUI tacticalAmountUI;

    public Sprite emptySlot;
    public Sprite greySlot;

    public GameObject middleDot;
    
    public static HUDManager Instance { get; set; }
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Update()
    {
        if (WeaponManager.Instance == null || WeaponManager.Instance.activeWeaponSlot == null) return;

        Weapon activeWeapon = WeaponManager.Instance.activeWeaponSlot.GetComponentInChildren<Weapon>();
        GameObject unActiveSlot = GetUnActiveWeaponSlot();
        Weapon unActiveWeapon = unActiveSlot != null ? unActiveSlot.GetComponentInChildren<Weapon>() : null;

        if (activeWeapon)
        {
            if (magazineAmmoUI != null) magazineAmmoUI.text = $"{activeWeapon.bulletsLeft}";
            if (totalAmmoUI != null) totalAmmoUI.text = WeaponManager.Instance.CheckAmmoLeftFor(activeWeapon.thisWeaponModel).ToString();

            Weapon.WeaponModel model = activeWeapon.thisWeaponModel;
            if (ammoTypeUI != null) ammoTypeUI.sprite = GetAmmoSprite(model);

            if (activeWeaponUI != null) activeWeaponUI.sprite = GetWeaponSprite(model);

            if (unActiveWeapon && unActiveWeaponUI != null)
            {
                unActiveWeaponUI.sprite = GetWeaponSprite(unActiveWeapon.thisWeaponModel);
            }
        }
        else
        {
            if (magazineAmmoUI != null) magazineAmmoUI.text = "";
            if (totalAmmoUI != null) totalAmmoUI.text = "";
            
            if (ammoTypeUI != null) ammoTypeUI.sprite = emptySlot;
            if (activeWeaponUI != null) activeWeaponUI.sprite = emptySlot;
            if (unActiveWeaponUI != null) unActiveWeaponUI.sprite = emptySlot;
        }

        if (WeaponManager.Instance.lethalsCount <= 0 && lethalUI != null)
        {
            lethalUI.sprite = greySlot;
        }

        if (WeaponManager.Instance.tacticalsCount <= 0 && tacticalUI != null)
        {
            tacticalUI.sprite = greySlot;
        }
    }

    private Sprite GetWeaponSprite(Weapon.WeaponModel model)
    {
        switch (model)
        {
            case Weapon.WeaponModel.M1911:
                return LoadSprite("M1191_Weapon");
            case Weapon.WeaponModel.AK74:
                return LoadSprite("AK74_Weapon");
            case Weapon.WeaponModel.Uzi:
                return LoadSprite("Uzi_Weapon");
            case Weapon.WeaponModel.Shotgun:
                return null;
            default:
                return null;
        }
    }

    private Sprite GetAmmoSprite(Weapon.WeaponModel model)
    {
        switch (model)
        {
            case Weapon.WeaponModel.M1911:
                return LoadSprite("PistolAmmo");
            case Weapon.WeaponModel.AK74:
                return LoadSprite("RifleAmmo");
            case Weapon.WeaponModel.Uzi:
                return LoadSprite("SMGAmmo");
            case Weapon.WeaponModel.Shotgun:
                return null;
            default:
                return null;
        }
    }

    private GameObject GetUnActiveWeaponSlot()
    {
        if (WeaponManager.Instance == null || WeaponManager.Instance.weaponSlots == null) return null;

        foreach (GameObject weaponSlot in WeaponManager.Instance.weaponSlots)
        {
            if (weaponSlot != WeaponManager.Instance.activeWeaponSlot)
            {
                return weaponSlot;
            }
        }

        return null;
    }

    internal void UpdateThrowables()
    {

        if (WeaponManager.Instance == null) return;

        if (lethalAmountUI != null) lethalAmountUI.text = WeaponManager.Instance.lethalsCount.ToString();
        if (tacticalAmountUI != null) tacticalAmountUI.text = WeaponManager.Instance.tacticalsCount.ToString();


        switch (WeaponManager.Instance.equippedLethalType)
        {
            case Throwable.ThrowableType.Grenade:
                if (lethalUI != null) lethalUI.sprite = LoadSprite("Grenade");
                break;
        }

        switch (WeaponManager.Instance.equippedTacticalType)
        {
            case Throwable.ThrowableType.Smoke:
                if (tacticalUI != null) tacticalUI.sprite = LoadSprite("Smoke");
                break;
        }
    }

    private Sprite LoadSprite(string resourceName)
    {
        GameObject prefab = Resources.Load<GameObject>(resourceName);
        return prefab != null && prefab.TryGetComponent(out SpriteRenderer spriteRenderer) ? spriteRenderer.sprite : emptySlot;
    }
}

