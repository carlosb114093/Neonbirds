using System.Collections.Generic;
using UnityEngine;

namespace PlumajeNeon
{
    /// <summary>
    /// Gráficos procedurales: texturas básicas, polígonos rasterizados con borde y el escenario de Cali.
    /// Coordenadas "px": el lienzo lógico de 960x540 con y hacia abajo.
    /// 100 px = 1 unidad de Unity.
    /// </summary>
    public static class Gfx
    {
        public const float PPU = 100f;
        const float S = 3f;   // texeles por px lógico al rasterizar polígonos

        public static Texture2D White;
        public static Sprite Square, Circle, Soft, Ring, VGrad;
        static bool inited;

        public struct PolySprite { public Sprite fill, outline; public Vector2 center; }

        public static void Init()
        {
            if (inited) return;
            inited = true;
            White = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var w = new Color32[16];
            for (int i = 0; i < 16; i++) w[i] = new Color32(255, 255, 255, 255);
            White.SetPixels32(w);
            White.Apply();
            Square = Sprite.Create(White, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            Circle = Radial(128, 0);
            Soft = Radial(128, 1);
            Ring = Radial(128, 2);

            var g = new Texture2D(4, 64, TextureFormat.RGBA32, false);
            g.wrapMode = TextureWrapMode.Clamp;
            var gp = new Color32[4 * 64];
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 4; x++)
                    gp[y * 4 + x] = new Color32(255, 255, 255, (byte)(255 * (1f - y / 63f)));
            g.SetPixels32(gp);
            g.Apply();
            VGrad = Sprite.Create(g, new Rect(0, 0, 4, 64), new Vector2(0.5f, 0.5f), 64f);
        }

        // ---------- helpers de coordenadas y color ----------
        public static Vector3 W(float x, float y) { return new Vector3((x - 480f) / PPU, (270f - y) / PPU, 0f); }
        public static Vector3 L(float x, float y) { return new Vector3(x / PPU, -y / PPU, 0f); }
        public static Color A(Color c, float a) { c.a = a; return c; }

        public static SpriteRenderer MakeSR(string name, Transform parent, Sprite s, int order, Color c)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            sr.sortingOrder = order;
            sr.color = c;
            return sr;
        }

