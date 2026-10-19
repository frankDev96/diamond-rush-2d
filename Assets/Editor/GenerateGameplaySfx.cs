using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class GenerateGameplaySfx
{
    public const string SfxFolder = "Assets/Audio/Sfx/";

    [MenuItem("Diamond Rush/Generate Gameplay SFX Placeholders")]
    public static void Generate()
    {
        Directory.CreateDirectory(SfxFolder);
        WriteTone(SfxFolder + "dig.wav", 120f, 0.12f, 0.35f, WaveShape.Thump);
        WriteTone(SfxFolder + "gem_collect.wav", 880f, 0.08f, 0.45f, WaveShape.Sine);
        WriteTone(SfxFolder + "hit.wav", 90f, 0.14f, 0.55f, WaveShape.Noise);
        WriteTone(SfxFolder + "door_unlock.wav", 660f, 0.22f, 0.4f, WaveShape.Chime);
        AssetDatabase.Refresh();
        Debug.Log("Diamond Rush gameplay SFX placeholders written to " + SfxFolder);
    }

    enum WaveShape
    {
        Sine,
        Thump,
        Noise,
        Chime
    }

    static void WriteTone(string path, float baseHz, float durationSec, float volume, WaveShape shape)
    {
        const int sampleRate = 44100;
        var sampleCount = Mathf.Max(1, Mathf.RoundToInt(sampleRate * durationSec));
        var samples = new float[sampleCount];
        var rng = new System.Random(path.GetHashCode());

        for (var i = 0; i < sampleCount; i++)
        {
            var t = i / (float)sampleRate;
            var envelope = 1f - i / (float)sampleCount;
            envelope *= envelope;
            float sample;

            switch (shape)
            {
                case WaveShape.Sine:
                    sample = Mathf.Sin(2f * Mathf.PI * baseHz * t);
                    break;
                case WaveShape.Thump:
                    sample = Mathf.Sin(2f * Mathf.PI * baseHz * t) * Mathf.Exp(-t * 28f);
                    break;
                case WaveShape.Noise:
                    sample = (float)(rng.NextDouble() * 2.0 - 1.0) * Mathf.Exp(-t * 18f);
                    break;
                case WaveShape.Chime:
                    var f = baseHz + t * 420f;
                    sample = Mathf.Sin(2f * Mathf.PI * f * t) * 0.7f
                        + Mathf.Sin(2f * Mathf.PI * f * 1.5f * t) * 0.3f;
                    break;
                default:
                    sample = 0f;
                    break;
            }

            samples[i] = Mathf.Clamp(sample * volume * envelope, -1f, 1f);
        }

        WriteWav(path, samples, sampleRate);
    }

    static void WriteWav(string path, float[] samples, int sampleRate)
    {
        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write);
        using var writer = new BinaryWriter(stream);
        var byteRate = sampleRate * 2;
        var dataSize = samples.Length * 2;
        writer.Write(new[] { (byte)'R', (byte)'I', (byte)'F', (byte)'F' });
        writer.Write(36 + dataSize);
        writer.Write(new[] { (byte)'W', (byte)'A', (byte)'V', (byte)'E' });
        writer.Write(new[] { (byte)'f', (byte)'m', (byte)'t', (byte)' ' });
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)1);
        writer.Write(sampleRate);
        writer.Write(byteRate);
        writer.Write((short)2);
        writer.Write((short)16);
        writer.Write(new[] { (byte)'d', (byte)'a', (byte)'t', (byte)'a' });
        writer.Write(dataSize);

        foreach (var sample in samples)
        {
            var value = (short)Mathf.Clamp(Mathf.RoundToInt(sample * 32767f), short.MinValue, short.MaxValue);
            writer.Write(value);
        }
    }
}
