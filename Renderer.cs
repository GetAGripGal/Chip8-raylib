using System;
using Raylib_cs;

namespace Raylib_Chip8Emu
{
    public class Renderer
    {
        private bool[,] _pixelScreen = new bool[32,64];
        private int _pixelScale = 16;
        
        public enum DrawingColor
        {
            White=0,
            Black
        }

        public Renderer()
        {
            Raylib.InitWindow(1024, 512, "Chip8 emulator");
            Raylib.SetTargetFPS(60);
        }
        
        public void SetScreen(bool[,] screen)
        {
            _pixelScreen = screen;
        }

        public bool SetPixel(long x, long y)
        {
            _pixelScreen[y,x] = !_pixelScreen[y,x]; // returns true if a pixel is erased.
            return !_pixelScreen[y, x];
        }

        public void Clear()
        {
            _pixelScreen = new bool[32, 64];
        }
    
        public void Render()
        {
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.BLACK);
            for (var x = 0; x < _pixelScreen.GetLength(0); x++)
            {
                for (var y = 0; y < _pixelScreen.GetLength(1); y++)
                {
                    if (_pixelScreen[x, y])
                    {
                        Raylib.DrawRectangle(y * _pixelScale, x * _pixelScale, _pixelScale, _pixelScale, Color.WHITE);
                    }
                }
            }
            Raylib.EndDrawing();
        }
    }
}