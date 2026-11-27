using System;
using Propulsor.Core.Physics;

namespace Propulsor.Core.Gameplay
{
    /// <summary>
    /// Verifica se um trecho é atravessável: existe ao menos uma trajetória, respeitando
    /// as velocidades verticais máximas (com margem), que evita todos os arcos de faísca
    /// na velocidade horizontal máxima. Arcos rotativos são tratados de forma conservadora
    /// como o círculo inteiro que varrem. Também confere margens de entrada/saída,
    /// faixas livres das ondas de feixes e se os slots de tokens estão fora dos perigos.
    /// </summary>
    public static class PathValidator
    {
        public const float EdgeMargin = 2f;
        public const float MinLaserGap = 1.6f;
        /// <summary>Fração das velocidades verticais máximas considerada alcançável (margem humana).</summary>
        public const float VerticalSpeedFactor = 0.6f;

        public readonly struct Result
        {
            public readonly bool Ok;
            public readonly string Reason;
            public Result(bool ok, string reason) { Ok = ok; Reason = reason; }
            public override string ToString() => Ok ? "OK" : Reason;
        }

        public static Result Validate(SegmentDef seg, PlayerPhysicsParams p)
        {
            foreach (var z in seg.Zappers)
            {
                float reach = z.IsRotating ? z.Length * 0.5f : Math.Abs((float)Math.Cos(z.AngleDeg * MathF2.Deg2Rad)) * z.Length * 0.5f;
                reach += Hazards.ZapperRadius;
                if (z.X - reach < EdgeMargin || z.X + reach > seg.Width - EdgeMargin)
                    return new Result(false, $"{seg.Id}: arco em x={z.X} invade a margem de entrada/saída");
            }

            foreach (var wave in seg.LaserWaves)
                if (LargestLaserGap(wave, p) < MinLaserGap)
                    return new Result(false, $"{seg.Id}: onda de feixes sem faixa livre ≥ {MinLaserGap} u");

            foreach (var slot in seg.TokenSlots)
                if (Hazards.ZapperHitsBox(seg, Aabb.FromBase(slot.X, slot.Y - 0.4f, 0.8f, 0.8f)))
                    return new Result(false, $"{seg.Id}: slot ({slot.X},{slot.Y}) dentro de um perigo");

            return Traversable(seg, p)
                ? new Result(true, null)
                : new Result(false, $"{seg.Id}: nenhuma trajetória livre");
        }

        private static float LargestLaserGap(LaserWaveDef wave, PlayerPhysicsParams p)
        {
            var rows = (float[])wave.Rows.Clone();
            Array.Sort(rows);
            float prev = p.FloorY, best = 0f;
            foreach (var r in rows)
            {
                best = Math.Max(best, (r - Hazards.LaserHalfThickness) - prev);
                prev = r + Hazards.LaserHalfThickness;
            }
            return Math.Max(best, p.CeilingY - prev);
        }

        private static bool Traversable(SegmentDef seg, PlayerPhysicsParams p)
        {
            const float dy = 0.05f;
            int cells = (int)Math.Round((p.MaxY - p.FloorY) / dy) + 1;
            float v = p.MaxSpeed;
            float riseSpeed = p.MaxRiseSpeed * VerticalSpeedFactor;
            float fallSpeed = -p.MaxFallSpeed * VerticalSpeedFactor;
            // Passo horizontal escolhido para que subir 1 célula leve exatamente 1 coluna
            // (evita que o arredondamento zere o movimento vertical). Queda arredondada para baixo: conservador.
            float dx = dy * v / riseSpeed;
            const int rise = 1;
            int fall = Math.Max(1, (int)Math.Floor(fallSpeed / riseSpeed));

            var reach = new bool[cells];
            var next = new bool[cells];
            var free = new bool[cells];
            int columns = (int)Math.Ceiling(seg.Width / dx);

            for (int c = 0; c <= columns; c++)
            {
                float x = c * dx;
                for (int j = 0; j < cells; j++)
                    free[j] = !Hazards.ZapperHitsBox(seg, PlayerHitbox(x, p.FloorY + j * dy), conservativeRotation: true);

                if (c == 0)
                {
                    Array.Copy(free, reach, cells);
                }
                else
                {
                    // Uma célula j é alcançável se alguma célula i alcançável está em [j - rise, j + fall].
                    bool any = false;
                    // janela deslizante usando soma de prefixos
                    var prefix = new int[cells + 1];
                    for (int i = 0; i < cells; i++) prefix[i + 1] = prefix[i] + (reach[i] ? 1 : 0);
                    for (int j = 0; j < cells; j++)
                    {
                        int lo = Math.Max(0, j - rise), hi = Math.Min(cells - 1, j + fall);
                        next[j] = free[j] && prefix[hi + 1] - prefix[lo] > 0;
                        any |= next[j];
                    }
                    if (!any) return false;
                    var tmp = reach; reach = next; next = tmp;
                }
            }
            return Array.IndexOf(reach, true) >= 0;
        }

        /// <summary>Hitbox do jogador a pé (menor que o sprite: tolerância a raspões).</summary>
        public static Aabb PlayerHitbox(float x, float baseY) => Aabb.FromBase(x, baseY + 0.1f, 0.5f, 0.8f);
    }
}
