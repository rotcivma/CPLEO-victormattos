namespace Propulsor.Core.Gameplay
{
    /// <summary>
    /// Curva de dificuldade em função da distância (m). Valores [E]/[H] —
    /// docs/fase-1/04-obstaculos.md §4.5 e docs/fase-3/01-mapeamento-sistemas.md §4.3.
    /// </summary>
    public static class Difficulty
    {
        /// <summary>Distância em que a dificuldade atinge o máximo.</summary>
        public const float MaxAt = 3000f;
        /// <summary>Distância mínima para o primeiro drone-foguete.</summary>
        public const float FirstMissileAt = 150f;
        /// <summary>Distância a partir da qual podem ocorrer rajadas.</summary>
        public const float BurstsFrom = 1500f;

        /// <summary>Nível normalizado 0..1.</summary>
        public static float Level(float distance) => MathF2.Clamp01(distance / MaxAt);

        /// <summary>Tempo em que o aviso do drone acompanha a altura do jogador (s): 1,4 → 0,6.</summary>
        public static float MissileTrackTime(float distance) => MathF2.Lerp(1.4f, 0.6f, Level(distance));

        public static float MissileIntervalMin(float distance) => MathF2.Lerp(8f, 2.5f, Level(distance));
        public static float MissileIntervalMax(float distance) => MathF2.Lerp(12f, 5f, Level(distance));

        /// <summary>Probabilidade de o evento de drone ser uma rajada.</summary>
        public static float BurstChance(float distance) =>
            distance < BurstsFrom ? 0f : MathF2.Lerp(0f, 0.35f, (distance - BurstsFrom) / (MaxAt - BurstsFrom));

        /// <summary>Peso relativo de um trecho com dificuldade <paramref name="segmentDifficulty"/> na distância dada.</summary>
        public static float SegmentAffinity(float segmentDifficulty, float distance)
        {
            float diff = segmentDifficulty - Level(distance);
            if (diff < 0f) diff = -diff;
            float w = 1f - diff * 1.5f;
            return w < 0.05f ? 0.05f : w;
        }
    }
}
