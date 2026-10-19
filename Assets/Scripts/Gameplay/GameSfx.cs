using UnityEngine;

public class GameSfx : MonoBehaviour
{
    public static GameSfx Instance { get; private set; }

    [SerializeField] AudioClip digClip;
    [SerializeField] AudioClip gemCollectClip;
    [SerializeField] AudioClip hitClip;
    [SerializeField] AudioClip doorUnlockClip;
    [SerializeField] AudioSource sourceA;
    [SerializeField] AudioSource sourceB;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ConfigureSource(sourceA);
        ConfigureSource(sourceB);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    static void ConfigureSource(AudioSource source)
    {
        if (source == null)
            return;
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
    }

    public void PlayDig()
    {
        PlayOneShot(digClip);
    }

    public void PlayGemCollect()
    {
        PlayOneShot(gemCollectClip);
    }

    public void PlayHit()
    {
        PlayOneShot(hitClip);
    }

    public void PlayDoorUnlock()
    {
        PlayOneShot(doorUnlockClip);
    }

    void PlayOneShot(AudioClip clip)
    {
        if (clip == null || !GameSettings.SoundOn)
            return;

        var source = sourceA != null && !sourceA.isPlaying ? sourceA : sourceB;
        if (source == null)
            source = sourceA;
        if (source == null)
            return;

        source.PlayOneShot(clip);
    }
}
