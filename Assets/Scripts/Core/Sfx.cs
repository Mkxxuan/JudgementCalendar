using UnityEngine;

namespace JudgementCalendar
{
    /// <summary>程序化 8-bit 音效：纯代码生成方波，无任何素材文件。</summary>
    public static class Sfx
    {
        const int Rate = 44100;
        static bool inited;

        public static AudioClip Click, Confirm, Deny, Reveal, Drift, Day, EndGood, EndBad;

        public static void Init()
        {
            if (inited) return;
            inited = true;
            Click = Seq(new[] { 880f }, new[] { 0.045f }, 0.45f);
            Confirm = Seq(new[] { 660f, 880f }, new[] { 0.06f, 0.09f }, 0.5f);
            Deny = Seq(new[] { 196f }, new[] { 0.16f }, 0.5f);
            Reveal = Seq(new[] { 1046f, 1318f }, new[] { 0.05f, 0.09f }, 0.5f);
            Drift = Seq(new[] { 130f, 98f }, new[] { 0.12f, 0.14f }, 0.5f);
            Day = Seq(new[] { 523f, 659f }, new[] { 0.06f, 0.12f }, 0.4f);
            EndGood = Seq(new[] { 523f, 659f, 784f, 1046f }, new[] { 0.10f, 0.10f, 0.10f, 0.22f }, 0.5f);
            EndBad = Seq(new[] { 392f, 311f, 247f, 165f }, new[] { 0.12f, 0.12f, 0.12f, 0.26f }, 0.5f);
        }

        static AudioClip Seq(float[] freqs, float[] durs, float vol)
        {
            int total = 0;
            for (int i = 0; i < durs.Length; i++) total += (int)(durs[i] * Rate);
            var data = new float[total];
            int idx = 0;
            for (int s = 0; s < freqs.Length && s < durs.Length; s++)
            {
                int n = (int)(durs[s] * Rate);
                float f = freqs[s];
                for (int i = 0; i < n && idx < total; i++, idx++)
                {
                    float t = (float)i / Rate;
                    float release = Mathf.Clamp01((n - i) / (Rate * 0.03f));
                    float v = Mathf.Sin(2f * Mathf.PI * f * t);
                    v = v >= 0f ? 1f : -1f; // 方波
                    data[idx] = v * release * vol;
                }
            }
            var clip = AudioClip.Create("sfx_" + freqs.Length, total, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
