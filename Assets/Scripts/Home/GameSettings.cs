using UnityEngine;

public static class GameSettings
{
    const string SoundKey = "dr_sound";
    const string VibrateKey = "dr_vibrate";

    public static bool SoundOn => PlayerPrefs.GetInt(SoundKey, 1) == 1;

    public static bool VibrateOn => PlayerPrefs.GetInt(VibrateKey, 1) == 1;

    public static void SetSound(bool on)
    {
        PlayerPrefs.SetInt(SoundKey, on ? 1 : 0);
        PlayerPrefs.Save();
        Apply();
    }

    public static void SetVibrate(bool on)
    {
        PlayerPrefs.SetInt(VibrateKey, on ? 1 : 0);
        PlayerPrefs.Save();
        if (on)
            TriggerVibrate();
    }

    public static void Apply()
    {
        AudioListener.volume = SoundOn ? 1f : 0f;
    }

    public static void TriggerVibrate()
    {
        Handheld.Vibrate();
    }
}
