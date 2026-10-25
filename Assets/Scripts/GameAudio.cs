using System;
using UnityEngine;

// Short synthesized sound effects, so the game needs no audio files.
public class GameAudio
{
    const int SampleRate = 44100;

    readonly AudioSource source;
    readonly AudioClip click, buy, golden, error;

    public bool Muted;

    public GameAudio(GameObject host)
    {
        source = host.AddComponent<AudioSource>();
        source.playOnAwake = false;
        Muted = PlayerPrefs.GetInt("caramelo_muted", 0) == 1;

        click = Tone("click", 0.09f, t => Mathf.Lerp(620f, 260f, t / 0.09f), 0.5f);
        buy = Tone("buy", 0.24f, t => t < 0.11f ? 660f : 990f, 0.35f);
        golden = Tone("golden", 0.56f, t => t < 0.14f ? 523f : t < 0.28f ? 659f : t < 0.42f ? 784f : 1047f, 0.35f);
        error = Tone("error", 0.16f, t => 150f, 0.35f);
    }

    public void SetMuted(bool muted)
    {
        Muted = muted;
        PlayerPrefs.SetInt("caramelo_muted", muted ? 1 : 0);
    }

    public void PlayClick()
    {
        Play(click, UnityEngine.Random.Range(0.9f, 1.2f));
    }

    public void PlayBuy() { Play(buy, 1f); }
    public void PlayGolden() { Play(golden, 1f); }
    public void PlayError() { Play(error, 1f); }

    void Play(AudioClip clip, float pitch)
    {
        if (Muted) return;
        source.pitch = pitch;
        source.PlayOneShot(clip);
    }

    static AudioClip Tone(string name, float duration, Func<float, float> frequencyAt, float volume)
    {
        int count = (int)(SampleRate * duration);
        var samples = new float[count];
        float phase = 0f;
        for (int i = 0; i < count; i++)
        {
            float t = (float)i / SampleRate;
            phase += 2f * Mathf.PI * frequencyAt(t) / SampleRate;
            float attack = Mathf.Min(1f, t * 400f);
            float decay = 1f - t / duration;
            samples[i] = Mathf.Sin(phase) * attack * decay * decay * volume;
        }

        var clip = AudioClip.Create(name, count, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
