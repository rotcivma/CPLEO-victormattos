using System;

namespace Propulsor.Core.Gameplay
{
    /// <summary>Geometria e colisão dos arcos de faísca e feixes de corte.</summary>
    public static class Hazards
    {
        /// <summary>Raio do arco elétrico (meia espessura do perigo).</summary>
        public const float ZapperRadius = 0.3f;
        /// <summary>Meia espessura de um feixe de corte disparando.</summary>
        public const float LaserHalfThickness = 0.3f;

        /// <summary>Ângulo atual (graus) de um arco no instante <paramref name="time"/>.</summary>
        public static float ZapperAngle(ZapperDef z, float time) => z.AngleDeg + z.AngularSpeed * time;

        /// <summary>Extremidades do arco em coordenadas de mundo.</summary>
        public static void ZapperEndpoints(ZapperDef z, float offsetX, float time,
                                           out float ax, out float ay, out float bx, out float by)
        {
            float a = ZapperAngle(z, time) * MathF2.Deg2Rad;
            float hx = (float)Math.Cos(a) * z.Length * 0.5f;
            float hy = (float)Math.Sin(a) * z.Length * 0.5f;
            float cx = z.X + offsetX;
            ax = cx - hx; ay = z.Y - hy;
            bx = cx + hx; by = z.Y + hy;
        }

        public static bool ZapperHits(ZapperDef z, float offsetX, float time, in Aabb box)
        {
            ZapperEndpoints(z, offsetX, time, out var ax, out var ay, out var bx, out var by);
            return Geometry.CapsuleOverlapsAabb(ax, ay, bx, by, ZapperRadius, box);
        }

        /// <summary>
        /// Colisão com qualquer arco de um trecho em coordenadas locais. Com
        /// <paramref name="conservativeRotation"/>, arcos rotativos ocupam o círculo inteiro.
        /// </summary>
        public static bool ZapperHitsBox(SegmentDef seg, in Aabb box, bool conservativeRotation = true)
        {
            foreach (var z in seg.Zappers)
            {
                if (z.IsRotating && conservativeRotation)
                {
                    if (box.DistanceTo(z.X, z.Y) <= z.Length * 0.5f + ZapperRadius) return true;
                }
                else if (ZapperHits(z, 0f, 0f, box)) return true;
            }
            return false;
        }
    }
}
