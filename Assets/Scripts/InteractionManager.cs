using UnityEngine;

public class InteractionManager : MonoBehaviour
{
    public static InteractionManager Instance { get; set; }
    public Weapon hoveredWeapon = null;
    public AmmoBox hoveredAmmoBox = null;
    public Flashlight hoveredFlashlight = null;
    public Throwable hoveredThrowable = null;

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
        
        bool foundInteractable = false;
        Weapon currentWeapon = null;
        AmmoBox currentAmmoBox = null;
        Flashlight currentFlashlight = null;
        Throwable currentThrowable = null;

        if (Camera.main == null) return;

        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            GameObject objecthitbyraycast = hit.transform.gameObject;

            
            Weapon weaponComponent = objecthitbyraycast.GetComponent<Weapon>();
            if (weaponComponent && !weaponComponent.isActiveWeapon)
            {
                currentWeapon = weaponComponent;
                foundInteractable = true;
                SetOutline(currentWeapon, true);
                if (Input.GetKeyDown(KeyCode.E) && WeaponManager.Instance != null)
                {
                    WeaponManager.Instance.PickupWeapon(currentWeapon.gameObject);
                }
            }

            
            AmmoBox ammoComponent = objecthitbyraycast.GetComponent<AmmoBox>();
            if (ammoComponent)
            {
                currentAmmoBox = ammoComponent;
                foundInteractable = true;
                SetOutline(currentAmmoBox, true);
                if (Input.GetKeyDown(KeyCode.E) && WeaponManager.Instance != null)
                {
                    WeaponManager.Instance.PickupAmmo(currentAmmoBox);
                    Destroy(currentAmmoBox.gameObject);
                }
            }

            
            Throwable throwableComponent = objecthitbyraycast.GetComponent<Throwable>();
            if (throwableComponent)
            {
                currentThrowable = throwableComponent;
                foundInteractable = true;
                SetOutline(currentThrowable, true);
                if (Input.GetKeyDown(KeyCode.E) && WeaponManager.Instance != null)
                {
                    WeaponManager.Instance.PickupThrowable(currentThrowable);
                }
            }

            
            Flashlight flashlightComponent = objecthitbyraycast.GetComponent<Flashlight>();
            if (flashlightComponent)
            {
                currentFlashlight = flashlightComponent;
                foundInteractable = true;
                SetOutline(currentFlashlight, true);
                if (Input.GetKeyDown(KeyCode.E))
                {
                    currentFlashlight.PickUp();
                }
            }
        }

        
        if (!foundInteractable)
        {
            if (hoveredWeapon)
            {
                SetOutline(hoveredWeapon, false);
            }
            if (hoveredAmmoBox)
            {
                SetOutline(hoveredAmmoBox, false);
            }
            if (hoveredThrowable)
            {
                SetOutline(hoveredThrowable, false);
            }
            if (hoveredFlashlight)
            {
                SetOutline(hoveredFlashlight, false);
            }
        }

        
        hoveredWeapon = currentWeapon;
        hoveredAmmoBox = currentAmmoBox;
        hoveredThrowable = currentThrowable;
        hoveredFlashlight = currentFlashlight;
    }

    private void SetOutline(Component target, bool enabled)
    {
        if (target != null && target.TryGetComponent(out Outline outline))
        {
            outline.enabled = enabled;
        }
    }
}