        public static Sprite SpriteFrom(Texture2D t)
        {
            return Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), PPU);
        }

        static Sprite Radial(int n, int kind)
        {
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
            t.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[n * n];
            float h = n / 2f;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f - h) / h, dy = (y + 0.5f - h) / h;
                    float d = Mathf.Sqrt(dx * dx + dy * dy), a;
                    if (kind == 0) a = Mathf.Clamp01((1f - d) * h);                       // círculo con borde suave
                    else if (kind == 1) a = Mathf.Pow(Mathf.Clamp01(1f - d), 2f);         // halo
                    else a = Mathf.Clamp01(1f - Mathf.Abs(d - 0.9f) / 0.08f);             // anillo
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(255 * a));
                }
            t.SetPixels32(px);
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
        }

        // ---------- polígonos ----------
        public static List<Vector2> Ellipse(float cx, float cy, float rx, float ry, float rot = 0f, int n = 28)
        {
            var pts = new List<Vector2>(n);
            float cr = Mathf.Cos(rot), sr = Mathf.Sin(rot);
            for (int i = 0; i < n; i++)
            {
                float a = Mathf.PI * 2f * i / n;
                float x = Mathf.Cos(a) * rx, y = Mathf.Sin(a) * ry;
                pts.Add(new Vector2(cx + x * cr - y * sr, cy + x * sr + y * cr));
            }
            return pts;
        }

        /// <summary>Banda en arco (como un trazo de canvas): radio central r, grosor w, ángulos en sentido horario con y hacia abajo.</summary>
        public static List<Vector2> Arc(float cx, float cy, float r, float a0, float a1, float w, int n = 20)
        {
            var pts = new List<Vector2>();
            for (int i = 0; i <= n; i++) { float a = Mathf.Lerp(a0, a1, i / (float)n); pts.Add(new Vector2(cx + Mathf.Cos(a) * (r + w / 2), cy + Mathf.Sin(a) * (r + w / 2))); }
            for (int i = n; i >= 0; i--) { float a = Mathf.Lerp(a0, a1, i / (float)n); pts.Add(new Vector2(cx + Mathf.Cos(a) * (r - w / 2), cy + Mathf.Sin(a) * (r - w / 2))); }
            return pts;
        }

        public static List<Vector2> Pts(params float[] xy)
        {
            var pts = new List<Vector2>();
            for (int i = 0; i + 1 < xy.Length; i += 2) pts.Add(new Vector2(xy[i], xy[i + 1]));
            return pts;
        }

        /// <summary>Rasteriza un polígono (px, y hacia abajo) a dos sprites: relleno y relleno+borde. Ambos centrados en 'center'.</summary>
        public static PolySprite Poly(List<Vector2> pts, float outline)
        {
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            foreach (var p in pts) { minX = Mathf.Min(minX, p.x); minY = Mathf.Min(minY, p.y); maxX = Mathf.Max(maxX, p.x); maxY = Mathf.Max(maxY, p.y); }
            float pad = outline + 2f;
            float ox = minX - pad, oy = minY - pad;
            int w = Mathf.CeilToInt((maxX - minX + 2 * pad) * S), h = Mathf.CeilToInt((maxY - minY + 2 * pad) * S);
            var fill = new Color32[w * h];
            var outl = new Color32[w * h];
            int n = pts.Count;
            for (int j = 0; j < h; j++)
            {
                float py = oy + (h - j - 0.5f) / S;
                for (int i = 0; i < w; i++)
                {
                    float px = ox + (i + 0.5f) / S;
                    bool inside = false;
                    float dmin = float.MaxValue;
                    for (int a = 0, b = n - 1; a < n; b = a++)
                    {
                        Vector2 pa = pts[a], pb = pts[b];
                        if (((pa.y > py) != (pb.y > py)) && (px < (pb.x - pa.x) * (py - pa.y) / (pb.y - pa.y) + pa.x)) inside = !inside;
                        float d = SegDist(px, py, pa, pb);
                        if (d < dmin) dmin = d;
                    }
                    float sd = inside ? -dmin : dmin;
                    fill[j * w + i] = new Color32(255, 255, 255, (byte)(255 * Mathf.Clamp01(0.5f - sd * S)));
                    outl[j * w + i] = new Color32(255, 255, 255, (byte)(255 * Mathf.Clamp01(0.5f - (sd - outline) * S)));
                }
            }
            var ps = new PolySprite();
            ps.fill = MakeSprite(fill, w, h);
            ps.outline = MakeSprite(outl, w, h);
            ps.center = new Vector2(ox + w / (2f * S), oy + h / (2f * S));
            return ps;
        }

        static Sprite MakeSprite(Color32[] px, int w, int h)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.wrapMode = TextureWrapMode.Clamp;
            t.filterMode = FilterMode.Bilinear;
            t.SetPixels32(px);
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), PPU * S);
        }

        static float SegDist(float px, float py, Vector2 a, Vector2 b)
        {
            float vx = b.x - a.x, vy = b.y - a.y, wx = px - a.x, wy = py - a.y;
            float l2 = vx * vx + vy * vy;
            float t = l2 > 0 ? Mathf.Clamp01((wx * vx + wy * vy) / l2) : 0f;
            float dx = wx - vx * t, dy = wy - vy * t;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        // ---------- escenario: atardecer en Cali ----------
        const int BW = 960, BH = 540;
        static Color[] buf;
        static uint seed;

        static float Rnd() { seed = (uint)((seed * 16807UL) % 2147483647UL); return seed / 2147483647f; }

        static void Blend(int x, int y, Color c)
        {
            if (x < 0 || y < 0 || x >= BW || y >= BH) return;
            int i = (BH - 1 - y) * BW + x;
            Color d = buf[i];
            float a = c.a;
            buf[i] = new Color(d.r + (c.r - d.r) * a, d.g + (c.g - d.g) * a, d.b + (c.b - d.b) * a, Mathf.Max(d.a, a));
        }

        static void FillRect(float x, float y, float w, float h, Color c)
        {
            for (int yy = Mathf.RoundToInt(y); yy < Mathf.RoundToInt(y + h); yy++)
                for (int xx = Mathf.RoundToInt(x); xx < Mathf.RoundToInt(x + w); xx++) Blend(xx, yy, c);
        }

        static void Disk(float cx, float cy, float r, Color c)
        {
            for (int y = Mathf.FloorToInt(cy - r); y <= Mathf.CeilToInt(cy + r); y++)
                for (int x = Mathf.FloorToInt(cx - r); x <= Mathf.CeilToInt(cx + r); x++)
                {
                    float d = Mathf.Sqrt((x + 0.5f - cx) * (x + 0.5f - cx) + (y + 0.5f - cy) * (y + 0.5f - cy));
                    float a = Mathf.Clamp01(r - d + 0.5f);
                    if (a > 0) Blend(x, y, A(c, c.a * a));
                }
        }

        static Color Grad(float t, float[] at, Color[] cols)
        {
            if (t <= at[0]) return cols[0];
            for (int i = 1; i < at.Length; i++)
                if (t <= at[i]) return Color.Lerp(cols[i - 1], cols[i], (t - at[i - 1]) / (at[i] - at[i - 1]));
            return cols[cols.Length - 1];
        }

        static Vector2 Quad(Vector2 a, Vector2 c, Vector2 b, float t)
        {
            float u = 1 - t;
            return u * u * a + 2 * u * t * c + t * t * b;
        }

        static void Ridge(Color col, float[] xy)
        {
            for (int x = 0; x < BW; x++)
            {
                float top = 440;
                for (int i = 0; i + 3 < xy.Length; i += 2)
                    if (x >= xy[i] && x <= xy[i + 2]) { top = Mathf.Lerp(xy[i + 1], xy[i + 3], (x - xy[i]) / (xy[i + 2] - xy[i])); break; }
                for (int y = Mathf.FloorToInt(top); y < 440; y++) Blend(x, y, y == Mathf.FloorToInt(top) ? A(col, 1f - (top - y)) : col);
            }
        }

        public static Texture2D BuildBackground()
        {
            buf = new Color[BW * BH];
            seed = 11;
            var skyAt = new[] { 0f, 0.45f, 0.78f, 1f };
            var skyC = new[] { Data.C("#120a2e"), Data.C("#4a1d5c"), Data.C("#c4475a"), Data.C("#f39a5b") };
            for (int y = 0; y < 440; y++)
            {
                Color c = Grad(y / 440f, skyAt, skyC);
                for (int x = 0; x < BW; x++) buf[(BH - 1 - y) * BW + x] = c;
            }
            for (int i = 0; i < 70; i++) { float a = Rnd() * 0.6f; float sx = Rnd() * BW, sy = Rnd() * 190; FillRect(sx, sy, 2, 2, new Color(1f, 0.94f, 1f, a)); }

            // sol
            var sunAt = new[] { 0f, 0.3f, 1f };
            var sunC = new[] { new Color(1f, 0.89f, 0.63f, 0.95f), new Color(1f, 0.67f, 0.35f, 0.35f), new Color(1f, 0.47f, 0.31f, 0f) };
            for (int y = 180; y < 520; y++)
                for (int x = 520; x < 860; x++)
                {
                    float d = Mathf.Sqrt((x - 690) * (x - 690) + (y - 350) * (y - 350));
                    if (d < 170) Blend(x, y, Grad(d / 170f, sunAt, sunC));
                }
            Disk(690, 350, 42, Data.C("#ffd99a"));

            // Farallones de Cali
            Ridge(Data.C("#6a3470"), new float[] { 0, 320, 70, 282, 140, 300, 220, 240, 300, 276, 370, 226, 440, 258, 520, 210, 600, 252, 690, 238, 780, 272, 870, 246, 960, 282 });
            Ridge(Data.C("#43204f"), new float[] { 0, 360, 90, 330, 170, 352, 260, 318, 350, 350, 450, 326, 560, 356, 660, 334, 760, 360, 860, 338, 960, 356 });

            // Cerro de las Tres Cruces
            var hill = Data.C("#2a1640");
            Vector2 h0 = new Vector2(0, 420), hc = new Vector2(120, 300), h1 = new Vector2(260, 400);
            var top = new float[261];
            for (int i = 0; i <= 400; i++) { var p = Quad(h0, hc, h1, i / 400f); int xi = Mathf.Clamp(Mathf.RoundToInt(p.x), 0, 260); top[xi] = p.y; }
            for (int x = 0; x <= 260; x++) for (int y = Mathf.FloorToInt(top[x]); y < 440; y++) Blend(x, y, hill);
            var crossC = Data.C("#120a1e");
            float[][] crosses = { new float[] { 104, 358, 20 }, new float[] { 125, 353, 26 }, new float[] { 146, 358, 20 } };
            foreach (var c in crosses)
            {
                FillRect(c[0] - 1.5f, c[1] - c[2], 3, c[2], crossC);
                FillRect(c[0] - 7, c[1] - c[2] + 5.5f, 14, 3, crossC);
            }

            // edificios de Cali
            var bld = Data.C("#1d1030");
            float bx = 280;
            while (bx < 940)
            {
                float w = 18 + Rnd() * 34;
                float hgt = (bx > 500 && bx < 540) ? 150 : 28 + Rnd() * 95;
                FillRect(bx, 440 - hgt, w, hgt, bld);
                for (float wy = 440 - hgt + 6; wy < 436; wy += 9)
                    for (float wx = bx + 4; wx < bx + w - 4; wx += 7)
                        if (Rnd() < 0.28f) { float g = (190 + Rnd() * 50) / 255f; FillRect(wx, wy, 3, 4, new Color(1f, g, 0.43f, 0.45f + Rnd() * 0.4f)); }
                bx += w + 2 + Rnd() * 6;
            }

            // palmas
            Palm(36, 150, 14); Palm(880, 170, -18); Palm(930, 130, 10);

            // plaza
            var flAt = new[] { 0f, 1f };
            var flC = new[] { Data.C("#2a1340"), Data.C("#0a0614") };
            for (int y = 440; y < BH; y++) { Color c = Grad((y - 440) / 100f, flAt, flC); for (int x = 0; x < BW; x++) buf[(BH - 1 - y) * BW + x] = c; }
            FillRect(0, 439, BW, 2, new Color(1f, 0.59f, 0.39f, 0.35f));
            var lineC = new Color(1f, 0.7f, 0.86f, 0.08f);
            foreach (int ly in new[] { 452, 470, 494, 526 }) FillRect(0, ly, BW, 1, lineC);
            for (int i = -10; i <= 10; i++) PerspLine(i, lineC, 1);

            return ToTexture(buf);
        }

        static void PerspLine(int i, Color c, int width)
        {
            for (int y = 440; y < BH; y++)
            {
                float t = (y - 440) / 100f;
                float x = Mathf.Lerp(480 + i * 30, 480 + i * 150, t);
                for (int k = 0; k < width; k++) Blend(Mathf.RoundToInt(x) + k, y, c);
            }
        }

        static void Palm(float px, float ph, float lean)
        {
            var c = Data.C("#0f0819");
            Vector2 a = new Vector2(px, 440), ctrl = new Vector2(px + lean * 0.5f, 440 - ph * 0.6f), b = new Vector2(px + lean, 440 - ph);
            for (int i = 0; i <= 80; i++) { var p = Quad(a, ctrl, b, i / 80f); Disk(p.x, p.y, 2.6f, c); }
            for (int i = 0; i < 7; i++)
            {
                float ang = -Mathf.PI + i * (Mathf.PI / 6f) + 0.1f;
                Vector2 c1 = new Vector2(b.x + Mathf.Cos(ang) * 30, b.y + Mathf.Sin(ang) * 22 - 10);
                Vector2 e = new Vector2(b.x + Mathf.Cos(ang) * 48, b.y + Mathf.Sin(ang) * 14 + 14);
                for (int s = 0; s <= 30; s++) { float t = s / 30f; var p = Quad(b, c1, e, t); Disk(p.x, p.y, Mathf.Lerp(4.5f, 1f, t), c); }
            }
        }

        public static Texture2D BuildNeonFloor()
        {
            buf = new Color[BW * BH];
            var mag = Data.C("#ff2bd6"); var cy = Data.C("#00f0ff");
            foreach (int ly in new[] { 452, 470, 494, 526 }) for (int x = 0; x < BW; x++) { buf[(BH - 1 - ly) * BW + x] = mag; }
            for (int i = -10; i <= 10; i++)
                for (int y = 440; y < BH; y++)
                {
                    float x = Mathf.Lerp(480 + i * 30, 480 + i * 150, (y - 440) / 100f);
                    int xi = Mathf.RoundToInt(x);
                    if (xi >= 0 && xi < BW) buf[(BH - 1 - y) * BW + xi] = cy;
                }
            return ToTexture(buf);
        }

        static Texture2D ToTexture(Color[] px)
        {
            var t = new Texture2D(BW, BH, TextureFormat.RGBA32, false);
            t.wrapMode = TextureWrapMode.Clamp;
            t.filterMode = FilterMode.Bilinear;
            t.SetPixels(px);
            t.Apply();
            return t;
        }
    }
}
