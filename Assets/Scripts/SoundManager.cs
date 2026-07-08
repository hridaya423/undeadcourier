using UnityEngine;
using static Weapon;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; set; }

    public AudioSource ShootingChannel;
    public AudioSource emptymag;
    public AudioSource reloadpistol;
    public AudioClip m1911shoot;
    public AudioClip ak74shoot;
    public AudioClip uziShoot;
    public AudioSource reloaduzi;
    public AudioSource reloadak;

    public AudioSource throwablesChannel;

    public AudioClip grenadeSound;
    public AudioClip smokeSound;

    public AudioClip zombieWalking;
    public AudioClip zombieChasing;
    public AudioClip zombieAttacking;
    public AudioClip zombieDeath;
    public AudioClip zombieHurt;

    public AudioSource zombieChannel;
    public AudioSource zombieChannel1;

    public AudioSource playerChannel;
    public AudioClip playerHurt;
    public AudioClip playerDeath;

    public AudioClip gameOversfx;

    [Header("Boss Sound Effects")]
    public AudioClip bossSpawnSound;
    public AudioClip bossAttackSound;
    public AudioClip bossSpecialAttackSound;
    public AudioClip bossDeathSound;
    public AudioClip bossEnrageSound;

    
    [Header("Boss Audio Channel")]
    public AudioSource bossChannel;

    [Header("P6 Audio")]
    public AudioClip[] flashlightOnClips;
    public AudioClip[] flashlightOffClips;
    public AudioClip[] flashlightDeadClips;
    public AudioClip[] flashlightFlickerClips;
    public AudioClip flashlightBuzzLoop;
    public AudioClip[] footsteps;
    public AudioClip[] landingClips;
    public AudioClip[] breathingClips;
    public AudioClip[] heartbeatClips;
    public AudioClip[] ambienceClips;
    public AudioClip[] distantZombieClips;
    public AudioClip[] uiStingers;

    AudioSource ambienceSource;
    AudioSource breathingSource;
    AudioSource heartbeatSource;
    AudioSource heartbeatBoostSource;
    float nextDistantStingerTime;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        LoadP6Audio();
        EnsureCoreSources();
    }

    void Start()
    {
        StartAmbience();
    }

    void Update()
    {
        if (distantZombieClips.Length == 0 || playerChannel == null || Time.time < nextDistantStingerTime) return;
        nextDistantStingerTime = Time.time + Random.Range(18f, 42f);
        PlayRandom(playerChannel, distantZombieClips, 0.24f, 0.85f, 1.08f);
    }

    public void PlayShootingSound(WeaponModel weapon)
    {
        switch(weapon)
        {
            case WeaponModel.M1911:
                ShootingChannel.PlayOneShot(m1911shoot);
                break;
            case WeaponModel.AK74:
                ShootingChannel.PlayOneShot(ak74shoot);
                break;
            case WeaponModel.Uzi:
                ShootingChannel.PlayOneShot(uziShoot);
                break;
        }
    }

    public void PlayReloadSound(WeaponModel weapon)
    {
        switch (weapon)
        {
            case WeaponModel.M1911:
                reloadpistol.Play();
                break;
            case WeaponModel.AK74:
                reloadak.Play();
                break;
            case WeaponModel.Uzi:
                reloaduzi.Play();
                break;
        }
    }

    public void PlayFlashlightOn(AudioSource source) => PlayRandom(source, flashlightOnClips, 0.38f, 0.95f, 1.05f);
    public void PlayFlashlightOff(AudioSource source) => PlayRandom(source, flashlightOffClips, 0.34f, 0.92f, 1.03f);
    public void PlayFlashlightDead(AudioSource source) => PlayRandom(source, flashlightDeadClips, 0.32f, 0.85f, 0.98f);
    public void PlayFlashlightFlicker(AudioSource source) => PlayRandom(source, flashlightFlickerClips, 0.18f, 0.9f, 1.15f);
    public void PlayFootstep(Vector3 position, bool sprinting) => PlayRandom(playerChannel, footsteps, sprinting ? 1f : 0.85f, 0.88f, 1.12f);
    public void PlayLanding(Vector3 position, float strength) => PlayRandomAt(position, landingClips, Mathf.Lerp(0.28f, 0.75f, strength), 0.85f, 1.08f, 28f);
    public void SetBreathing(bool active, float intensity)
    {
        if (breathingClips.Length == 0) return;
        if (breathingSource == null) breathingSource = New2DSource("P6 Breathing", true, 0.6f);
        if (!active)
        {
            breathingSource.Stop();
            return;
        }
        intensity = Mathf.Clamp01(intensity);
        AudioClip clip = breathingClips[intensity > 0.65f && breathingClips.Length > 1 ? 1 : 0];
        breathingSource.volume = Mathf.Lerp(0.65f, 1f, intensity);
        breathingSource.pitch = Mathf.Lerp(0.95f, 1.12f, intensity);
        if (breathingSource.clip == clip && breathingSource.isPlaying) return;
        breathingSource.clip = clip;
        breathingSource.loop = true;
        breathingSource.Play();
    }

    public void SetLowHealthHeartbeat(bool active)
    {
        if (heartbeatClips.Length == 0) return;
        if (heartbeatSource == null) heartbeatSource = New2DSource("P6 Heartbeat", true, 1f);
        if (heartbeatBoostSource == null) heartbeatBoostSource = New2DSource("P6 Heartbeat Boost", true, 1f);
        if (!active)
        {
            heartbeatSource.Stop();
            heartbeatBoostSource.Stop();
            return;
        }
        heartbeatSource.clip = heartbeatClips.Length > 1 ? heartbeatClips[1] : heartbeatClips[0];
        float healthStress = Player.Active != null ? 1f - Player.Active.Health01 : 1f;
        heartbeatSource.volume = 1f;
        heartbeatSource.pitch = Mathf.Lerp(0.95f, 1.18f, healthStress);
        heartbeatSource.loop = true;
        if (!heartbeatSource.isPlaying) heartbeatSource.Play();

        heartbeatBoostSource.clip = heartbeatSource.clip;
        heartbeatBoostSource.volume = Mathf.Lerp(0.45f, 1f, healthStress);
        heartbeatBoostSource.pitch = heartbeatSource.pitch;
        heartbeatBoostSource.loop = true;
        if (!heartbeatBoostSource.isPlaying) heartbeatBoostSource.Play();
    }

    public static void PlayRandom(AudioSource source, AudioClip[] clips, float volume = 1f, float minPitch = 0.95f, float maxPitch = 1.05f)
    {
        if (source == null || clips == null || clips.Length == 0) return;
        float oldPitch = source.pitch;
        source.pitch = Random.Range(minPitch, maxPitch);
        source.PlayOneShot(clips[Random.Range(0, clips.Length)], volume);
        source.pitch = oldPitch;
    }

    public static void PlayRandomAt(Vector3 position, AudioClip[] clips, float volume = 1f, float minPitch = 0.95f, float maxPitch = 1.05f, float maxDistance = 35f)
    {
        if (clips == null || clips.Length == 0) return;
        GameObject go = new GameObject("P6 OneShot");
        go.transform.position = position;
        AudioSource source = go.AddComponent<AudioSource>();
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = 2f;
        source.maxDistance = maxDistance;
        source.pitch = Random.Range(minPitch, maxPitch);
        AudioClip clip = clips[Random.Range(0, clips.Length)];
        source.PlayOneShot(clip, volume);
        Destroy(go, clip.length / Mathf.Max(0.1f, source.pitch) + 0.2f);
    }

    void LoadP6Audio()
    {
        flashlightOnClips = LoadIfEmpty(flashlightOnClips, "Audio/P6/Flashlight", "on");
        flashlightOffClips = LoadIfEmpty(flashlightOffClips, "Audio/P6/Flashlight", "off");
        flashlightDeadClips = LoadIfEmpty(flashlightDeadClips, "Audio/P6/Flashlight", "dead");
        flashlightFlickerClips = LoadIfEmpty(flashlightFlickerClips, "Audio/P6/Flashlight", "tick", "flicker");
        if (flashlightBuzzLoop == null) flashlightBuzzLoop = First(Resources.LoadAll<AudioClip>("Audio/P6/Flashlight"), "buzz");
        footsteps = LoadIfEmpty(footsteps, "Audio/P6/Player/Footsteps");
        landingClips = LoadIfEmpty(landingClips, "Audio/P6/Player/Landing");
        breathingClips = LoadIfEmpty(breathingClips, "Audio/P6/Player/Breathing");
        heartbeatClips = LoadIfEmpty(heartbeatClips, "Audio/P6/Player/Heartbeat");
        ambienceClips = LoadIfEmpty(ambienceClips, "Audio/P6/Ambience", "wind");
        distantZombieClips = LoadIfEmpty(distantZombieClips, "Audio/P6/Ambience", "distant", "scream");
        uiStingers = LoadIfEmpty(uiStingers, "Audio/P6/UI");
    }

    static AudioClip[] LoadIfEmpty(AudioClip[] current, string path, params string[] nameFilters)
    {
        if (current != null && current.Length > 0) return current;
        AudioClip[] all = Resources.LoadAll<AudioClip>(path);
        if (nameFilters == null || nameFilters.Length == 0) return all;
        return System.Array.FindAll(all, clip => Matches(clip.name, nameFilters));
    }

    static bool Matches(string name, string[] filters)
    {
        for (int i = 0; i < filters.Length; i++)
        {
            if (name.ToLowerInvariant().Contains(filters[i].ToLowerInvariant())) return true;
        }
        return false;
    }

    static AudioClip First(AudioClip[] clips, string namePart)
    {
        if (clips == null) return null;
        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i].name.ToLowerInvariant().Contains(namePart)) return clips[i];
        }
        return clips.Length > 0 ? clips[0] : null;
    }

    void EnsureCoreSources()
    {
        if (playerChannel == null) playerChannel = New2DSource("Player Channel", false, 1f);
        if (ShootingChannel == null) ShootingChannel = New2DSource("Shooting Channel", false, 1f);
    }

    AudioSource New2DSource(string name, bool loop, float volume)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform);
        AudioSource source = go.AddComponent<AudioSource>();
        source.spatialBlend = 0f;
        source.loop = loop;
        source.volume = volume;
        return source;
    }

    void StartAmbience()
    {
        if (ambienceClips.Length == 0) return;
        ambienceSource = New2DSource("P6 Forest Wind", true, 0.24f);
        ambienceSource.clip = ambienceClips[0];
        ambienceSource.Play();
        nextDistantStingerTime = Time.time + Random.Range(8f, 20f);
    }

}
