using UnityEngine;

public static class Sfx
{
    const int SR = 44100;

    static AudioClip Build(string name, float seconds, System.Func<float, int, float> sample)
    {
        int n = Mathf.CeilToInt(SR * seconds);
        var data = new float[n];
        for (int i = 0; i < n; i++) data[i] = sample((float)i / SR, i);
        var clip = AudioClip.Create(name, n, 1, SR, false);
        clip.SetData(data, 0);
        return clip;
    }

    public static AudioClip Slice() => Build("slice", 0.13f, (t, i) =>
    {
        float f = Mathf.Lerp(560f + Random.value * 160f, 190f, t / 0.12f);
        float env = Mathf.Pow(1f - t / 0.12f, 2f);
        return Mathf.Sin(2f * Mathf.PI * f * t) * env * 0.18f;
    });

    public static AudioClip Heal() => Build("heal", 0.45f, (t, i) =>
    {
        float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };
        int idx = Mathf.Min(3, (int)(t / 0.06f));
        float local = t - idx * 0.06f;
        float env = Mathf.Pow(1f - local / 0.25f, 2f);
        return env > 0 ? Mathf.Sin(2f * Mathf.PI * notes[idx] * t) * env * 0.16f : 0f;
    });

    public static AudioClip Bomb() => Build("bomb", 0.35f, (t, i) =>
    {
        float env = Mathf.Pow(1f - t / 0.35f, 1.8f);
        return (Random.value * 2f - 1f) * env * 0.28f;
    });

    public static AudioClip Launch(bool bomb) => Build("launch", 0.19f, (t, i) =>
    {
        float f = Mathf.Lerp(bomb ? 130f : 210f, bomb ? 80f : 380f, t / 0.18f);
        float env = Mathf.Pow(1f - t / 0.18f, 2f);
        return Mathf.Sin(2f * Mathf.PI * f * t) * env * 0.05f;
    });
}