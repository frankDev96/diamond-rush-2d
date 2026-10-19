using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MenuMusicPlayer : MonoBehaviour
{
    public static MenuMusicPlayer Instance { get; private set; }

    [SerializeField] AudioClip track;

    AudioSource source;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        if (track != null)
            source.clip = track;
        GameSettings.Apply();
        Refresh();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void PlayClip(AudioClip clip)
    {
        if (clip == null)
            return;

        if (source == null)
            source = GetComponent<AudioSource>();

        if (source.clip != clip)
        {
            source.clip = clip;
            if (GameSettings.SoundOn)
                source.Play();
        }

        Refresh();
    }

    public void Refresh()
    {
        if (source == null || source.clip == null)
            return;

        if (GameSettings.SoundOn)
        {
            if (!source.isPlaying)
                source.Play();
        }
        else if (source.isPlaying)
        {
            source.Pause();
        }
    }
}
