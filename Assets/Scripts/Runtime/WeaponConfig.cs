using UnityEngine;

public struct WeaponData
{
    public int damage;
    public float shotDelay;
    public float hipSpread;
    public float adsSpread;
    public float recoilPitchMin;
    public float recoilPitchMax;
    public float recoilYawMin;
    public float recoilYawMax;
    public float recoilSnapSpeed;
    public float recoilRecoverSpeed;
    public float tracerWidth;
    public float noiseRadius;
    public int pelletCount;
}

public static class WeaponConfig
{
    public const float HeadshotMultiplier = 2.5f;

    public static WeaponData Get(Weapon.WeaponModel model)
    {
        switch (model)
        {
            case Weapon.WeaponModel.M1911:
                return new WeaponData
                {
                    damage = 25,
                    shotDelay = 0.3f,
                    hipSpread = 1.2f,
                    adsSpread = 0.2f,
                    recoilPitchMin = 0.8f,
                    recoilPitchMax = 1.2f,
                    recoilYawMin = -0.6f,
                    recoilYawMax = 0.6f,
                    recoilSnapSpeed = 40f,
                    recoilRecoverSpeed = 22f,
                    tracerWidth = 0.02f,
                    noiseRadius = 15f,
                    pelletCount = 1
                };
            case Weapon.WeaponModel.AK74:
                return new WeaponData
                {
                    damage = 22,
                    shotDelay = 0.15f,
                    hipSpread = 2.5f,
                    adsSpread = 0.6f,
                    recoilPitchMin = 1f,
                    recoilPitchMax = 1.6f,
                    recoilYawMin = -1f,
                    recoilYawMax = 1f,
                    recoilSnapSpeed = 45f,
                    recoilRecoverSpeed = 16f,
                    tracerWidth = 0.025f,
                    noiseRadius = 25f,
                    pelletCount = 1
                };
            case Weapon.WeaponModel.Uzi:
                return new WeaponData
                {
                    damage = 15,
                    shotDelay = 0.1f,
                    hipSpread = 3.5f,
                    adsSpread = 1.2f,
                    recoilPitchMin = 0.35f,
                    recoilPitchMax = 0.6f,
                    recoilYawMin = -1f,
                    recoilYawMax = 1f,
                    recoilSnapSpeed = 50f,
                    recoilRecoverSpeed = 26f,
                    tracerWidth = 0.018f,
                    noiseRadius = 20f,
                    pelletCount = 1
                };
            case Weapon.WeaponModel.Shotgun:
                return new WeaponData
                {
                    damage = 12,
                    shotDelay = 0.8f,
                    hipSpread = 8f,
                    adsSpread = 4f,
                    recoilPitchMin = 2.5f,
                    recoilPitchMax = 3.5f,
                    recoilYawMin = -2f,
                    recoilYawMax = 2f,
                    recoilSnapSpeed = 35f,
                    recoilRecoverSpeed = 14f,
                    tracerWidth = 0.03f,
                    noiseRadius = 35f,
                    pelletCount = 8
                };
            default:
                return new WeaponData
                {
                    damage = 20,
                    shotDelay = 0.3f,
                    hipSpread = 1.5f,
                    adsSpread = 0.3f,
                    recoilPitchMin = 2f,
                    recoilPitchMax = 3f,
                    recoilYawMin = -1f,
                    recoilYawMax = 1f,
                    recoilSnapSpeed = 40f,
                    recoilRecoverSpeed = 10f,
                    tracerWidth = 0.02f,
                    noiseRadius = 20f,
                    pelletCount = 1
                };
        }
    }
}
