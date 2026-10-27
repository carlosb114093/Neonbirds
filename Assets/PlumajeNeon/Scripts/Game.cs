using System.Collections.Generic;
using UnityEngine;

namespace PlumajeNeon
{
    public class Banner { public string text, sub; public int t; public Color col, col2; public bool neon; }

    public class Proj
    {
        public string type;
        public int own, life, delay;
        public float x, y, vx, vy, r, spin;
        public bool aimed;
        public Move m;
        public Color col, col2;
        public GameObject go;
        public SpriteRenderer main, glow;
        public SpriteRenderer[] spokes;
    }

    /// <summary>
    /// Plumaje Neón: juego de pelea con aves del Valle del Cauca.
    /// Se instancia automáticamente al cargar la escena (no hace falta armarla). Toda la lógica corre a 60 pasos fijos por segundo.
    /// </summary>
    public class Game : MonoBehaviour
    {
        public static Game I;
        public const float W = 960f, H = 540f, GROUND = 468f;
        public const int NEON_DUR = 600, ROUND_TIME = 60, TF_DUR = 48;
        const float STEP = 1f / 60f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Object.FindAnyObjectByType<Game>() != null) return;
            new GameObject("PlumajeNeon").AddComponent<Game>();
        }

        // ---------- estado ----------
        string scene = "title";
        int menu, mode = 1, frame;
        readonly int[] selCur = { 0, 1 };
        readonly bool[] selDone = new bool[2];
        readonly int[] selShake = new int[2];
        int selT;
        public Fighter[] F = new Fighter[2];
        readonly List<Proj> proj = new List<Proj>();
        public int freeze, slow, cine, flash;
        public float shake;
        public Banner banner;
        string phase = "intro";
        int phaseT, round = 1, timer, matchWinner = -1, toastT;
        readonly int[] wins = new int[2];
        string toast;
        bool paused, hasClick;
        Vector2 click;
        float acc;

        // ---------- vista ----------
        Camera cam;
        Transform world;
        SpriteRenderer bg, overlay, neonFloor, cineDark;
        readonly SpriteRenderer[] cards = new SpriteRenderer[8], cardGlow = new SpriteRenderer[8];
        readonly BirdRig[] portraits = new BirdRig[8], titleRigs = new BirdRig[2];
        readonly BirdRig[,] fRigs = new BirdRig[2, 2];
        readonly SpriteRenderer[] shadows = new SpriteRenderer[2], mounds = new SpriteRenderer[2], moundRings = new SpriteRenderer[2];
        readonly SpriteRenderer[] columns = new SpriteRenderer[2], orbs = new SpriteRenderer[2];
        readonly SpriteRenderer[,] beams = new SpriteRenderer[2, 7];
        Rect vp = new Rect(0, 0, 1, 1);

        static readonly Color CYAN = Data.C("#00f0ff"), MAG = Data.C("#ff2bd6"), LIME = Data.C("#d4ff2a"), INK = Data.C("#0b0d18");
        static readonly Color MUTED = Data.C("#b9b0e6"), SOFT = Data.C("#d8d0ff"), DIM = Data.C("#8f88b8"), LINE = Data.C("#2a2350"), LINE2 = Data.C("#4a4280");
        static readonly Color[] BEAM = { Data.C("#ff3b3b"), Data.C("#ff9f1c"), Data.C("#ffe600"), Data.C("#39ff88"), Data.C("#2bd9ff"), Data.C("#a35bff") };

        // ---------- controles ----------
        // orden: izquierda, derecha, arriba, abajo, pico, garra, especial, transformar
        static readonly K[][][] CTRL =
        {
            new[] { new[] { K.A }, new[] { K.D }, new[] { K.W }, new[] { K.S }, new[] { K.F }, new[] { K.G }, new[] { K.H }, new[] { K.R } },
            new[] { new[] { K.Left }, new[] { K.Right }, new[] { K.Up }, new[] { K.Down }, new[] { K.KeyK, K.Num1 }, new[] { K.KeyL, K.Num2 }, new[] { K.Semicolon, K.Num3 }, new[] { K.KeyP, K.Num0 } }
        };
        static readonly string[][] KEYLABEL = { new[] { "R", "H", "S" }, new[] { "P", "Ñ", "↓" } };

        static bool AnyOf(K[] ks, bool edge)
        {
            foreach (var k in ks) if (edge ? Keys.Pressed(k) : Keys.Held(k)) return true;
            return false;
        }

        static Inp Read(int p, bool edge)
        {
            var c = CTRL[p];
            return new Inp { l = AnyOf(c[0], edge), r = AnyOf(c[1], edge), u = AnyOf(c[2], edge), d = AnyOf(c[3], edge), p = AnyOf(c[4], edge), k = AnyOf(c[5], edge), s = AnyOf(c[6], edge), x = AnyOf(c[7], edge) };
        }

        static Ctl ReadInput(int p) { return new Ctl { h = Read(p, false), e = Read(p, true) }; }

        // ================================================================
        void Awake()
        {
            I = this;
            Gfx.Init();
            Sfx.Init(gameObject);

            cam = Camera.main;
            if (cam == null) { cam = new GameObject("Main Camera").AddComponent<Camera>(); cam.tag = "MainCamera"; }
            cam.orthographic = true;
            cam.orthographicSize = 2.7f;
            cam.transform.position = new Vector3(0, 0, -10);
            cam.transform.rotation = Quaternion.identity;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;

            world = new GameObject("Mundo").transform;
            world.SetParent(transform, false);

            bg = Gfx.MakeSR("Escenario", world, Gfx.SpriteFrom(Gfx.BuildBackground()), 0, Color.white);
            neonFloor = Gfx.MakeSR("PisoNeon", world, Gfx.SpriteFrom(Gfx.BuildNeonFloor()), 1, Color.clear);
            overlay = Gfx.MakeSR("Velo", world, Gfx.Square, 2, Color.clear);
            overlay.transform.localScale = new Vector3(9.8f, 5.6f, 1);
            cineDark = Gfx.MakeSR("VeloTransformacion", world, Gfx.Square, 2, Color.clear);
            cineDark.transform.localScale = new Vector3(9.8f, 5.6f, 1);

            for (int i = 0; i < 8; i++)
            {
                var d = Data.Roster[i];
                cards[i] = Gfx.MakeSR("Tarjeta_" + d.id, world, Gfx.Square, 3, Data.C("#120e26"));
                cardGlow[i] = Gfx.MakeSR("TarjetaHalo_" + d.id, world, Gfx.Soft, 4, Color.clear);
                portraits[i] = new BirdRig(d.L, d.id, world);
                portraits[i].group.sortingOrder = 5;
            }
            for (int i = 0; i < 2; i++)
            {
                titleRigs[i] = new BirdRig(Data.Roster[i].L, Data.Roster[i].id, world);
                titleRigs[i].group.sortingOrder = 5;
                for (int s = 0; s < 2; s++) fRigs[i, s] = new BirdRig(Data.Roster[i].L, Data.Roster[i].id, world);
                shadows[i] = Gfx.MakeSR("Sombra", world, Gfx.Circle, 6, new Color(0, 0, 0, 0.4f));
                mounds[i] = Gfx.MakeSR("Monticulo", world, Gfx.Circle, 9, Data.C("#4a2e1a"));
                moundRings[i] = Gfx.MakeSR("MonticuloBorde", world, Gfx.Ring, 9, Color.white);
                columns[i] = Gfx.MakeSR("ColumnaLuz", world, Gfx.VGrad, 9, Color.clear);
                orbs[i] = Gfx.MakeSR("Carga", world, Gfx.Circle, 20, Color.white);
                for (int b = 0; b < 7; b++) beams[i, b] = Gfx.MakeSR("Rayo", world, Gfx.Square, 20, Color.white);
            }
            Fx.Init(world);
        }

        void Update()
        {
            Keys.Poll();
            acc += Mathf.Min(0.1f, Time.unscaledDeltaTime);
            while (acc >= STEP)
            {
                bool keep = Step();
                if (!keep) { Keys.ClearEdges(); hasClick = false; }
                acc -= STEP;
            }
            Render();
        }

        // ================================================================
        // PASOS
        // ================================================================
        bool Step()
        {
            frame++;
            if (scene == "title") return StepTitle();
            if (scene == "select") { if (toast != null && --toastT <= 0) toast = null; return StepSelect(); }
            return StepFight();
        }

        static readonly Rect[] MENU = { new Rect(320, 318, 320, 38), new Rect(320, 364, 320, 38) };
        static readonly string[] MENU_LABEL = { "1 JUGADOR  ·  VS CPU", "2 JUGADORES" };
        bool ClickIn(Rect r) { return hasClick && r.Contains(click); }

        bool StepTitle()
        {
            if (Keys.Any(K.W, K.Up) && menu != 0) { menu = 0; Sfx.Play("select"); }
            if (Keys.Any(K.S, K.Down) && menu != 1) { menu = 1; Sfx.Play("select"); }
            bool go = Keys.Any(K.Enter, K.Space, K.F, K.KeyK);
            for (int i = 0; i < 2; i++) if (ClickIn(MENU[i])) { menu = i; go = true; }
            if (go) { Sfx.Play("select"); mode = menu == 0 ? 1 : 2; ToSelect(); }
            Fx.Step();
            return false;
        }

        const float CW = 170, CH = 150, CX0 = 116, CY0 = 92, CG = 16;
        static Rect CardRect(int i) { return new Rect(CX0 + (i % 4) * (CW + CG), CY0 + (i / 4) * (CH + CG), CW, CH); }
        static int MoveCur(int i, int dx, int dy) { int c = i % 4, r = i / 4; c = (c + dx + 4) % 4; r = (r + dy + 2) % 2; return r * 4 + c; }

        void ToSelect()
        {
            scene = "select";
            selCur[0] = 0; selCur[1] = 1; selDone[0] = selDone[1] = false; selT = 0; selShake[0] = selShake[1] = 0;
            ClearProjectiles(); Fx.Clear();
        }

        void TryConfirm(int p)
        {
            var d = Data.Roster[selCur[p]];
            if (!d.playable) { Sfx.Play("deny"); toast = "Muy pronto: " + d.name; toastT = 110; selShake[p] = 14; return; }
            selDone[p] = true;
            Sfx.Play("select");
            if (mode == 1 && p == 0) { selCur[1] = selCur[0] == 0 ? 1 : 0; selDone[1] = true; }
        }

        bool StepSelect()
        {
            for (int i = 0; i < 2; i++) selShake[i] = Mathf.Max(0, selShake[i] - 1);
            if (selT > 0) { if (--selT == 0) StartMatch(); return false; }
            if (Keys.Any(K.Escape, K.Backspace))
            {
                if (mode == 2 && selDone[1]) selDone[1] = false;
                else if (selDone[0]) { selDone[0] = false; if (mode == 1) selDone[1] = false; }
                else scene = "title";
                return false;
            }
            int players = mode == 2 ? 2 : 1;
            for (int p = 0; p < players; p++)
            {
                if (selDone[p]) continue;
                var c = CTRL[p];
                int n = selCur[p];
                if (AnyOf(c[0], true)) n = MoveCur(n, -1, 0);
                if (AnyOf(c[1], true)) n = MoveCur(n, 1, 0);
                if (AnyOf(c[2], true)) n = MoveCur(n, 0, -1);
                if (AnyOf(c[3], true)) n = MoveCur(n, 0, 1);
                if (n != selCur[p]) { selCur[p] = n; Sfx.Play("select"); }
            }
            if (!selDone[0] && Keys.Any(K.F, K.Space)) TryConfirm(0);
            else if (mode == 2 && !selDone[1] && Keys.Any(K.KeyK, K.Num1)) TryConfirm(1);
            else if (Keys.Any(K.Enter)) { if (!selDone[0]) TryConfirm(0); else if (mode == 2 && !selDone[1]) TryConfirm(1); }
            if (hasClick)
            {
                for (int i = 0; i < 8; i++)
                    if (ClickIn(CardRect(i)))
                    {
                        int p = !selDone[0] ? 0 : (mode == 2 && !selDone[1] ? 1 : -1);
                        if (p >= 0) { selCur[p] = i; TryConfirm(p); }
                    }
            }
            if (selDone[0] && selDone[1] && selT == 0) selT = 60;
            return false;
        }

        void StartMatch()
        {
            wins[0] = wins[1] = 0; round = 1; matchWinner = -1; paused = false;
            StartRound();
            scene = "fight";
        }

        void StartRound()
        {
            F[0] = new Fighter(Data.Roster[selCur[0]], 0, false);
            F[1] = new Fighter(Data.Roster[selCur[1]], 1, mode == 1);
            ClearProjectiles(); Fx.Clear();
            timer = ROUND_TIME * 60; phase = "intro"; phaseT = 110; banner = null; cine = 0; slow = 0; freeze = 0;
        }

        void EndRound(int w, bool time)
        {
            if (phase != "fight") return;
            phase = "end"; phaseT = 175;
            if (w >= 0) wins[w]++;
            banner = new Banner
            {
                text = time ? "¡TIEMPO!" : "K.O.",
                sub = w >= 0 ? F[w].def.name.ToUpper() + " GANA LA RONDA" : "EMPATE",
                t = 160,
                col = w >= 0 ? F[w].L.nA : Color.white,
                col2 = w >= 0 ? F[w].L.nB : Color.white
            };
        }

        static readonly Rect BTN_REMATCH = new Rect(296, 338, 176, 44), BTN_SELECT = new Rect(488, 338, 176, 44), BTN_MENU = new Rect(400, 300, 160, 40);

        bool StepFight()
        {
            if (shake > 0) shake *= 0.86f;
            if (flash > 0) flash--;
            if (phase == "match")
            {
                if (Keys.Any(K.Enter, K.Space, K.F, K.KeyK) || ClickIn(BTN_REMATCH)) { StartMatch(); return false; }
                if (Keys.Any(K.Escape, K.Backspace) || ClickIn(BTN_SELECT)) { ToSelect(); return false; }
                F[0].Update(Ctl.None, F[1]); F[1].Update(Ctl.None, F[0]);
                Fx.Step();
                return false;
            }
            if (Keys.Any(K.Escape)) { paused = !paused; return false; }
            if (paused)
            {
                if (Keys.Any(K.M) || ClickIn(BTN_MENU)) { paused = false; scene = "title"; ClearProjectiles(); Fx.Clear(); }
                else if (hasClick || Keys.Any(K.Enter)) paused = false;
                return false;
            }
            if (freeze > 0) { freeze--; return true; }
            if (slow > 0) { slow--; if (slow % 2 == 1) return true; }

            Fighter a = F[0], b = F[1];
            if (cine > 0)
            {
                cine--;
                foreach (var f in F) if (f.tf > 0) f.Update(Ctl.None, F[1 - f.side]);
                Fx.Step();
                if (banner != null && --banner.t <= 0) banner = null;
                return true;
            }
            bool live = phase == "fight";
            Ctl ia = live ? ReadInput(0) : Ctl.None;
            Ctl ib = live ? (b.cpu ? AiInput(b, a) : ReadInput(1)) : Ctl.None;
            a.Update(ia, b);
            b.Update(ib, a);
            if (a.burT < 0 && b.burT < 0 && !a.ko && !b.ko)
            {
                float dx = b.x - a.x, min = 40;
                if (Mathf.Abs(dx) < min && Mathf.Abs(a.y - b.y) < 80)
                {
                    float push = (min - Mathf.Abs(dx)) / 2f * (dx >= 0 ? 1 : -1);
                    a.x = Mathf.Clamp(a.x - push, 36, W - 36);
                    b.x = Mathf.Clamp(b.x + push, 36, W - 36);
                }
            }
            StepProjectiles();
            Fx.Step();
            if (banner != null && --banner.t <= 0) banner = null;

            if (phase == "intro") { if (--phaseT <= 0) phase = "fight"; }
            else if (phase == "fight")
            {
                if (--timer <= 0) { timer = 0; EndRound(a.hp > b.hp ? 0 : b.hp > a.hp ? 1 : -1, true); }
            }
            else if (phase == "end")
            {
                if (--phaseT <= 0)
                {
                    if (wins[0] >= 2 || wins[1] >= 2) { phase = "match"; matchWinner = wins[0] >= 2 ? 0 : 1; }
                    else { round++; StartRound(); }
                }
            }
            return false;
        }

        // ================================================================
        // COMBATE
        // ================================================================
        void AddTank(Fighter f, float v)
        {
            if (f.neon || f.tf > 0 || f.ko) return;
            bool was = f.tank < 100;
            f.tank = Mathf.Min(100, f.tank + v);
            if (was && f.tank >= 100) { Sfx.Play("ready"); Ring(f.x, f.y - 60, f.L.nA, 8, 3, 30); }
        }

        public bool HitFighter(Fighter att, Fighter def, Move m, float srcX)
        {
            if (phase != "fight" || def.inv > 0 || def.tf > 0 || def.ko) return false;
            float s = def.L.size, from = float.IsNaN(srcX) ? att.x : srcX;
            int dirTo = from > def.x ? 1 : from < def.x ? -1 : -def.face;
            float pw = att.def.power * (att.neon ? 1.3f : 1f);
            float cx = def.x + dirTo * 14, cy = def.y - 70 * s;
            if (def.block && def.Ground && def.face == dirTo)
            {
                def.hp -= m.dmg * pw * 0.15f; def.bstun = 10; def.vx = -dirTo * m.kb * 0.6f;
                AddTank(def, 3); AddTank(att, 2);
                Sfx.Play("block"); freeze = 3;
                for (int i = 0; i < 8; i++) Spark(cx, cy, -dirTo * Random.Range(1f, 5f), Random.Range(-3f, 3f), 14, Data.C("#9fdcff"), 2);
            }
            else
            {
                def.hp -= m.dmg * pw; def.stun = m.stun; def.vx = -dirTo * m.kb;
                if (m.launch > 0) def.vy = -m.launch; else if (!def.Ground) def.vy = -4;
                def.atk = null; def.block = false; def.hurtFlash = 8;
                AddTank(att, m.gain); AddTank(def, m.gain > 0 ? m.gain * 0.7f : 5f);
                bool big = m.dmg >= 10;
                freeze = big ? 7 : 5; shake = big ? 10 : 5;
                Sfx.Play(m.sfx);
                Burst(def, cx, cy, big ? 16 : 10);
                Ring(cx, cy, att.neon ? att.L.nA : Color.white, 4, big ? 4 : 3, 12);
            }
            if (def.hp <= 0)
            {
                def.hp = 0; def.ko = true; def.stun = 0; def.atk = null; def.vy = -9; def.vx = -dirTo * 6;
                Sfx.Play("ko"); freeze = 18; slow = 70;
                EndRound(att.side, false);
            }
            return true;
        }

        // ---------- partículas ----------
        public void Spark(float x, float y, float vx, float vy, int life, Color c, float size)
        {
            Fx.Add(new Particle { type = Fx.SPARK, x = x, y = y, vx = vx, vy = vy, life = life, col = c, size = size });
        }

        public void Ring(float x, float y, Color c, float r0, float vr, int life)
        {
            Fx.Add(new Particle { type = Fx.RING, x = x, y = y, r = r0, vr2 = vr, life = life, col = c });
        }

        public void Dirt(float x, float y, int n)
        {
            var cols = new[] { Data.C("#6b4426"), Data.C("#8a5a32"), Data.C("#3d2816") };
            for (int i = 0; i < n; i++)
                Fx.Add(new Particle { type = Fx.DOT, x = x + Random.Range(-18f, 18f), y = y, vx = Random.Range(-3f, 3f), vy = Random.Range(-6f, -2f), g = 0.35f, life = Random.Range(20, 34), col = cols[i % 3], size = Random.Range(2f, 5f) });
        }

        public void Burst(Fighter f, float x, float y, int n)
        {
            var L = f.L;
            Color[] cols = f.neon ? new[] { L.nA, L.nB, Color.white } : new[] { L.body, L.belly, L.head, L.wing };
            for (int i = 0; i < n; i++)
                Fx.Add(new Particle
                {
                    type = f.neon ? Fx.SPARK : Fx.FEATHER, x = x, y = y, vx = Random.Range(-4f, 4f), vy = Random.Range(-5f, -1f),
                    g = f.neon ? 0.05f : 0.12f, drag = 0.95f, life = Random.Range(28, 52), col = cols[i % cols.Length],
                    rot = Random.Range(0f, 6f), vr = Random.Range(-0.3f, 0.3f), size = Random.Range(3f, 6f)
                });
        }

        // ---------- proyectiles ----------
        public void SpawnDisco(Fighter f)
        {
            float s = f.L.size;
            Sfx.Play("disco");
            var p = new Proj { type = "disco", own = f.side, x = f.x + f.face * 44 * s, y = f.y - 74 * s, vx = f.face * 8.5f, r = 15, life = 120, col = f.L.nA, col2 = f.L.nB };
            p.m = new Move { name = "disco", dmg = 10, kb = 7, stun = 20, sfx = "heavy" };
            p.go = new GameObject("DiscoPendulo");
            p.go.transform.SetParent(world, false);
            p.glow = Gfx.MakeSR("halo", p.go.transform, Gfx.Soft, 24, Gfx.A(p.col, 0.45f));
            p.glow.transform.localScale = new Vector3(0.7f, 0.7f, 1);
            p.main = Gfx.MakeSR("disco", p.go.transform, Gfx.Ring, 25, p.col);
            p.spokes = new SpriteRenderer[3];
            for (int i = 0; i < 3; i++) p.spokes[i] = Gfx.MakeSR("radio", p.go.transform, Gfx.Square, 26, p.col2);
            proj.Add(p);
        }

        static Gfx.PolySprite aveSprite;
        static bool aveReady;

        public void SpawnFlock(Fighter f)
        {
            Sfx.Play("flock");
            if (!aveReady) { aveSprite = Gfx.Poly(Gfx.Pts(12, 0, -8, -5, -3, 0, -8, 5), 0f); aveReady = true; }
            for (int i = 0; i < 3; i++)
            {
                var p = new Proj { type = "ave", own = f.side, delay = i * 9, x = Mathf.Clamp(f.x - f.face * (30 + i * 28), 20, W - 20), y = 120 + i * 26, r = 12, life = 130, col = f.L.nA, col2 = f.L.nB };
                p.m = new Move { name = "ave", dmg = 5, kb = 3, stun = 12 };
                p.go = new GameObject("AveNeon");
                p.go.transform.SetParent(world, false);
                p.glow = Gfx.MakeSR("halo", p.go.transform, Gfx.Soft, 24, Gfx.A(p.col, 0.4f));
                p.glow.transform.localScale = new Vector3(0.5f, 0.5f, 1);
                var holder = new GameObject("forma").transform;
                holder.SetParent(p.go.transform, false);
                p.main = Gfx.MakeSR("ave", holder, aveSprite.fill, 25, p.col);
                p.main.transform.localPosition = Gfx.L(aveSprite.center.x, aveSprite.center.y);
                p.go.SetActive(false);
                proj.Add(p);
            }
        }

        void StepProjectiles()
        {
            for (int i = proj.Count - 1; i >= 0; i--)
            {
                var p = proj[i];
                if (p.delay > 0) { p.delay--; continue; }
                var o = F[1 - p.own];
                var me = F[p.own];
                if (p.type == "ave" && !p.aimed)
                {
                    float dx = o.x - p.x, dy = (o.y - 60) - p.y, d = Mathf.Max(1f, Mathf.Sqrt(dx * dx + dy * dy));
                    p.vx = dx / d * 9; p.vy = dy / d * 9; p.aimed = true;
                }
                p.x += p.vx; p.y += p.vy; p.life--; p.spin += 0.35f;
                if (frame % 2 == 0) Spark(p.x, p.y, -p.vx * 0.1f, -p.vy * 0.1f, 12, p.col2, 2);
                var hb = o.Hurt();
                if (p.x + p.r > hb.x1 && p.x - p.r < hb.x2 && p.y + p.r > hb.y1 && p.y - p.r < hb.y2)
                    if (HitFighter(me, o, p.m, p.x)) p.life = 0;
                if (p.x < -40 || p.x > W + 40) p.life = 0;
                if (p.y > GROUND - 4)
                {
                    p.life = 0;
                    for (int k = 0; k < 6; k++) Spark(p.x, GROUND - 4, Random.Range(-3f, 3f), Random.Range(-3f, -1f), 14, p.col, 2);
                }
                if (p.life <= 0) { Destroy(p.go); proj.RemoveAt(i); }
            }
        }

        void ClearProjectiles()
        {
            foreach (var p in proj) if (p.go != null) Destroy(p.go);
            proj.Clear();
        }

        // ---------- CPU ----------
        Ctl AiInput(Fighter me, Fighter o)
        {
            var A = me.ai;
            var c = new Ctl();
            float dist = Mathf.Abs(o.x - me.x);
            int dir = o.x > me.x ? 1 : -1;
            if (A.t > 0) A.t--;
            else
            {
                A.hold = null; A.press = null;
                A.t = Random.Range(8, 24);
                float r = Random.value;
                bool threat = false;
                foreach (var p in proj)
                    if (p.own != me.side && Mathf.Abs(p.x - me.x) < 230 && Mathf.Sign(me.x - p.x) == Mathf.Sign(p.vx == 0 ? 1 : p.vx)) threat = true;
                if (me.tank >= 100 && !me.neon) A.press = "x";
                else if (threat && r < 0.55f) { A.press = "u"; A.hold = dir > 0 ? "r" : "l"; }
                else if (me.neon && me.cool == 0 && r < 0.5f)
                {
                    A.press = "s";
                    if (me.def.id == "tangara") A.hold = (dist < 200 && r < 0.25f) ? "d" : null;
                    else A.hold = (dist > 150 && r < 0.3f) ? "d" : null;
                }
                else if (o.atk != null && dist < 130 && r < 0.45f) { A.hold = "d"; A.t = 18; }
                else if (dist > 100) { A.hold = dir > 0 ? "r" : "l"; if (r < 0.08f) A.press = "u"; }
                else if (r < 0.36f) A.press = "p";
                else if (r < 0.66f) A.press = "k";
                else if (r < 0.8f) { A.hold = dir > 0 ? "l" : "r"; A.t = 12; }
                else { A.press = "u"; A.hold = dir > 0 ? "r" : "l"; }
            }
            if (A.hold != null) Set(c.h, A.hold);
            if (A.press != null) { Set(c.e, A.press); Set(c.h, A.press); A.press = null; }
            return c;
        }

        static void Set(Inp i, string k)
        {
            switch (k)
            {
                case "l": i.l = true; break;
                case "r": i.r = true; break;
                case "u": i.u = true; break;
                case "d": i.d = true; break;
                case "p": i.p = true; break;
                case "k": i.k = true; break;
                case "s": i.s = true; break;
                case "x": i.x = true; break;
            }
        }

        // ================================================================
        // RENDER DEL MUNDO
        // ================================================================
        void Render()
        {
            float target = 16f / 9f, sa = (float)Screen.width / Mathf.Max(1, Screen.height);
            vp = sa > target ? new Rect((1 - target / sa) / 2f, 0, target / sa, 1) : new Rect(0, (1 - sa / target) / 2f, 1, sa / target);
            cam.rect = vp;
            float sh = shake > 0.5f ? shake : 0f;
            cam.transform.position = new Vector3(Random.Range(-0.5f, 0.5f) * sh / 100f, Random.Range(-0.5f, 0.5f) * sh / 100f, -10f);

            bool T = scene == "title", S = scene == "select", Fi = scene == "fight";
            overlay.color = new Color(8 / 255f, 5 / 255f, 20 / 255f, T ? 0.5f : S ? 0.8f : 0f);

            bool anyNeon = Fi && F[0] != null && (F[0].neon || F[0].tf > 0 || F[1].neon || F[1].tf > 0);
            neonFloor.color = new Color(1, 1, 1, anyNeon ? 0.18f + Mathf.Sin(frame * 0.08f) * 0.06f : 0f);
            cineDark.color = new Color(4 / 255f, 2 / 255f, 12 / 255f, Fi && cine > 0 ? 0.55f * Mathf.Min(1f, Mathf.Min(cine / 10f, (TF_DUR - cine + 1) / 8f)) : 0f);

            // portada
            bool cyc = (frame / 150) % 2 == 1;
            for (int i = 0; i < 2; i++)
            {
                titleRigs[i].SetActive(T);
                if (!T) continue;
                titleRigs[i].Place(i == 0 ? 180 : 780, GROUND, i == 0 ? 1 : -1, 1.75f);
                titleRigs[i].Apply(new Pose { anim = frame + i * 40, neon = i == 0 ? cyc : !cyc });
            }

            // selección
            for (int i = 0; i < 8; i++)
            {
                var d = Data.Roster[i];
                cards[i].enabled = S;
                portraits[i].SetActive(S);
                cardGlow[i].enabled = false;
                if (!S) continue;
                var R = CardRect(i);
                float sx = 0;
                for (int p = 0; p < 2; p++) if (selCur[p] == i && selShake[p] > 0) sx = Mathf.Sin(selShake[p] * 1.8f) * 4;
                cards[i].transform.position = Gfx.W(R.x + sx + CW / 2, R.y + CH / 2);
                cards[i].transform.localScale = new Vector3(CW / 100f, CH / 100f, 1);
                bool cur = selCur[0] == i || selCur[1] == i;
                bool neon = d.playable && cur && ((frame + i * 30) / 90) % 2 == 1;
                if (neon)
                {
                    cardGlow[i].enabled = true;
                    cardGlow[i].color = Gfx.A(d.L.nA, 0.3f);
                    cardGlow[i].transform.position = Gfx.W(R.x + sx + CW / 2, R.y + 70);
                    cardGlow[i].transform.localScale = new Vector3(1.7f, 1.4f, 1);
                }
                portraits[i].Place(R.x + sx + CW / 2 - 4, R.y + 116, 1, 0.8f);
                portraits[i].Apply(new Pose { anim = frame + i * 17, neon = neon, sil = !d.playable });
            }

            // combate
            for (int i = 0; i < 2; i++) for (int s = 0; s < 2; s++) fRigs[i, s].SetActive(false);
            for (int s = 0; s < 2; s++)
            {
                shadows[s].enabled = false; mounds[s].enabled = false; moundRings[s].enabled = false; columns[s].enabled = false; orbs[s].enabled = false;
                for (int b = 0; b < 7; b++) beams[s, b].enabled = false;
                if (!Fi || F[s] == null) continue;
                RenderFighter(F[s], s);
                RenderBeam(F[s], s);
            }
            RenderProjectiles(Fi);
            Fx.Render();
        }

        void RenderFighter(Fighter f, int s)
        {
            float size = f.L.size, air = GROUND - f.y;
            shadows[s].enabled = true;
            float rx = Mathf.Max(10f, 30f * size * (1f - air / 300f));
            shadows[s].transform.position = Gfx.W(f.x, GROUND + 3);
            shadows[s].transform.localScale = new Vector3(rx * 2 / 100f, 0.12f, 1);

            if (f.burT >= 0)
            {
                mounds[s].enabled = true; moundRings[s].enabled = true;
                mounds[s].transform.position = Gfx.W(f.x, GROUND + 1);
                mounds[s].transform.localScale = new Vector3(0.6f, 0.2f, 1);
                moundRings[s].transform.position = Gfx.W(f.x, GROUND - 1);
                moundRings[s].transform.localScale = new Vector3(0.58f, 0.17f, 1);
                moundRings[s].color = f.L.nA;
                return;
            }

            int species = f.def == Data.Roster[0] ? 0 : 1;
            var rig = fRigs[species, s];
            rig.SetActive(true);
            rig.Place(f.x, f.y, f.face, 1f);
            rig.group.sortingOrder = 10 + (f.atk != null ? 1 : 0);

            float k = 0;
            var a = f.atk;
            if (a != null)
            {
                var m = a.m;
                k = a.t < m.start ? a.t / (float)m.start : a.t <= m.start + m.active ? 1f : Mathf.Max(0f, 1f - (a.t - m.start - m.active) / (float)Mathf.Max(1, m.rec));
            }
            float tfp = f.tf > 0 ? 1f - f.tf / (float)TF_DUR : 0f;
            bool win = (phase == "end" || phase == "match") && !f.ko && F[1 - f.side].ko;
            rig.Apply(new Pose
            {
                anim = f.anim, k = k, move = a != null ? a.name : null,
                neon = f.neon || tfp > 0.55f, hit = f.stun > 0, ko = f.ko, block = f.block || f.bstun > 0,
                air = !f.Ground, walk = Mathf.Abs(f.vx) > 0.5f && f.Ground && a == null, win = win, flash = f.hurtFlash > 4
            });

            if (f.tf > 0)
            {
                columns[s].enabled = true;
                columns[s].transform.position = Gfx.W(f.x, f.y - 130);
                columns[s].transform.localScale = new Vector3(0.8f, 2.6f, 1);
                columns[s].color = Gfx.A(f.L.nA, 0.5f * Mathf.Sin(tfp * Mathf.PI));
            }
        }

        void RenderBeam(Fighter f, int s)
        {
            var a = f.atk;
            if (a == null || a.name != "prisma") return;
            var m = a.m;
            float size = f.L.size, bx = f.x + f.face * 38 * size, by = f.y - 83 * size;
            if (a.t < m.start)
            {
                orbs[s].enabled = true;
                float d = (4 + a.t * 0.9f) * 2 / 100f;
                orbs[s].transform.position = Gfx.W(bx, by);
                orbs[s].transform.localScale = new Vector3(d, d, 1);
                orbs[s].color = new Color(1, 1, 1, 0.9f);
            }
            else if (a.t <= m.start + m.active)
            {
                float end = f.face > 0 ? W + 20 : -20, len = Mathf.Abs(end - bx), cx = (bx + end) / 2f;
                float jit = Mathf.Sin(frame * 0.9f) * 2f;
                for (int i = 0; i < 6; i++)
                {
                    var sr = beams[s, i];
                    sr.enabled = true;
                    float y = by - 15 + i * 5 + jit * (i % 2 == 1 ? 1 : -1) + 2.25f;
                    sr.transform.position = Gfx.W(cx, y);
                    sr.transform.localScale = new Vector3(len / 100f, 0.045f, 1);
                    sr.color = Gfx.A(BEAM[i], 0.8f);
                }
                beams[s, 6].enabled = true;
                beams[s, 6].transform.position = Gfx.W(cx, by);
                beams[s, 6].transform.localScale = new Vector3(len / 100f, 0.04f, 1);
                beams[s, 6].color = new Color(1, 1, 1, 0.9f);
            }
        }

        void RenderProjectiles(bool visible)
        {
            foreach (var p in proj)
            {
                bool on = visible && p.delay <= 0;
                if (p.go.activeSelf != on) p.go.SetActive(on);
                if (!on) continue;
                p.go.transform.position = Gfx.W(p.x, p.y);
                if (p.type == "disco")
                {
                    p.main.transform.localRotation = Quaternion.Euler(0, 0, -p.spin * Mathf.Rad2Deg);
                    p.main.transform.localScale = new Vector3(p.r * 2.2f / 100f, p.r * 1.5f / 100f, 1);
                    for (int i = 0; i < 3; i++)
                    {
                        float an = p.spin + i * 2.09f;
                        BirdRig.Seg(p.spokes[i].transform, 0, 0, Mathf.Cos(an) * p.r * 0.8f, Mathf.Sin(an) * p.r * 0.55f, 2f);
                    }
                }
                else
                {
                    float ang = Mathf.Atan2(-p.vy, p.vx) * Mathf.Rad2Deg;
                    var shape = p.main.transform.parent;
                    shape.localRotation = Quaternion.Euler(0, 0, ang);
                    shape.localScale = new Vector3(1, 1 + Mathf.Sin(p.spin * 1.5f) * 0.6f, 1);
                }
            }
        }

        // ================================================================
        // INTERFAZ (IMGUI, dibujada en un lienzo virtual de 960x540)
        // ================================================================
        GUIStyle st;
        float gs = 1f, gx, gy;

        Rect R(float x, float y, float w, float h) { return new Rect(gx + x * gs, gy + y * gs, w * gs, h * gs); }

        void Fill(float x, float y, float w, float h, Color c)
        {
            GUI.color = c;
            GUI.DrawTexture(R(x, y, w, h), Gfx.White);
            GUI.color = Color.white;
        }

        void Frame(float x, float y, float w, float h, Color c, float t)
        {
            Fill(x, y, w, t, c); Fill(x, y + h - t, w, t, c); Fill(x, y, t, h, c); Fill(x + w - t, y, t, h, c);
        }

        /// <summary>align: -1 izquierda, 0 centro, 1 derecha. y es la línea base aproximada.</summary>
        void Txt(string s, float x, float y, float size, Color c, int align, FontStyle fs = FontStyle.Bold)
        {
            st.fontSize = Mathf.Max(1, Mathf.RoundToInt(size * gs));
            st.fontStyle = fs;
            st.normal.textColor = c;
            float top = y - size * 1.02f, h = size * 1.6f;
            Rect r;
            if (align < 0) { st.alignment = TextAnchor.UpperLeft; r = R(x, top, 1400, h); }
            else if (align == 0) { st.alignment = TextAnchor.UpperCenter; r = R(x - 700, top, 1400, h); }
            else { st.alignment = TextAnchor.UpperRight; r = R(x - 1400, top, 1400, h); }
            GUI.color = Color.white;
            GUI.Label(r, s, st);
        }

        void Glow(string s, float x, float y, float size, Color core, Color glow, float rad)
        {
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI / 6f;
                Txt(s, x + Mathf.Cos(a) * rad, y + Mathf.Sin(a) * rad, size, Gfx.A(glow, 0.14f * core.a), 0);
            }
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI / 3f;
                Txt(s, x + Mathf.Cos(a) * rad * 0.5f, y + Mathf.Sin(a) * rad * 0.5f, size, Gfx.A(glow, 0.3f * core.a), 0);
            }
            Txt(s, x, y, size, core, 0);
        }

        void Button(Rect r, string label, Color col, bool active)
        {
            if (active) Frame(r.x - 3, r.y - 3, r.width + 6, r.height + 6, Gfx.A(col, 0.25f), 3);
            Fill(r.x, r.y, r.width, r.height, active ? Gfx.A(col, 0.18f) : new Color(10 / 255f, 8 / 255f, 24 / 255f, 0.8f));
            Frame(r.x, r.y, r.width, r.height, col, active ? 2 : 1);
            Txt(label, r.x + r.width / 2, r.y + r.height / 2 + 6, 15, Data.C("#f2eeff"), 0);
        }

        void OnGUI()
        {
            if (st == null)
            {
                st = new GUIStyle(GUI.skin.label);
                st.wordWrap = false;
                st.clipping = TextClipping.Overflow;
                st.richText = false;
                st.padding = new RectOffset(0, 0, 0, 0);
                st.margin = new RectOffset(0, 0, 0, 0);
            }
            float sw = Screen.width, shh = Screen.height;
            gx = vp.x * sw; gy = (1f - vp.y - vp.height) * shh; gs = vp.width * sw / W;

            var e = Event.current;
            if (e.type == EventType.MouseDown)
            {
                click = new Vector2((e.mousePosition.x - gx) / gs, (e.mousePosition.y - gy) / gs);
                hasClick = true;
            }
            if (e.type != EventType.Repaint) return;

            // barras negras fuera del 16:9
            GUI.color = Color.black;
            if (gx > 0) { GUI.DrawTexture(new Rect(0, 0, gx, shh), Gfx.White); GUI.DrawTexture(new Rect(sw - gx, 0, gx, shh), Gfx.White); }
            if (gy > 0) { GUI.DrawTexture(new Rect(0, 0, sw, gy), Gfx.White); GUI.DrawTexture(new Rect(0, shh - gy, sw, gy), Gfx.White); }
            GUI.color = Color.white;

            if (scene == "title") DrawTitle();
            else if (scene == "select") DrawSelect();
            else DrawFight();
            GUI.matrix = Matrix4x4.identity;
            GUI.color = Color.white;
        }

        void DrawTitle()
        {
            int ph = frame % 150;
            if (ph < 10) Fill(0, 0, W, H, new Color(1, 1, 1, (10 - ph) / 30f));
            float fl = Random.value < 0.03f ? 0.4f : 1f;
            Glow("PLUMAJE", 480, 128, 62, Data.C("#ffe9f8"), MAG, 4);
            Glow("NEÓN", 480, 206, 70, Gfx.A(Data.C("#e6fffe"), fl), CYAN, 4);
            Txt("A V E S   D E L   V A L L E   D E L   C A U C A   ·   P R O T O T I P O", 480, 246, 13, SOFT, 0, FontStyle.Normal);
            for (int i = 0; i < 2; i++) Button(MENU[i], MENU_LABEL[i], i == 0 ? CYAN : MAG, menu == i);
            Txt("W/S o ↑/↓ para elegir  ·  Enter o clic para jugar", 480, 522, 13, MUTED, 0, FontStyle.Normal);
        }

        void DrawSelect()
        {
            Glow("E L I G E   T U   A V E", 480, 40, 28, Color.white, MAG, 3);
            Txt("Pelea, llena el tanque y transfórmate en androide neón", 480, 62, 13, MUTED, 0, FontStyle.Normal);
            for (int i = 0; i < 8; i++)
            {
                var d = Data.Roster[i];
                var r = CardRect(i);
                float sx = 0;
                for (int p = 0; p < 2; p++) if (selCur[p] == i && selShake[p] > 0) sx = Mathf.Sin(selShake[p] * 1.8f) * 4;
                float x = r.x + sx, y = r.y;
                Frame(x, y, CW, CH, LINE, 1);
                Fill(x + 1, y + CH - 28, CW - 2, 27, new Color(5 / 255f, 4 / 255f, 14 / 255f, 0.88f));
                Txt(d.name, x + 10, y + CH - 9, 14, d.playable ? Data.C("#f2eeff") : DIM, -1);
                if (!d.playable)
                {
                    var pivot = new Vector2(gx + (x + CW / 2) * gs, gy + (y + CH / 2 - 16) * gs);
                    GUIUtility.RotateAroundPivot(-17f, pivot);
                    float tx = x + CW / 2 - 86, ty = y + CH / 2 - 16 - 13;
                    Fill(tx, ty, 172, 26, LIME);
                    for (float k = 0; k < 172; k += 16) { Fill(tx + k, ty, 6, 3, INK); Fill(tx + k + 8, ty + 23, 6, 3, INK); }
                    Txt("COMING SOON", x + CW / 2, ty + 19, 15, INK, 0);
                    GUI.matrix = Matrix4x4.identity;
                }
            }
            for (int p = 0; p < 2; p++)
            {
                var r = CardRect(selCur[p]);
                Color col = p == 0 ? CYAN : MAG;
                float o = p == 0 ? -3 : -7;
                float t = selDone[p] ? 4 : 2;
                Frame(r.x + o - 2, r.y + o - 2, r.width - o * 2 + 4, r.height - o * 2 + 4, Gfx.A(col, 0.25f), 2);
                Frame(r.x + o, r.y + o, r.width - o * 2, r.height - o * 2, col, t);
                string tag = p == 0 ? "1P" : (mode == 1 ? "CPU" : "2P");
                float tw = tag.Length * 10 + 14, tx = p == 0 ? r.x - 3 : r.x + r.width + 7 - tw;
                Fill(tx, r.y - 22, tw, 18, col);
                Txt(tag, tx + tw / 2, r.y - 8, 13, Data.C("#07060f"), 0);
            }
            for (int p = 0; p < 2; p++)
            {
                var d = Data.Roster[selCur[p]];
                float x0 = p == 1 ? 488 : 116, y0 = 420, w = 356;
                Color col = p == 0 ? CYAN : MAG;
                Fill(x0, y0, w, 108, new Color(16 / 255f, 12 / 255f, 32 / 255f, 0.92f));
                Fill(x0, y0, 3, 108, col);
                string who = p == 0 ? "JUGADOR 1" : (mode == 1 ? "CPU" : "JUGADOR 2");
                Txt(who + (selDone[p] ? "  ·  LISTO" : ""), x0 + 16, y0 + 20, 11, col, -1);
                Txt(d.name, x0 + 16, y0 + 44, 19, Color.white, -1);
                Txt(d.sci, x0 + 16, y0 + 63, 13, MUTED, -1, FontStyle.Italic);
                if (d.playable)
                {
                    Txt("Forma neón: " + d.neonName, x0 + 16, y0 + 84, 12, d.L.nA, -1);
                    Txt("Especiales: " + d.sp[0] + " · " + d.sp[1], x0 + 16, y0 + 100, 12, SOFT, -1, FontStyle.Normal);
                }
                else Txt("Coming soon. Su forma neón está en desarrollo.", x0 + 16, y0 + 88, 12, DIM, -1, FontStyle.Normal);
            }
            if (toast != null)
            {
                st.fontSize = Mathf.RoundToInt(14 * gs);
                float tw = st.CalcSize(new GUIContent(toast)).x / gs + 32;
                Fill(480 - tw / 2, 388, tw, 26, LIME);
                Txt(toast, 480, 406, 14, INK, 0);
            }
            if (selT > 0)
            {
                Fill(0, 0, W, H, new Color(8 / 255f, 5 / 255f, 20 / 255f, 0.6f * (1 - selT / 60f)));
                Glow("¡A PELEAR!", 480, 300, 64, Color.white, MAG, 5);
            }
            Txt("Esc: volver", W - 16, H - 8, 11, Data.C("#6f6898"), 1, FontStyle.Normal);
        }

        void HpBar(float x, float y, float w, float h, Fighter f, bool rev)
        {
            Fill(x, y, w, h, Data.C("#140f2a"));
            float lag = w * f.hpLag / 100f, hp = w * Mathf.Max(0, f.hp) / 100f;
            Fill(rev ? x + w - lag : x, y, lag, h, Data.C("#ff4d6d"));
            Color c0 = f.neon ? f.L.nA : Data.C("#ffd23f"), c1 = f.neon ? f.L.nB : Data.C("#ff9f1c");
            const int slices = 24;
            float sw = hp / slices;
            for (int i = 0; i < slices; i++)
            {
                float t = i / (float)(slices - 1);
                float sx = rev ? x + w - hp + i * sw : x + i * sw;
                Fill(sx, y, sw + 0.6f, h, Color.Lerp(c0, c1, rev ? 1 - t : t));
            }
            if (f.neon) Frame(rev ? x + w - hp - 2 : x - 2, y - 2, hp + 4, h + 4, Gfx.A(f.L.nA, 0.35f), 2);
            Fill(rev ? x + w - hp : x, y, hp, 4, new Color(1, 1, 1, 0.18f));
            Frame(x, y, w, h, LINE2, 1);
        }

        void TankBar(float x, float y, float w, float h, Fighter f, bool rev)
        {
            bool full = f.tank >= 100 && !f.neon, blink = (frame / 15) % 2 == 0;
            var K = KEYLABEL[f.side];
            Txt(f.neon ? "MODO NEÓN" : "TANQUE NEÓN", rev ? x + w : x, y - 6, 11, f.neon ? f.L.nA : MUTED, rev ? 1 : -1);
            if (full && !f.cpu && blink) Txt("¡LISTO! PULSA " + K[0], rev ? x : x + w, y - 6, 11, f.L.nB, rev ? -1 : 1);
            Fill(x, y, w, h, Data.C("#140f2a"));
            float fw = w * Mathf.Clamp(f.tank, 0, 100) / 100f;
            Color col = f.neon ? f.L.nA : full ? f.L.nB : Data.C("#7b6cf0");
            if (f.neon || full) Frame(rev ? x + w - fw - 3 : x - 3, y - 3, fw + 6, h + 6, Gfx.A(col, full && blink ? 0.45f : 0.25f), 3);
            Fill(rev ? x + w - fw : x, y, fw, h, col);
            for (int i = 1; i < 10; i++) Fill(x + w * i / 10f - 1, y, 2, h, Data.C("#07060f"));
            Frame(x, y, w, h, full ? f.L.nB : LINE2, 1);
            if (f.neon && !f.cpu)
                Txt(K[1] + ": " + f.def.sp[0] + "    " + K[2] + "+" + K[1] + ": " + f.def.sp[1], rev ? x + w : x, y + h + 14, 11, f.L.nB, rev ? 1 : -1);
        }

        void Dot(float x, float y, bool on, Color col)
        {
            if (on) { Fill(x - 7, y - 7, 14, 14, Gfx.A(col, 0.25f)); Fill(x - 5, y - 5, 10, 10, col); }
            else Frame(x - 5, y - 5, 10, 10, LINE2, 1.5f);
        }

        void DrawFight()
        {
            Fighter a = F[0], b = F[1];
            if (a == null) return;
            HpBar(40, 22, 384, 20, a, false);
            HpBar(536, 22, 384, 20, b, true);
            Txt(a.def.name.ToUpper(), 40, 62, 15, Color.white, -1);
            Txt(b.def.name.ToUpper() + (b.cpu ? "  ·  CPU" : ""), 920, 62, 15, Color.white, 1);
            if (a.neon) Txt(a.def.neonName, 40, 78, 11, a.L.nA, -1);
            if (b.neon) Txt(b.def.neonName, 920, 78, 11, b.L.nA, 1);
            for (int i = 0; i < 2; i++) { Dot(410 - i * 16, 56, wins[0] > i, CYAN); Dot(550 + i * 16, 56, wins[1] > i, MAG); }
            Fill(446, 14, 68, 40, new Color(8 / 255f, 6 / 255f, 20 / 255f, 0.85f));
            Frame(446, 14, 68, 40, Data.C("#3a3070"), 1);
            int tsec = Mathf.CeilToInt(timer / 60f);
            Txt(tsec.ToString(), 480, 46, 30, tsec <= 10 ? Data.C("#ff4d6d") : Color.white, 0);
            TankBar(40, 500, 268, 14, a, false);
            TankBar(652, 500, 268, 14, b, true);
            Txt("Esc: pausa", 480, 530, 11, new Color(0.86f, 0.82f, 1f, 0.55f), 0, FontStyle.Normal);

            if (phase == "intro")
            {
                if (phaseT > 40) Glow("R O N D A   " + round, 480, 280, 48, Color.white, CYAN, 4);
                else Glow("¡A PELEAR!", 480, 284, 64, Color.white, MAG, 5);
            }
            if (banner != null && phase != "match")
            {
                Glow(banner.text, 480, 262, banner.neon ? 54 : 72, Color.white, banner.col, 5);
                if (banner.sub != null) Glow(banner.sub, 480, 300, 16, banner.col2, banner.col, 2);
            }
            if (flash > 0) Fill(0, 0, W, H, new Color(1, 1, 1, flash / 14f));
            if (phase == "match" && matchWinner >= 0)
            {
                var w = F[matchWinner];
                Fill(0, 0, W, H, new Color(6 / 255f, 4 / 255f, 16 / 255f, 0.72f));
                Txt("G A N A", 480, 200, 20, MUTED, 0);
                Glow(w.def.name.ToUpper(), 480, 262, 54, Color.white, w.L.nA, 5);
                Txt(wins[0] + " – " + wins[1], 480, 300, 18, SOFT, 0);
                Button(BTN_REMATCH, "REVANCHA · ENTER", CYAN, true);
                Button(BTN_SELECT, "ELEGIR AVES · ESC", MAG, false);
            }
            if (paused)
            {
                Fill(0, 0, W, H, new Color(6 / 255f, 4 / 255f, 16 / 255f, 0.7f));
                Glow("P A U S A", 480, 250, 46, Color.white, CYAN, 4);
                Button(BTN_MENU, "MENÚ · M", MAG, false);
                Txt("Esc o clic para continuar", 480, 370, 13, MUTED, 0, FontStyle.Normal);
            }
        }
    }
}
