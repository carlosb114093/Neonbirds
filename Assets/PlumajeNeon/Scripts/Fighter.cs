using UnityEngine;

namespace PlumajeNeon
{
    public class Inp { public bool l, r, u, d, p, k, s, x; }

    public class Ctl
    {
        public Inp h = new Inp(), e = new Inp();   // h = mantenida, e = recién pulsada
        public static readonly Ctl None = new Ctl();
    }

    public class Atk { public string name; public Move m; public int t; public bool hit; }

    public struct Box
    {
        public float x1, x2, y1, y2;
        public bool Overlaps(Box b) { return x1 < b.x2 && x2 > b.x1 && y1 < b.y2 && y2 > b.y1; }
    }

    public class AIState { public int t = 20; public string hold, press; }

    /// <summary>Lógica de un luchador. Unidades: px del lienzo 960x540, y hacia abajo, 60 pasos por segundo.</summary>
    public class Fighter
    {
        public BirdDef def;
        public Look L;
        public int side;
        public bool cpu;
        public float x, y, vx, vy;
        public int face;
        public float hp = 100, hpLag = 100, tank;
        public bool neon, block, ko;
        public int neonT, tf, stun, bstun, cool, inv, hurtFlash;
        public int burT = -1;   // >= 0 mientras está enterrado (Túnel del Barranco)
        public Atk atk;
        public float anim;
        public AIState ai = new AIState();

        public Fighter(BirdDef d, int side, bool cpu)
        {
            def = d; L = d.L; this.side = side; this.cpu = cpu;
            x = side == 1 ? 660 : 300;
            y = Game.GROUND;
            face = side == 1 ? -1 : 1;
            anim = Random.Range(0f, 100f);
        }

        public bool Ground { get { return y >= Game.GROUND - 0.5f; } }

        public Box Hurt()
        {
            float s = L.size;
            return new Box { x1 = x - 22 * s, x2 = x + 22 * s, y1 = y - 118 * s, y2 = y };
        }

        public Box Hitbox(Move m)
        {
            float s = L.size, a = x + face * m.rx0 * s, b = x + face * m.rx1 * s;
            return new Box { x1 = Mathf.Min(a, b), x2 = Mathf.Max(a, b), y1 = y + m.ry0 * s, y2 = y + m.ry1 * s };
        }

        public void StartAtk(string name)
        {
            atk = new Atk { name = name, m = Data.Moves[name] };
            block = false;
            if (name == "picada") { vy = Mathf.Max(vy, 5f); vx = face * 5.5f; }
            else if (name == "ascenso") vy = -12.5f;
        }

        void TransformNeon()
        {
            var g = Game.I;
            tf = Game.TF_DUR; inv = Game.TF_DUR; vx = 0; atk = null; block = false;
            g.cine = Game.TF_DUR; g.flash = 6;
            Sfx.Play("transform");
            g.Ring(x, y - 60, L.nA, 10, 6, 40);
            g.Ring(x, y - 60, L.nB, 4, 4, 46);
            g.banner = new Banner { text = "¡MODO NEÓN!", sub = def.neonName, t = 100, col = L.nA, col2 = L.nB, neon = true };
        }

        void Special(bool down, Fighter opp)
        {
            string mv;
            if (def.id == "barranquero")
            {
                if (down) { burT = 0; inv = 999; vx = 0; cool = 85; Sfx.Play("dig"); Game.I.Dirt(x, Game.GROUND, 10); return; }
                mv = "disco";
            }
            else mv = down ? "bandada" : "prisma";
            StartAtk(mv);
            cool = Data.Moves[mv].cool;
            vx = 0;
        }

