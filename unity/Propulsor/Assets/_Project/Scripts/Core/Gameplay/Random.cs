using System;

namespace Propulsor.Core.Gameplay
{
    /// <summary>Fonte de aleatoriedade injetável (determinística em testes).</summary>
    public interface IRandom
    {
        /// <summary>Valor em [0, 1).</summary>
        float NextFloat();
        /// <summary>Inteiro em [min, maxExclusive).</summary>
        int Range(int min, int maxExclusive);
        /// <summary>Float em [min, max).</summary>
        float Range(float min, float max);
    }

    /// <summary>
    /// Xorshift32: pequeno, rápido e com a mesma sequência em .NET e no Mono do Unity
    /// (System.Random não garante isso entre runtimes).
    /// </summary>
    public sealed class XorShiftRandom : IRandom
    {
        private uint _state;

        public XorShiftRandom(int seed)
        {
            _state = (uint)seed;
            if (_state == 0) _state = 0x9E3779B9u;
        }

        private uint NextUInt()
        {
            uint x = _state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            _state = x;
            return x;
        }

        public float NextFloat() => (NextUInt() >> 8) * (1f / 16777216f);

        public int Range(int min, int maxExclusive)
        {
            if (maxExclusive <= min) return min;
            return min + (int)(NextUInt() % (uint)(maxExclusive - min));
        }

        public float Range(float min, float max) => min + (max - min) * NextFloat();
    }

    internal static class MathF2
    {
        public static float Clamp(float v, float min, float max) => v < min ? min : (v > max ? max : v);
        public static float Clamp01(float v) => Clamp(v, 0f, 1f);
        public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);
        public const float Deg2Rad = (float)(Math.PI / 180.0);
    }
}
