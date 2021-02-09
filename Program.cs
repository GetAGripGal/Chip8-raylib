using System;
using Raylib_cs;

namespace Raylib_Chip8Emu
{
    class Program
    {
        static void Main(string[] args)
        {
            var renderer = new Renderer();
            var cpu = new Cpu(renderer);
            
            cpu.Init("[Rom path here]");
            
            while (!Raylib.WindowShouldClose())
            {
                cpu.Cycle();
                var currentKey = (KeyboardKey) Raylib.GetKeyPressed();
                if (!Raylib.IsKeyDown(0) && cpu._keyMap.ContainsKey(currentKey)) cpu.NextKeyEvent?.Invoke(currentKey); // Invoke key event
            }
        }
    }
}
