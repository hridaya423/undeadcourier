using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

public class HUDManager : MonoBehaviour
{
    static Sprite whiteSprite;

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

    [Header("Player")]
    public Image staminaUI;
    public TextMeshProUGUI staminaTextUI;
    public Image flashlightBatteryUI;
    public TextMeshProUGUI flashlightBatteryTextUI;

    PlayerMovement playerMovement;
    PlayerFlashlight playerFlashlight;
    
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

    void Start()
    {
        playerMovement = FindAnyObjectByType<PlayerMovement>();
        playerFlashlight = FindAnyObjectByType<PlayerFlashlight>();
        EnsurePlayerBars();
    }

    private void Update()
    {
        UpdatePlayerHUD();

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

    void UpdatePlayerHUD()
    {
        if (playerMovement != null)
        {
            if (staminaUI != null) staminaUI.fillAmount = playerMovement.Stamina01;
            if (staminaTextUI != null) staminaTextUI.text = Mathf.RoundToInt(playerMovement.Stamina).ToString();
        }

        if (playerFlashlight != null)
        {
            if (flashlightBatteryUI != null) flashlightBatteryUI.fillAmount = playerFlashlight.Battery01;
            if (flashlightBatteryTextUI != null) flashlightBatteryTextUI.text = Mathf.RoundToInt(playerFlashlight.Battery).ToString();
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

    void EnsurePlayerBars()
    {
        if (staminaUI == null) staminaUI = CreateBar("Stamina", new Vector2(20f, 92f), new Color(0.9f, 0.78f, 0.28f, 0.9f));
        if (flashlightBatteryUI == null) flashlightBatteryUI = CreateBar("Battery", new Vector2(20f, 66f), new Color(0.3f, 0.72f, 1f, 0.9f));
    }

    Image CreateBar(string label, Vector2 anchoredPosition, Color fillColor)
    {
        Transform parent = GetHudCanvasTransform();
        Transform existing = parent.Find(label + "Bar");
        if (existing != null) return existing.GetComponentInChildren<Image>();

        GameObject root = new GameObject(label + "Bar", typeof(RectTransform));
        root.transform.SetParent(parent, false);
        root.transform.SetAsLastSibling();
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = Vector2.zero;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(140f, 14f);

        Image background = root.AddComponent<Image>();
        background.sprite = GetWhiteSprite();
        background.color = new Color(0f, 0f, 0f, 0.55f);

        GameObject fillObject = new GameObject("Fill", typeof(RectTransform));
        fillObject.transform.SetParent(root.transform, false);
        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(2f, 2f);
        fillRect.offsetMax = new Vector2(-2f, -2f);

        Image fill = fillObject.AddComponent<Image>();
        fill.sprite = GetWhiteSprite();
        fill.color = fillColor;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = 0;
        fill.fillAmount = 1f;
        return fill;
    }

    Transform GetHudCanvasTransform()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null) return canvas.transform;

        canvas = FindAnyObjectByType<Canvas>();
        if (canvas != null) return canvas.transform;

        return transform;
    }

    static Sprite GetWhiteSprite()
    {
        if (whiteSprite == null)
        {
            whiteSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
        }
        return whiteSprite;
    }
}
