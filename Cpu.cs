using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Raylib_cs;

namespace Raylib_Chip8Emu
{
    public class Cpu
    {
        // Components
        private Renderer _renderer;
        
        // --- Memory ---
        // 4KB of memory
        private long[] _memory = new long[4096];
        // 16 8-bit registers
        private long[] _registers = new long[16];
        // stores the current memory address
        private long _i = 0;
        
        // --- Timers ---
        private long _delayTimer = 0;
        private long _soundTimer = 0;
        
        // Program counter. Stores the currently executing address.
        private long _pc = 0x200;

        private List<long> _stack = new List<long>();
        private bool _paused = false;
        private int _speed = 10;
        
        // Delegates
        public delegate void NextInput(KeyboardKey key);
        public NextInput NextKeyEvent;
        
        // Keys
        public Dictionary<KeyboardKey, long> _keyMap = new Dictionary<KeyboardKey, long>
        {
            {KeyboardKey.KEY_ONE, 0x1}, // 1
            {KeyboardKey.KEY_TWO, 0x2}, // 2
            {KeyboardKey.KEY_THREE, 0x3}, // 3
            {KeyboardKey.KEY_FOUR, 0xc}, // 4
            {KeyboardKey.KEY_Q, 0x4}, // Q
            {KeyboardKey.KEY_W, 0x5}, // W
            {KeyboardKey.KEY_E, 0x6}, // E
            {KeyboardKey.KEY_R, 0xD}, // R
            {KeyboardKey.KEY_A, 0x7}, // A
            {KeyboardKey.KEY_S, 0x8}, // S
            {KeyboardKey.KEY_D, 0x9}, // D
            {KeyboardKey.KEY_F, 0xE}, // F
            {KeyboardKey.KEY_Z, 0xA}, // Z
            {KeyboardKey.KEY_X, 0x0}, // X
            {KeyboardKey.KEY_C, 0xB}, // C
            {KeyboardKey.KEY_V, 0xF}  // V

        };
        
        public Cpu(Renderer renderer)
        {
            _renderer = renderer;
        }
        
        public void Init (string romPath)
        {
            LoadSpriteIntoMemory();
            LoadRom(romPath);
        }

        public void Cycle()
        {
            for (var i = 0; i < _speed; i++)
            {
                if (_paused) break;
                var opcode = _memory[_pc] << 8 | _memory[_pc + 1];
                ExecuteInstructions(opcode);
            }

            if (!_paused)
            {
                UpdateTimers();
            }
            
            // TODO: Implement sound system.
            // PlaySound();
            _renderer.Render();
        }

        private void UpdateTimers()
        {
            if (_delayTimer > 0)
                _delayTimer--;
            if (_soundTimer > 0)
                _soundTimer--;
        }