        public void Update(Ctl c, Fighter opp)
        {
            var g = Game.I;
            anim++;
            if (cool > 0) cool--;
            if (inv > 0 && burT < 0) inv--;
            if (hurtFlash > 0) hurtFlash--;
            if (hpLag > hp) hpLag = Mathf.Max(hp, hpLag - 0.4f);
            if (neon)
            {
                neonT--;
                tank = neonT / (float)Game.NEON_DUR * 100f;
                if ((int)anim % 4 == 0) g.Spark(x - face * 18, y - 36, -face * Random.Range(0.5f, 2f), Random.Range(1f, 3f), 16, L.nB, 2);
                if (neonT <= 0) { neon = false; tank = 0; g.Burst(this, x, y - 70, 14); }
            }
            if (tf > 0)
            {
                tf--;
                if (tf % 8 == 0) g.Ring(x, y - 60, (tf % 16 != 0) ? L.nA : L.nB, 6, 5, 26);
                if (tf == 0) { neon = true; neonT = Game.NEON_DUR; tank = 100; g.Burst(this, x, y - 70, 22); g.flash = 8; }
                return;
            }
            if (ko) { Phys(0.9f); return; }
            if (burT >= 0) { UpdBurrow(opp); return; }
            if (stun > 0) { stun--; Phys(0.86f); return; }
            if (bstun > 0) { bstun--; Phys(0.8f); return; }
            if (atk != null)
            {
                UpdAtk(opp);
                if (atk != null || !Ground) Phys(Ground ? 0.8f : 1f);
                return;
            }

            Inp h = c.h, e = c.e;
            if (Ground) face = opp.x >= x ? 1 : -1;
            block = Ground && h.d;
            float spd = def.speed * (neon ? 1.15f : 1f);
            int dir = (h.r ? 1 : 0) - (h.l ? 1 : 0);

            if (e.x && tank >= 100 && !neon) { TransformNeon(); return; }
            if (e.s && neon && cool == 0) { Special(h.d, opp); return; }
            if (e.p) { StartAtk("pico"); return; }
            if (e.k) { StartAtk(Ground ? "garra" : "picada"); return; }

            if (Ground)
            {
                if (block) vx *= 0.6f;
                else
                {
                    vx = dir * spd;
                    if (e.u) { vy = -def.jump; vx = dir * spd * 1.1f; }
                }
            }
            else vx = Mathf.Clamp(vx + dir * 0.25f, -spd, spd);
            Phys(1f);
        }

        void UpdAtk(Fighter opp)
        {
            var a = atk;
            var m = a.m;
            a.t++;
            if (a.name == "disco" && a.t == 13) Game.I.SpawnDisco(this);
            if (a.name == "prisma" && a.t == 16) Sfx.Play("beam");
            if (a.name == "bandada" && a.t == 10) Game.I.SpawnFlock(this);
            if (m.lunge > 0 && a.t == m.start) vx = face * m.lunge;
            if (m.dmg > 0 && !a.hit && a.t > m.start && a.t <= m.start + m.active)
            {
                if (Hitbox(m).Overlaps(opp.Hurt()) && Game.I.HitFighter(this, opp, m, float.NaN)) a.hit = true;
            }
            if (m.air && Ground && a.t > m.start) { atk = null; vx = 0; bstun = 6; return; }
            if (a.t >= m.Total) atk = null;
        }

        void UpdBurrow(Fighter opp)
        {
            burT++;
            y = Game.GROUND; vy = 0;
            if (burT > 8 && burT < 40)
            {
                x = Mathf.Clamp(x + Mathf.Clamp(opp.x - x, -7f, 7f), 36, Game.W - 36);
                if (burT % 3 == 0) Game.I.Dirt(x, Game.GROUND, 2);
            }
            if (burT >= 40)
            {
                burT = -1; inv = 0;
                face = opp.x >= x ? 1 : -1;
                Game.I.Dirt(x, Game.GROUND, 16);
                StartAtk("ascenso");
            }
        }

        void Phys(float fr)
        {
            vy += 0.72f;
            x += vx;
            y += vy;
            if (y >= Game.GROUND)
            {
                if (vy > 7 && (stun > 0 || ko)) { Game.I.Dirt(x, Game.GROUND, 6); Game.I.shake = Mathf.Max(Game.I.shake, 4f); }
                y = Game.GROUND; vy = 0; vx *= fr;
            }
            x = Mathf.Clamp(x, 36, Game.W - 36);
        }
    }
}
