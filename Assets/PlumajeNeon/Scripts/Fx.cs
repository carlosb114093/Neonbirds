using System.Collections.Generic;
using UnityEngine;

namespace PlumajeNeon
{
    public class Particle
    {
        public int type, life, max;
        public float x, y, vx, vy, g, drag = 1f, size = 3f, rot, vr, r, vr2;
        public Color col = Color.white;
        public SpriteRenderer sr;
    }

    /// <summary>Partículas con un pool fijo de SpriteRenderers: plumas, chispas, anillos y tierra.</summary>
    public static class Fx
    {
        public const int DOT = 0, FEATHER = 1, SPARK = 2, RING = 3;
        const int MAX = 450;
        static readonly List<Particle> live = new List<Particle>();
        static readonly Stack<SpriteRenderer> pool = new Stack<SpriteRenderer>();

        public static void Init(Transform parent)
        {
            var holder = new GameObject("Particulas").transform;
            holder.SetParent(parent, false);
            for (int i = 0; i < MAX; i++)
            {
                var sr = Gfx.MakeSR("p", holder, Gfx.Square, 30, Color.white);
                sr.gameObject.SetActive(false);
                pool.Push(sr);
            }
        }

        public static void Add(Particle p)
        {
            if (pool.Count == 0) return;
            p.max = Mathf.Max(1, p.life);
            p.sr = pool.Pop();
            p.sr.sprite = p.type == RING ? Gfx.Ring : p.type == FEATHER ? Gfx.Circle : Gfx.Square;
            p.sr.gameObject.SetActive(true);
            live.Add(p);
        }

        public static void Clear()
        {
            foreach (var p in live) Release(p);
            live.Clear();
        }

        static void Release(Particle p)
        {
            p.sr.gameObject.SetActive(false);
            pool.Push(p.sr);
        }

        public static void Step()
        {
            for (int i = live.Count - 1; i >= 0; i--)
            {
                var p = live[i];
                p.x += p.vx; p.y += p.vy; p.vy += p.g; p.vx *= p.drag;
                p.rot += p.vr; p.r += p.vr2; p.life--;
                if (p.life <= 0) { Release(p); live.RemoveAt(i); }
            }
        }

        public static void Render()
        {
            foreach (var p in live)
            {
                var c = p.col;
                c.a *= Mathf.Clamp01(p.life / (float)p.max);
                p.sr.color = c;
                var t = p.sr.transform;
                switch (p.type)
                {
                    case FEATHER:
                        t.position = Gfx.W(p.x, p.y);
                        t.rotation = Quaternion.Euler(0, 0, -p.rot * Mathf.Rad2Deg);
                        t.localScale = new Vector3(p.size * 3.2f / 100f, p.size * 1.2f / 100f, 1f);
                        break;
                    case SPARK:
                        float len = Mathf.Max(2f, Mathf.Sqrt(p.vx * p.vx + p.vy * p.vy) * 2.2f);
                        t.position = Gfx.W(p.x - p.vx * 1.1f, p.y - p.vy * 1.1f);
                        t.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(-p.vy, p.vx) * Mathf.Rad2Deg);
                        t.localScale = new Vector3(len / 100f, p.size / 100f, 1f);
                        break;
                    case RING:
                        t.position = Gfx.W(p.x, p.y);
                        t.rotation = Quaternion.identity;
                        float d = Mathf.Max(0f, p.r) * 2f / 100f;
                        t.localScale = new Vector3(d, d, 1f);
                        break;
                    default:
                        t.position = Gfx.W(p.x, p.y);
                        t.rotation = Quaternion.identity;
                        t.localScale = new Vector3(p.size / 100f, p.size / 100f, 1f);
                        break;
                }
            }
        }
    }
}
