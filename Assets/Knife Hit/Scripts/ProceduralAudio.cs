using UnityEngine;

public static class ProceduralAudio
{
    const int SAMPLE_RATE = 44100;

    static AudioClip Make(string name, float[] data)
    {
        var clip = AudioClip.Create(name, data.Length, 1, SAMPLE_RATE, false);
        clip.SetData(data, 0);
        return clip;
    }

    public static AudioClip GenerateHit()
    {
        int n = (int)(SAMPLE_RATE * 0.09f);
        float[] s = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / SAMPLE_RATE;
            float prog = (float)i / n;
            float f = Mathf.Lerp(120f, 30f, prog);
            float env = 1f - prog;
            s[i] = Mathf.Sin(2 * Mathf.PI * f * t) * 0.6f * env;
        }
        return Make("hit", s);
    }

    public static AudioClip GenerateClank()
    {
        int n = (int)(SAMPLE_RATE * 0.22f);
        float[] s = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / SAMPLE_RATE;
            float prog = (float)i / n;
            float f = Mathf.Lerp(600f, 150f, prog);
            float saw = 2f * (t * f - Mathf.Floor(t * f + 0.5f));
            float env = 1f - prog;
            s[i] = saw * 0.5f * env;
        }
        return Make("clank", s);
    }

    public static AudioClip GenerateStageClear()
    {
        int totalN = (int)(SAMPLE_RATE * 0.5f);
        float[] s = new float[totalN];
        int[] notes = { 400, 500, 600, 800 };
        int noteLen = (int)(SAMPLE_RATE * 0.1f);

        for (int k = 0; k < notes.Length; k++)
        {
            int start = (int)(SAMPLE_RATE * 0.06f * k);
            for (int i = 0; i < noteLen; i++)
            {
                int idx = start + i;
                if (idx >= totalN) break;
                float t = (float)i / SAMPLE_RATE;
                float prog = (float)i / noteLen;
                float env = 1f - prog;
                s[idx] += Mathf.Sin(2 * Mathf.PI * notes[k] * t) * 0.3f * env;
            }
        }
        return Make("clear", s);
    }
}