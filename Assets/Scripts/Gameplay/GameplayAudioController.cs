using System.Collections;
using UnityEngine;

public class GameplayAudioController : MonoBehaviour
{
    public static bool SkipEntryOnce;

    [SerializeField] AudioClip entryMusic;
    [SerializeField] AudioClip levelBgm;
    [SerializeField] AudioSource entrySource;
    [SerializeField] AudioSource bgmSource;
    [SerializeField] float bgmVolume = 0.7f;

    void Awake()
    {
        if (MenuMusicPlayer.Instance != null)
            MenuMusicPlayer.Instance.PauseForGameplay();

        ConfigureMusicSource(entrySource, false);
        ConfigureMusicSource(bgmSource, true);
        GameSettings.Apply();
    }

    void Start()
    {
        var skipEntry = SkipEntryOnce;
        SkipEntryOnce = false;
        StartCoroutine(PlayEntryThenBgm(skipEntry));
    }

    static void ConfigureMusicSource(AudioSource source, bool loop)
    {
        if (source == null)
            return;
        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;
        source.ignoreListenerPause = false;
    }

    // Music always plays; the Sound toggle mutes it through AudioListener.volume so it can be switched back on mid-level.
    IEnumerator PlayEntryThenBgm(bool skipEntry)
    {
        if (!skipEntry && entryMusic != null && entrySource != null)
        {
            entrySource.clip = entryMusic;
            entrySource.loop = false;
            entrySource.Play();
            while (entrySource != null && (entrySource.isPlaying || AudioListener.pause))
                yield return null;
        }

        if (levelBgm != null && bgmSource != null)
        {
            bgmSource.clip = levelBgm;
            bgmSource.loop = true;
            bgmSource.volume = bgmVolume;
            bgmSource.Play();
        }
    }

    void OnDestroy()
    {
        if (entrySource != null && entrySource.isPlaying)
            entrySource.Stop();
        if (bgmSource != null && bgmSource.isPlaying)
            bgmSource.Stop();
    }
}
