using System.Collections.Generic;
using UnityEngine;

namespace PlumajeNeon
{
    /// <summary>Efectos de sonido sintetizados al arrancar (barridos de onda cuadrada, sierra y triángulo). No usa archivos de audio.</summary>
    public static class Sfx
    {
        const int SR = 44100;
        const int SQUARE = 0, SAW = 1, TRI = 2;
        static AudioSource src;
        static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();

        public static void Init(GameObject host)
        {
            src = host.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.volume = 0.8f;
            Make("hit", T(320, 90, 0.09f, SQUARE, 0.11f));
            Make("heavy", T(190, 45, 0.2f, SAW, 0.14f), T(90, 40, 0.15f, SQUARE, 0.08f));
            Make("block", T(900, 620, 0.06f, TRI, 0.08f));
            Make("transform", T(140, 1400, 0.75f, SAW, 0.09f), T(280, 2600, 0.75f, SQUARE, 0.04f));
            Make("beam", T(1300, 260, 0.55f, SAW, 0.07f), T(650, 130, 0.55f, TRI, 0.06f));
            Make("disco", T(480, 1150, 0.22f, SQUARE, 0.07f));
            Make("ko", T(420, 40, 0.9f, SAW, 0.14f));
            Make("select", T(660, 990, 0.07f, SQUARE, 0.05f));
            Make("deny", T(210, 140, 0.16f, SQUARE, 0.07f));
            Make("dig", T(95, 55, 0.35f, TRI, 0.14f));
            Make("ready", T(880, 1760, 0.12f, SQUARE, 0.05f), Delay(T(1320, 2640, 0.12f, SQUARE, 0.05f), 0.11f));
            Make("flock", T(1500, 2400, 0.18f, TRI, 0.06f));
        }

        public static void Play(string name)
        {
            AudioClip c;
            if (src != null && clips.TryGetValue(name, out c)) src.PlayOneShot(c);
        }

        static float[] T(float f1, float f2, float dur, int type, float vol)
        {
            int n = (int)(dur * SR) + 1;
            var d = new float[n];
            double ph = 0;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                float f = f1 * Mathf.Pow(f2 / f1, t / dur);
                ph += f / SR;
                float x = (float)(ph - System.Math.Floor(ph));
                float w = type == SQUARE ? (x < 0.5f ? 1f : -1f) : type == SAW ? 2f * x - 1f : 1f - 4f * Mathf.Abs(x - 0.5f);
                float g = vol * Mathf.Pow(0.001f / vol, t / dur);
                d[i] = w * g * 2.2f;
            }
            return d;
        }

        static float[] Delay(float[] d, float sec)
        {
            int off = (int)(sec * SR);
            var o = new float[d.Length + off];
            System.Array.Copy(d, 0, o, off, d.Length);
            return o;
        }

        static void Make(string name, params float[][] layers)
        {
            int len = 0;
            foreach (var l in layers) len = Mathf.Max(len, l.Length);
            var mix = new float[len];
            foreach (var l in layers) for (int i = 0; i < l.Length; i++) mix[i] += l[i];
            for (int i = 0; i < len; i++) mix[i] = Mathf.Clamp(mix[i], -1f, 1f);
            var clip = AudioClip.Create(name, len, 1, SR, false);
            clip.SetData(mix, 0);
            clips[name] = clip;
        }
    }
}