        private void ExecuteInstructions(long opcode)
        {
            Console.WriteLine($"Executing: 0x{(opcode & 0xF000):X}");
            _pc += 2;

            var x = (opcode & 0x0F00) >> 8;
            var y = (opcode & 0x00F0) >> 4;
            
            switch (opcode & 0xF000)
            {
                case 0x0000:
                    switch (opcode)
                    {
                        case 0x00E0:
                            _renderer.Clear();
                            break;
                        case 0x00EE:
                            _pc = _stack[0];
                            _stack.RemoveAt(0);
                            break;
                    }
                    break;
                case 0x1000:
                    _pc = opcode & 0xFFF;
                    break;
                case 0x2000:
                    _stack.Add(_pc);
                    _pc = opcode & 0xFFF;
                    break;
                case 0x3000:
                    if (_registers[x] == (opcode & 0xFF)) 
                        _pc += 2;
                    break;
                case 0x4000:
                    if (_registers[x] != (opcode & 0xFF)) 
                        _pc += 2;
                    break;
                case 0x5000:
                    if (_registers[x] == _registers[y]) 
                        _pc += 2;
                    break;
                case 0x6000:
                    _registers[x] = (opcode & 0xFF);
                    break; 
                case 0x7000:
                    _registers[x] += (opcode & 0xFF);
                    break;
                case 0x8000:
                    Console.WriteLine($"Executing: 0x8000, 0x{opcode & 0xF:X}");
                    switch (opcode & 0xF)
                    {
                        case 0x0:
                            _registers[x] = _registers[y];
                            break;
                        case 0x1:
                            _registers[x] |= _registers[y];
                            break;
                        case 0x2:
                            _registers[x] &= _registers[y];
                            break;
                        case 0x3:
                            _registers[x] ^= _registers[y];
                            break;
                        case 0x4:
                            var sum = _registers[x] += _registers[y];
                            _registers[0xF] = 0;
                            if (sum > 0xFF)
                                _registers[0xF] = 1;
                            _registers[x] = sum;
                            break;
                        case 0x5:
                            _registers[0xF] = 0;
                            if (_registers[x] > _registers[y]) 
                                _registers[0xF] = 1;
                            _registers[x] -= _registers[y];
                            break;
                        case 0x6:
                            _registers[0xF] = _registers[x] & 0x1;
                            _registers[x] >>= 1;
                            break;
                        case 0x7:
                            _registers[0xF] = 0;
                            if (_registers[y] > _registers[x]) 
                                _registers[0xF] = 1;
                            _registers[x] = _registers[y] - _registers[x];
                            break;
                        case 0xE:
                            _registers[0xF] = _registers[x] & 0x80;
                            _registers[x] <<= 1;
                            break;
                    }
                    break;
                case 0x9000:
                    if (_registers[x] != _registers[y]) 
                        _pc += 2;
                    break; 
                case 0xA000:
                    _i = opcode & 0xFFF;
                    break;
                case 0xB000:
                    _pc = (opcode & 0xFFF) + _registers[0];
                    break; 
                case 0xC000:
                    var rand = new Random().Next(0, 0xFF);
                    _registers[x] = rand & opcode & 0xFF;
                    break;
                case 0xD000:
                    var width = 8;
                    var height = opcode & 0xF;
                    
                    _registers[0xF] = 0;

                    for (var row = 0; row < height; row++)
                    {
                        var sprite = _memory[_i + row];
                        
                        for (var col = 0; col < width; col++)
                        {
                            if ((sprite & 0x80) > 0)
                            {
                                var xPos = _registers[x] + col;
                                var yPos = _registers[y] + row;

                                if ((xPos >= 0 && xPos < 64) && (yPos >= 0 && yPos < 32))
                                {
                                    var pixelRes = _renderer.SetPixel(xPos, yPos);
                                    if (pixelRes) _registers[0xF] = 1;
                                }
                            }

                            sprite <<= 1;
                        }
                    }
                    break;
                case 0xE000:
                    switch (opcode & 0xFF)    
                    {
                        case 0x9E:
                            var key = _keyMap.FirstOrDefault(i => i.Value == _registers[x] ).Key;
                            if (Raylib.IsKeyDown(key))
                                _pc += 2;
                            break;
                        case 0xA1:
                            var notKey = _keyMap.FirstOrDefault(i => i.Value == _registers[x] ).Key;
                            if (!Raylib.IsKeyDown(notKey))
                                _pc += 2;
                            break;
                    }

                    break;
                case 0xF000:
                    Console.WriteLine($"Executing: 0xF000, 0x{opcode & 0xFF:X}");
                    switch (opcode & 0xFF) {
                        case 0x07:
                            _registers[x] = _delayTimer;
                            break;
                        case 0x0A:
                            _paused = true;
                            NextKeyEvent = (key) =>
                            {
                                _registers[x] = _keyMap[key];
                                _paused = false;
                                NextKeyEvent = null;
                            };
                            break;
                        case 0x15:
                            _delayTimer = _registers[x];
                            break;
                        case 0x18:
                            _soundTimer = _registers[x];
                            break;
                        case 0x1E:
                            _i += _registers[x];
                            break;
                        case 0x29:
                            _i = _registers[x] * 5;
                            break;
                        case 0x33:
                            // Gets the 100th digit and saves it in _i
                            _memory[_i] = Convert.ToInt64((_registers[x] / 100));
                            // Gets 10th digit and places it in _i + 1
                            _memory[_i + 1] = Convert.ToInt64((_registers[x] % 100) / 10);
                            // Gets the last digit and places it in _i + 2
                            _memory[_i + 2] = Convert.ToInt64((_registers[x] % 10));
                            break;
                        case 0x55:
                            for (long registerIndex = 0; registerIndex <= x; registerIndex++)
                                _memory[_i + registerIndex] = _registers[registerIndex];
                            break;
                        case 0x65:
                            for (long registerIndex = 0; registerIndex <= x; registerIndex++)
                                _registers[registerIndex] = _memory[_i + registerIndex];
                            break;
                    }
                    break;
                default:
                    throw new Exception("Unknown Opcode: " + opcode);
            }

        }

        private void LoadRom(string romPath)
        {
            var romFile = File.ReadAllBytes(romPath);
            // Printing rom to console
            var output = romFile.Aggregate("", (current, code) => current + code);
            Console.WriteLine(output);
            
            LoadProgramIntoMemory(romFile);
        }

        private void LoadProgramIntoMemory(byte[] program)
        {
            for (var loc = 0; loc < program.Length; loc++)
            {
                _memory[0x200 + loc] = program[loc];
            }
        }

        private void LoadSpriteIntoMemory()
        {
            // Array of hex values for each sprite.
            var sprites = new[]
            {
                0xF0, 0x90, 0x90, 0x90, 0xF0, // 0
                0x20, 0x60, 0x20, 0x20, 0x70, // 1
                0xF0, 0x10, 0xF0, 0x80, 0xF0, // 2
                0xF0, 0x10, 0xF0, 0x10, 0xF0, // 3
                0x90, 0x90, 0xF0, 0x10, 0x10, // 4
                0xF0, 0x80, 0xF0, 0x10, 0xF0, // 5
                0xF0, 0x80, 0xF0, 0x90, 0xF0, // 6
                0xF0, 0x10, 0x20, 0x40, 0x40, // 7
                0xF0, 0x90, 0xF0, 0x90, 0xF0, // 8
                0xF0, 0x90, 0xF0, 0x10, 0xF0, // 9
                0xF0, 0x90, 0xF0, 0x90, 0x90, // A
                0xE0, 0x90, 0xE0, 0x90, 0xE0, // B
                0xF0, 0x80, 0x80, 0x80, 0xF0, // C
                0xE0, 0x90, 0x90, 0x90, 0xE0, // D
                0xF0, 0x80, 0xF0, 0x80, 0xF0, // E
                0xF0, 0x80, 0xF0, 0x80, 0x80  // F
            };

            for (var i = 0; i < sprites.Length; i++)
            {
                _memory[i] = sprites[i];
            }
        }
    }
}