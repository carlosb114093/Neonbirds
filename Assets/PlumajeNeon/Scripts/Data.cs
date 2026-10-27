using System.Collections.Generic;
using UnityEngine;

namespace PlumajeNeon
{
    public enum TailType { Racket, Short, Long, Fan }

    /// <summary>Colores y forma de un ave. Todo el dibujo se genera por código a partir de esto.</summary>
    public class Look
    {
        public float size = 1f;
        public Color body = Data.C("#55606e"), belly = Data.C("#6b7685"), head = Data.C("#55606e"), wing = Data.C("#4a5461");
        public Color tail = Data.C("#4a5461"), beak = Data.C("#222222"), eye = Data.C("#111111"), leg = Data.C("#5a5a5a");
        public Color crown = Data.C("#3fb5ea"), mask = Data.C("#0d0d0d"), cheek = Data.C("#111111"), throat = Data.C("#ff8a1f");
        public Color nA = Color.cyan, nB = Color.magenta;   // colores de la forma neón
        public bool hasCrown, hasMask, hasCheek, hasThroat, hook, thin;
        public TailType tailType = TailType.Short;
        public float beakLen = 14f;
    }

    public class BirdDef
    {
        public string id, name, sci, neonName;
        public bool playable;
        public float speed = 4f, jump = 13.5f, power = 1f;
        public string[] sp = new string[0];
        public Look L;
    }

    /// <summary>Un ataque. Tiempos en frames (60 fps), distancias en píxeles del lienzo de 960x540.</summary>
    public class Move
    {
        public string name;
        public int start, active, rec, cool, stun;
        public float dmg, kb, launch, lunge, gain;
        public float rx0, rx1, ry0, ry1;   // caja de golpe relativa a los pies (x hacia donde mira, y hacia abajo)
        public bool air;
        public string sfx = "hit";
        public int Total { get { return start + active + rec; } }
    }

    public static class Data
    {
        public static Color C(string hex)
        {
            Color c;
            if (!ColorUtility.TryParseHtmlString(hex, out c)) c = Color.magenta;
            return c;
        }

        public static readonly BirdDef[] Roster;
        public static readonly Dictionary<string, Move> Moves;

        static Data()
        {
            var barranquero = new Look
            {
                size = 1.06f, body = C("#4f8b3b"), belly = C("#c7783a"), head = C("#4f8b3b"), wing = C("#3d7330"),
                tail = C("#3f7f9a"), tailType = TailType.Racket, beak = C("#1a1a1a"), beakLen = 20f, eye = C("#e0262f"),
                leg = C("#6b6b6b"), crown = C("#3fb5ea"), hasCrown = true, mask = C("#0d0d0d"), hasMask = true,
                nA = C("#00f0ff"), nB = C("#ff7a1a")
            };
            var tangara = new Look
            {
                size = 0.94f, body = C("#2fb44b"), belly = C("#27a6cf"), head = C("#ffd60a"), wing = C("#23923a"),
                tail = C("#1f8a3a"), tailType = TailType.Short, beak = C("#262626"), beakLen = 12f, eye = C("#111111"),
                leg = C("#7a6a55"), throat = C("#ff8a1f"), hasThroat = true, cheek = C("#111111"), hasCheek = true,
                nA = C("#ff2bd6"), nB = C("#d4ff2a")
            };

            Roster = new BirdDef[]
            {
                new BirdDef { id = "barranquero", name = "Barranquero", sci = "Momotus momota", playable = true,
                    neonName = "BARRANQUERO PÉNDULO-X", speed = 3.5f, jump = 13.4f, power = 1.08f,
                    sp = new[] { "Disco Péndulo", "Túnel del Barranco" }, L = barranquero },
                new BirdDef { id = "tangara", name = "Tángara multicolor", sci = "Chlorochrysa nitidissima", playable = true,
                    neonName = "TÁNGARA PRISMA-7", speed = 4.3f, jump = 14.2f, power = 0.94f,
                    sp = new[] { "Rayo Prisma", "Bandada Mixta" }, L = tangara },
                Locked("guacamaya", "Guacamaya cariseca", "Ara severus", new Look { size = 1.12f, tailType = TailType.Long, beakLen = 15f, hook = true, nA = C("#39ff88"), nB = C("#ff4d4d") }),
                Locked("bichofue", "Bichofué gritón", "Pitangus sulphuratus", new Look { size = 0.98f, beakLen = 17f, nA = C("#ffe600"), nB = C("#ffffff") }),
                Locked("azulejo", "Azulejo común", "Thraupis episcopus", new Look { size = 0.9f, beakLen = 11f, nA = C("#6fb7ff"), nB = C("#b9e2ff") }),
                Locked("pava", "Pava caucana", "Penelope perspicax", new Look { size = 1.16f, tailType = TailType.Fan, beakLen = 12f, nA = C("#ff3b3b"), nB = C("#c08bff") }),
                Locked("lora", "Lora común", "Amazona ochrocephala", new Look { size = 1.02f, beakLen = 12f, hook = true, nA = C("#7dff4d"), nB = C("#ffd400") }),
                Locked("colibri", "Amazilia colirrufa", "Amazilia tzacatl", new Look { size = 0.8f, beakLen = 30f, thin = true, nA = C("#2bffe0"), nB = C("#ff8a3d") }),
            };

            Moves = new Dictionary<string, Move>();
            Add(new Move { name = "pico", start = 4, active = 4, rec = 11, dmg = 6, rx0 = 10, rx1 = 68, ry0 = -120, ry1 = -80, kb = 4, stun = 15, gain = 10 });
            Add(new Move { name = "garra", start = 7, active = 5, rec = 16, dmg = 9, rx0 = 0, rx1 = 84, ry0 = -52, ry1 = -4, kb = 7, stun = 18, gain = 12, sfx = "heavy", lunge = 2.6f });
            Add(new Move { name = "picada", start = 4, active = 40, rec = 6, dmg = 8, rx0 = -10, rx1 = 54, ry0 = -40, ry1 = 12, kb = 5, stun = 16, gain = 9, air = true });
            Add(new Move { name = "disco", start = 13, active = 0, rec = 18, cool = 55 });
            Add(new Move { name = "prisma", start = 16, active = 16, rec = 18, dmg = 13, rx0 = 30, rx1 = 600, ry0 = -102, ry1 = -64, kb = 9, stun = 24, gain = 0, cool = 80, sfx = "heavy" });
            Add(new Move { name = "bandada", start = 10, active = 0, rec = 16, cool = 90 });
            Add(new Move { name = "ascenso", start = 2, active = 10, rec = 20, dmg = 14, rx0 = -34, rx1 = 46, ry0 = -150, ry1 = 0, kb = 3, launch = 12, stun = 34, gain = 0, sfx = "heavy" });
        }

        static BirdDef Locked(string id, string name, string sci, Look l)
        {
            return new BirdDef { id = id, name = name, sci = sci, playable = false, L = l };
        }

        static void Add(Move m) { Moves[m.name] = m; }
    }
}
