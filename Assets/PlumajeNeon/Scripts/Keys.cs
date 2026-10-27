using UnityEngine;
#if PN_INPUTSYSTEM && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace PlumajeNeon
{
    public enum K
    {
        A, D, W, S, F, G, H, R,
        Left, Right, Up, Down, KeyK, KeyL, Semicolon, KeyP,
        Num0, Num1, Num2, Num3,
        Enter, Space, Escape, Backspace, M,
        Count
    }

    /// <summary>
    /// Teclado para ambos sistemas de input de Unity (Input System nuevo o Input Manager clásico).
    /// Usa posición física de tecla: "Semicolon" es la Ñ en un teclado latinoamericano o español.
    /// </summary>
    public static class Keys
    {
        static readonly bool[] held = new bool[(int)K.Count];
        static readonly bool[] edge = new bool[(int)K.Count];

        /// <summary>Lee el teclado. Las pulsaciones nuevas se acumulan hasta que el juego las consume con ClearEdges().</summary>
        public static void Poll()
        {
            for (int i = 0; i < (int)K.Count; i++)
            {
                bool h = IsDown((K)i);
                if (h && !held[i]) edge[i] = true;
                held[i] = h;
            }
        }

        public static void ClearEdges() { System.Array.Clear(edge, 0, edge.Length); }
        public static bool Held(K k) { return held[(int)k]; }
        public static bool Pressed(K k) { return edge[(int)k]; }

        public static bool Any(params K[] ks)
        {
            foreach (var k in ks) if (edge[(int)k]) return true;
            return false;
        }

#if PN_INPUTSYSTEM && ENABLE_INPUT_SYSTEM
        static bool IsDown(K k)
        {
            var kb = Keyboard.current;
            if (kb == null) return false;
            switch (k)
            {
                case K.A: return kb[Key.A].isPressed;
                case K.D: return kb[Key.D].isPressed;
                case K.W: return kb[Key.W].isPressed;
                case K.S: return kb[Key.S].isPressed;
                case K.F: return kb[Key.F].isPressed;
                case K.G: return kb[Key.G].isPressed;
                case K.H: return kb[Key.H].isPressed;
                case K.R: return kb[Key.R].isPressed;
                case K.Left: return kb[Key.LeftArrow].isPressed;
                case K.Right: return kb[Key.RightArrow].isPressed;
                case K.Up: return kb[Key.UpArrow].isPressed;
                case K.Down: return kb[Key.DownArrow].isPressed;
                case K.KeyK: return kb[Key.K].isPressed;
                case K.KeyL: return kb[Key.L].isPressed;
                case K.Semicolon: return kb[Key.Semicolon].isPressed;
                case K.KeyP: return kb[Key.P].isPressed;
                case K.Num0: return kb[Key.Numpad0].isPressed;
                case K.Num1: return kb[Key.Numpad1].isPressed;
                case K.Num2: return kb[Key.Numpad2].isPressed;
                case K.Num3: return kb[Key.Numpad3].isPressed;
                case K.Enter: return kb[Key.Enter].isPressed || kb[Key.NumpadEnter].isPressed;
                case K.Space: return kb[Key.Space].isPressed;
                case K.Escape: return kb[Key.Escape].isPressed;
                case K.Backspace: return kb[Key.Backspace].isPressed;
                case K.M: return kb[Key.M].isPressed;
            }
            return false;
        }
#else
        static bool IsDown(K k)
        {
            switch (k)
            {
                case K.A: return Input.GetKey(KeyCode.A);
                case K.D: return Input.GetKey(KeyCode.D);
                case K.W: return Input.GetKey(KeyCode.W);
                case K.S: return Input.GetKey(KeyCode.S);
                case K.F: return Input.GetKey(KeyCode.F);
                case K.G: return Input.GetKey(KeyCode.G);
                case K.H: return Input.GetKey(KeyCode.H);
                case K.R: return Input.GetKey(KeyCode.R);
                case K.Left: return Input.GetKey(KeyCode.LeftArrow);
                case K.Right: return Input.GetKey(KeyCode.RightArrow);
                case K.Up: return Input.GetKey(KeyCode.UpArrow);
                case K.Down: return Input.GetKey(KeyCode.DownArrow);
                case K.KeyK: return Input.GetKey(KeyCode.K);
                case K.KeyL: return Input.GetKey(KeyCode.L);
                case K.Semicolon: return Input.GetKey(KeyCode.Semicolon);
                case K.KeyP: return Input.GetKey(KeyCode.P);
                case K.Num0: return Input.GetKey(KeyCode.Keypad0);
                case K.Num1: return Input.GetKey(KeyCode.Keypad1);
                case K.Num2: return Input.GetKey(KeyCode.Keypad2);
                case K.Num3: return Input.GetKey(KeyCode.Keypad3);
                case K.Enter: return Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter);
                case K.Space: return Input.GetKey(KeyCode.Space);
                case K.Escape: return Input.GetKey(KeyCode.Escape);
                case K.Backspace: return Input.GetKey(KeyCode.Backspace);
                case K.M: return Input.GetKey(KeyCode.M);
            }
            return false;
        }
#endif
    }
}
