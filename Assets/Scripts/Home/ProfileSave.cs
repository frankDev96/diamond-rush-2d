using System;
using UnityEngine;

[Serializable]
public class ProfileSlot
{
    public bool occupied;
    public string profileName;
    public string locationName;
    public int level;

    public string Subtitle =>
        occupied ? profileName + " - " + locationName + " Level " + level : "Empty";
}

[Serializable]
public class ProfileFile
{
    public int activeIndex;
    public ProfileSlot[] slots;
}

public static class ProfileSave
{
    const string Key = "dr_profiles_v1";
    const int SlotCount = 3;

    static ProfileFile data;

    public static int ActiveIndex
    {
        get
        {
            EnsureSeeded();
            return data.activeIndex;
        }
    }

    public static ProfileSlot Active
    {
        get
        {
            EnsureSeeded();
            return data.slots[data.activeIndex];
        }
    }

    public static ProfileSlot Get(int index)
    {
        EnsureSeeded();
        return data.slots[index];
    }

    public static void EnsureSeeded()
    {
        if (data != null && data.slots != null && data.slots.Length == SlotCount)
            return;

        if (PlayerPrefs.HasKey(Key))
            data = JsonUtility.FromJson<ProfileFile>(PlayerPrefs.GetString(Key));

        if (data == null || data.slots == null || data.slots.Length != SlotCount)
        {
            data = new ProfileFile
            {
                activeIndex = 0,
                slots = new[]
                {
                    new ProfileSlot
                    {
                        occupied = true,
                        profileName = "Profile 1",
                        locationName = "Angkor Wat",
                        level = 2
                    },
                    new ProfileSlot(),
                    new ProfileSlot()
                }
            };
            Write();
        }

        data.activeIndex = Mathf.Clamp(data.activeIndex, 0, SlotCount - 1);
    }

    public static string LastSavedLabel()
    {
        EnsureSeeded();
        var slot = Active;
        return slot.occupied ? "Last Saved: " + slot.Subtitle : "Last Saved: None";
    }

    public static bool TryNewGame(out string message)
    {
        EnsureSeeded();
        for (var i = 0; i < SlotCount; i++)
        {
            if (data.slots[i].occupied)
                continue;

            data.slots[i] = new ProfileSlot
            {
                occupied = true,
                profileName = "Profile " + (i + 1),
                locationName = "Angkor Wat",
                level = 1
            };
            data.activeIndex = i;
            Write();
            message = "New profile ready: " + data.slots[i].Subtitle;
            return true;
        }

        message = "All three profiles are in use.";
        return false;
    }

    public static void CompleteLevel(int levelIndex)
    {
        EnsureSeeded();
        var slot = Active;
        if (slot == null || !slot.occupied)
            return;

        if (levelIndex == slot.level)
            slot.level = Mathf.Min(12, slot.level + 1);
        Write();
    }

    public static bool TrySelect(int index, out string message)
    {
        EnsureSeeded();
        if (index < 0 || index >= SlotCount || !data.slots[index].occupied)
        {
            message = "That profile is empty.";
            return false;
        }

        data.activeIndex = index;
        Write();
        message = "Loaded " + data.slots[index].Subtitle;
        return true;
    }

    static void Write()
    {
        PlayerPrefs.SetString(Key, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }
}
