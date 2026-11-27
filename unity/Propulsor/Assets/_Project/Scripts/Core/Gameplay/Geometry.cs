using System;

namespace Propulsor.Core.Gameplay
{
    /// <summary>Caixa alinhada aos eixos.</summary>
    public readonly struct Aabb
    {
        public readonly float MinX, MinY, MaxX, MaxY;

        public Aabb(float minX, float minY, float maxX, float maxY)
        {
            MinX = minX; MinY = minY; MaxX = maxX; MaxY = maxY;
        }

        /// <summary>Caixa de largura <paramref name="w"/> centrada em x, com a base em y.</summary>
        public static Aabb FromBase(float x, float baseY, float w, float h) =>
            new Aabb(x - w * 0.5f, baseY, x + w * 0.5f, baseY + h);

        public float CenterX => (MinX + MaxX) * 0.5f;
        public float CenterY => (MinY + MaxY) * 0.5f;

        public bool Overlaps(in Aabb o) =>
            MinX < o.MaxX && MaxX > o.MinX && MinY < o.MaxY && MaxY > o.MinY;

        /// <summary>Distância de um ponto até a caixa (0 se dentro).</summary>
        public float DistanceTo(float px, float py)
        {
            float dx = Math.Max(Math.Max(MinX - px, 0f), px - MaxX);
            float dy = Math.Max(Math.Max(MinY - py, 0f), py - MaxY);
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }
    }

    public static class Geometry
    {
        /// <summary>
        /// true se a cápsula (segmento AB com raio r) toca a caixa.
        /// Amostra o segmento em passos de no máximo 0,05 u (suficiente para hitboxes ≥ 0,3 u).
        /// </summary>
        public static bool CapsuleOverlapsAabb(float ax, float ay, float bx, float by, float radius, in Aabb box)
        {
            float len = (float)Math.Sqrt((bx - ax) * (bx - ax) + (by - ay) * (by - ay));
            int n = Math.Max(1, (int)Math.Ceiling(len / 0.05f));
            for (int i = 0; i <= n; i++)
            {
                float t = (float)i / n;
                if (box.DistanceTo(ax + (bx - ax) * t, ay + (by - ay) * t) <= radius) return true;
            }
            return false;
        }
    }
}
