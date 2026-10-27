using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PlumajeNeon
{
    public struct Pose
    {
        public float anim, k;
        public string move;
        public bool neon, sil, hit, ko, block, air, walk, win, flash;
    }

    /// <summary>
    /// Ave armada con sprites generados por código (cuerpo, ala, cola, patas, cabeza, pico).
    /// Tres modos: natural, androide neón y silueta (para los "Coming soon").
    /// </summary>
    public class BirdRig
    {
        const int STD = 0, NAT = 1, NEON = 2, LINE = 3;

        class Part
        {
            public SpriteRenderer fill, outl;
            public Color nat, neon;
            public int kind;
            public bool main;
        }

        class Leg
        {
            public SpriteRenderer thigh, shin;
            public SpriteRenderer[] claws = new SpriteRenderer[4];
        }

        static readonly Dictionary<string, Gfx.PolySprite> cache = new Dictionary<string, Gfx.PolySprite>();
        static readonly Color DARK = Data.C("#0b0d18"), MAIN = Data.C("#151927"), SUB = Data.C("#1e2336");
        static readonly Color EDGE = new Color(10f / 255f, 10f / 255f, 22f / 255f, 0.45f);
        static readonly Color BLACK = Data.C("#111111");

        public Transform root;
        public SortingGroup group;
        Transform pose, tailPivot, body, wingPivot, head, thruster;
        SpriteRenderer glow;
        Leg back, front;
        readonly List<Part> parts = new List<Part>();
        readonly Look L;
        readonly string key;
        int order;

        public BirdRig(Look look, string speciesKey, Transform parent)
        {
            L = look;
            key = speciesKey;
            root = new GameObject("Ave_" + key).transform;
            root.SetParent(parent, false);
            group = root.gameObject.AddComponent<SortingGroup>();

            glow = Gfx.MakeSR("halo", root, Gfx.Soft, order++, Color.clear);
            glow.transform.localPosition = Gfx.L(0, -62);
            glow.transform.localScale = new Vector3(1.6f, 2.0f, 1f);

            pose = Node("pose", root, 0, 0);

            // cola
            tailPivot = Node("cola", pose, -14, -42);
            if (L.tailType == TailType.Racket)
            {
                AddLine("eje", tailPivot, L.tail, L.nA, 0, 0, 0, 52, 3.2f, LINE);
                AddPoly("raqueta", tailPivot, Gfx.Ellipse(0, 58, 6.5f, 10), L.crown, L.nA, STD, false);
                AddPoly("punta", tailPivot, Gfx.Ellipse(0, 66, 4.5f, 3.6f), BLACK, L.nB, STD, false);
            }
            else
            {
                List<Vector2> t;
                if (L.tailType == TailType.Long) t = Gfx.Pts(0, -6, -34, 60, -25, 63, 8, 4);
                else if (L.tailType == TailType.Fan) t = Gfx.Pts(0, -8, -40, 30, -36, 40, -28, 45, -16, 42, 8, 4);
                else t = Gfx.Pts(0, -6, -24, 22, -12, 27, 6, 4);
                AddPoly("cola", tailPivot, t, L.tail, L.nA, STD, false);
            }

            back = MakeLeg(pose);

            // propulsor (solo neón)
            thruster = Node("propulsor", pose, -22, -46);
            AddPoly("llama", thruster, Gfx.Pts(0, -6, -14, 2, 2, 8), L.nB, L.nB, NEON, false);

            // cuerpo
            body = Node("cuerpo", pose, 0, -60);
            AddPoly("cuerpo", body, Gfx.Ellipse(0, 0, 22, 31), L.body, L.nA, STD, true);
            AddPoly("vientre", body, Gfx.Ellipse(8, 6, 13, 22), L.belly, L.nB, STD, false);
            Color circ = Gfx.A(L.nB, 0.8f);
            AddLine("circuito1", body, Color.clear, circ, -14, -14, -5, -5, 1.4f, NEON);
            AddLine("circuito2", body, Color.clear, circ, -5, -5, -5, 12, 1.4f, NEON);
            AddLine("circuito3", body, Color.clear, circ, 4, -22, 10, -14, 1.4f, NEON);
            AddLine("circuito4", body, Color.clear, circ, 10, -14, 10, -6, 1.4f, NEON);
            AddPoly("nucleo", body, Gfx.Ellipse(9, 2, 3.2f, 3.2f), L.nB, L.nB, NEON, false);
            wingPivot = Node("ala", body, -4, -6);
            AddPoly("ala", wingPivot, Gfx.Ellipse(0, 13, 9, 24), L.wing, L.nA, STD, false);
            AddLine("pluma1", wingPivot, new Color(0, 0, 0, 0.25f), Gfx.A(L.nA, 0.6f), -3, 14, -2, 32, 1f, LINE);
            AddLine("pluma2", wingPivot, new Color(0, 0, 0, 0.25f), Gfx.A(L.nA, 0.6f), 3, 14, 2, 32, 1f, LINE);

            front = MakeLeg(pose);

            // cabeza
            head = Node("cabeza", pose, 10, -96);
            AddPoly("cabeza", head, Gfx.Ellipse(0, 0, 15, 15), L.head, L.nA, STD, true);
            if (L.hasCrown)
            {
                AddPoly("corona", head, Gfx.Arc(0, 0, 14, Mathf.PI * 1.08f, Mathf.PI * 1.92f, 5), L.crown, L.crown, NAT, false);
                AddPoly("corona2", head, Gfx.Arc(0, 1, 8, Mathf.PI * 1.25f, Mathf.PI * 1.75f, 4), BLACK, BLACK, NAT, false);
            }
            if (L.hasMask)
            {
                AddPoly("antifaz", head, Gfx.Ellipse(5, -1, 13, 4.5f, 0.06f), L.mask, L.mask, NAT, false);
                AddLine("antifaz2", head, L.crown, L.crown, -6, 4, 14, 4.5f, 1.2f, NAT);
            }
            if (L.hasCheek) AddPoly("mejilla", head, Gfx.Ellipse(-3, 2, 5, 5), L.cheek, L.cheek, NAT, false);
            if (L.hasThroat) AddPoly("garganta", head, Gfx.Ellipse(8, 11, 7, 5), L.throat, L.throat, NAT, false);

            float bl = L.beakLen;
            List<Vector2> beak;
            if (L.hook)
            {
                beak = new List<Vector2> { new Vector2(11, -5) };
                Vector2 a = new Vector2(11, -5), c = new Vector2(14 + bl, -9), b = new Vector2(12 + bl, 6);
                for (int i = 1; i <= 10; i++) { float t = i / 10f, u = 1 - t; beak.Add(u * u * a + 2 * u * t * c + t * t * b); }
                beak.Add(new Vector2(11, 5));
            }
            else beak = Gfx.Pts(11, -5, 12 + bl, L.thin ? 0 : 1, 11, L.thin ? 2 : 6);
            AddPoly("pico", head, beak, L.beak, L.nB, STD, false);

            AddPoly("ojo", head, Gfx.Ellipse(6, -3, 3.2f, 3.2f), L.eye, L.eye, NAT, false);
            AddPoly("brillo", head, Gfx.Ellipse(7, -4, 1, 1, 0, 10), Color.white, Color.white, NAT, false);
            AddPoly("visor", head, Gfx.Pts(-2, -6, 15, -6, 15, -1.5f, -2, -1.5f), L.nA, L.nA, NEON, false);
            AddLine("antena", head, L.nB, L.nB, -6, -13, -12, -25, 2f, NEON);
            AddPoly("antena2", head, Gfx.Ellipse(-12, -26, 2.6f, 2.6f), L.nB, L.nB, NEON, false);
        }

        // ---------- construcción ----------
        Transform Node(string name, Transform parent, float x, float y)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = Gfx.L(x, y);
            return t;
        }

        void AddPoly(string name, Transform parent, List<Vector2> pts, Color nat, Color neon, int kind, bool main)
        {
            string ck = key + "/" + name;
            Gfx.PolySprite ps;
            if (!cache.TryGetValue(ck, out ps)) { ps = Gfx.Poly(pts, 2f); cache[ck] = ps; }
            var p = new Part { nat = nat, neon = neon, kind = kind, main = main };
            if (kind == STD)
            {
                p.outl = Gfx.MakeSR(name + "_borde", parent, ps.outline, order++, Color.white);
                p.outl.transform.localPosition = Gfx.L(ps.center.x, ps.center.y);
            }
            p.fill = Gfx.MakeSR(name, parent, ps.fill, order++, nat);
            p.fill.transform.localPosition = Gfx.L(ps.center.x, ps.center.y);
            parts.Add(p);
        }

        void AddLine(string name, Transform parent, Color nat, Color neon, float ax, float ay, float bx, float by, float w, int kind)
        {
            var p = new Part { nat = nat, neon = neon, kind = kind };
            p.fill = Gfx.MakeSR(name, parent, Gfx.Square, order++, nat);
            Seg(p.fill.transform, ax, ay, bx, by, w);
            parts.Add(p);
        }

        Leg MakeLeg(Transform parent)
        {
            var lg = new Leg();
            lg.thigh = Gfx.MakeSR("muslo", parent, Gfx.Square, order++, Color.white);
            lg.shin = Gfx.MakeSR("tarso", parent, Gfx.Square, order++, Color.white);
            for (int i = 0; i < 4; i++) lg.claws[i] = Gfx.MakeSR("garra", parent, Gfx.Square, order++, Color.white);
            return lg;
        }

        public static void Seg(Transform t, float ax, float ay, float bx, float by, float w)
        {
            float dx = bx - ax, dy = by - ay, len = Mathf.Sqrt(dx * dx + dy * dy);
            t.localPosition = Gfx.L((ax + bx) / 2f, (ay + by) / 2f);
            t.localRotation = Quaternion.Euler(0, 0, -Mathf.Atan2(dy, dx) * Mathf.Rad2Deg);
            t.localScale = new Vector3((len + w * 0.8f) / 100f, w / 100f, 1f);
        }

        static readonly float[] clawAng = { -0.5f, 0f, 0.5f };

        void SetLeg(Leg lg, float hx, float hy, float fx, float fy, Color c)
        {
            float kx = (hx + fx) / 2f + 6f, ky = (hy + fy) / 2f;
            Seg(lg.thigh.transform, hx, hy, kx, ky, 4f);
            Seg(lg.shin.transform, kx, ky, fx, fy, 4f);
            for (int i = 0; i < 3; i++)
                Seg(lg.claws[i].transform, fx, fy, fx + Mathf.Cos(clawAng[i]) * 9f, fy + Mathf.Sin(clawAng[i]) * 4f - 1f, 2.4f);
            Seg(lg.claws[3].transform, fx, fy, fx - 6f, fy, 2.4f);
            lg.thigh.color = c; lg.shin.color = c;
            for (int i = 0; i < 4; i++) lg.claws[i].color = c;
        }

        // ---------- por frame ----------
        public void SetActive(bool on) { if (root.gameObject.activeSelf != on) root.gameObject.SetActive(on); }

        public void Place(float x, float y, int face, float scale)
        {
            root.position = Gfx.W(x, y);
            root.localScale = new Vector3(face * L.size * scale, L.size * scale, 1f);
        }

        public void Apply(Pose P)
        {
            int mode = P.sil ? 2 : (P.neon ? 1 : 0);
            float k = P.k, lean = 0, hx = 0, hy = 0, wing = 0, kick = 0;
            bool dive = false;
            string mv = P.move;
            float bob = P.air ? 0f : Mathf.Sin(P.anim * 0.1f) * 1.6f;

            if (mv == "pico") { hx = 22 * k; hy = 8 * k; lean = 0.18f * k; }
            else if (mv == "garra") { kick = k; lean = -0.12f * k; }
            else if (mv == "picada") { dive = true; lean = 0.35f; }
            else if (mv == "ascenso") { wing = 1; lean = -0.2f; kick = 0.5f; }
            else if (mv == "disco") { lean = 0.1f * k; wing = 0.4f * k; }
            else if (mv == "prisma") { hx = 6 * k; wing = 0.9f * k; }
            else if (mv == "bandada") { wing = k; lean = -0.15f * k; }
            if (P.air && mv == null) wing = 0.5f + Mathf.Sin(P.anim * 0.5f) * 0.5f;
            if (P.win) wing = Mathf.Abs(Mathf.Sin(P.anim * 0.2f));
            if (P.hit) { lean = -0.3f; hx = -6; }

            pose.localPosition = P.ko ? Gfx.L(-10, -6) : Vector3.zero;
            pose.localRotation = P.ko ? Quaternion.Euler(0, 0, 1.35f * Mathf.Rad2Deg) : Quaternion.identity;

            tailPivot.localPosition = Gfx.L(-14, -42 + bob);
            if (L.tailType == TailType.Racket)
            {
                float ang = 0.55f + Mathf.Sin(P.anim * 0.06f) * 0.28f + (mv == "disco" ? k : 0f) * 1.6f;
                tailPivot.localRotation = Quaternion.Euler(0, 0, -ang * Mathf.Rad2Deg);
            }

            Color legC = mode == 0 ? L.leg : mode == 1 ? L.nB : Gfx.A(L.nA, 0.55f);
            if (P.flash) legC = Color.Lerp(legC, Color.white, 0.6f);
            float walk = P.walk ? Mathf.Sin(P.anim * 0.35f) : 0f;
            if (dive) SetLeg(back, -4, -36, 22, 2, legC); else SetLeg(back, -4, -36 + bob, -8 + walk * 6, 0, legC);
            if (dive) SetLeg(front, 4, -36, 32, 6, legC); else SetLeg(front, 4, -36 + bob, 8 - walk * 6 + kick * 58, -kick * 30, legC);

            thruster.localScale = new Vector3(Random.Range(0.7f, 1.4f), 1f, 1f);

            body.localPosition = Gfx.L(0, -60 + bob);
            body.localRotation = Quaternion.Euler(0, 0, -lean * Mathf.Rad2Deg);
            float wr = P.block ? -1.15f : 0.25f + wing * 2.2f;
            wingPivot.localRotation = Quaternion.Euler(0, 0, -wr * Mathf.Rad2Deg);
            head.localPosition = Gfx.L(10 + hx + lean * 34, -96 + hy + bob);

            glow.enabled = mode == 1;
            glow.color = Gfx.A(L.nA, 0.28f);

            foreach (var p in parts) Paint(p, mode, P.flash);
        }

        void Paint(Part p, int mode, bool flash)
        {
            bool vis;
            Color f, o = Color.clear;
            switch (p.kind)
            {
                case STD:
                    vis = true;
                    if (mode == 0) { f = p.nat; o = EDGE; }
                    else if (mode == 1) { f = p.main ? MAIN : SUB; o = p.neon; }
                    else { f = DARK; o = Gfx.A(L.nA, 0.6f); }
                    break;
                case NAT: vis = mode == 0; f = p.nat; break;
                case NEON: vis = mode == 1; f = p.neon; break;
                default: vis = true; f = mode == 0 ? p.nat : mode == 1 ? p.neon : DARK; break;
            }
            if (flash) { f = Color.Lerp(f, Color.white, 0.6f); o = Color.Lerp(o, Color.white, 0.6f); }
            p.fill.enabled = vis;
            p.fill.color = f;
            if (p.outl != null) { p.outl.enabled = vis; p.outl.color = o; }
        }
    }
}
