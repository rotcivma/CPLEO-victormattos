using System.Collections.Generic;

namespace Propulsor.Core.Gameplay
{
    public enum HazardType { Zapper, Laser, Missile }

    public enum PickupKind { Coin, Token, VehicleToken }

    /// <summary>Arco de faísca: segmento entre duas bobinas, centrado em (X, Y) local ao segmento.</summary>
    public sealed class ZapperDef
    {
        public float X, Y;
        public float Length = 3f;
        /// <summary>Ângulo inicial em graus (0 = horizontal, 90 = vertical).</summary>
        public float AngleDeg;
        /// <summary>Velocidade angular em graus/s (0 = estático).</summary>
        public float AngularSpeed;

        public bool IsRotating => AngularSpeed != 0f;

        public ZapperDef(float x, float y, float length, float angleDeg, float angularSpeed = 0f)
        {
            X = x; Y = y; Length = length; AngleDeg = angleDeg; AngularSpeed = angularSpeed;
        }
    }

    /// <summary>Uma "onda" de feixes de corte: linhas horizontais (alturas dos centros).</summary>
    public sealed class LaserWaveDef
    {
        public float[] Rows;
        public LaserWaveDef(params float[] rows) { Rows = rows; }
    }

    /// <summary>Ponto de coleta local ao segmento.</summary>
    public readonly struct PickupSpot
    {
        public readonly float X, Y;
        public PickupSpot(float x, float y) { X = x; Y = y; }
    }

    /// <summary>
    /// Trecho pré-desenhado do corredor (coordenadas locais: x a partir do início do trecho,
    /// y a partir do chão). Ver docs/fase-3/01-mapeamento-sistemas.md §5.
    /// </summary>
    public sealed class SegmentDef
    {
        public string Id;
        public float Width;
        /// <summary>Dificuldade 0..1, comparada à curva de dificuldade da distância.</summary>
        public float Difficulty;
        public float BaseWeight = 1f;
        /// <summary>Distância mínima (m) para o trecho poder aparecer.</summary>
        public float MinDistance;

        public readonly List<ZapperDef> Zappers = new List<ZapperDef>();
        /// <summary>Ondas de feixes (sequência presa à tela, disparada ao entrar no trecho).</summary>
        public readonly List<LaserWaveDef> LaserWaves = new List<LaserWaveDef>();
        public readonly List<PickupSpot> Coins = new List<PickupSpot>();
        /// <summary>Locais livres onde podem surgir fichas ou tokens de veículo.</summary>
        public readonly List<PickupSpot> TokenSlots = new List<PickupSpot>();

        public bool HasLasers => LaserWaves.Count > 0;

        public SegmentDef(string id, float width, float difficulty, float minDistance = 0f)
        {
            Id = id; Width = width; Difficulty = difficulty; MinDistance = minDistance;
        }

        // ---- construtores fluentes de conteúdo ----
        public SegmentDef Zap(float x, float y, float length, float angleDeg, float angularSpeed = 0f)
        {
            Zappers.Add(new ZapperDef(x, y, length, angleDeg, angularSpeed));
            return this;
        }

        public SegmentDef Lasers(params float[] rows)
        {
            LaserWaves.Add(new LaserWaveDef(rows));
            return this;
        }

        public SegmentDef CoinLine(float x0, float y, int count, float spacing = 0.6f)
        {
            for (int i = 0; i < count; i++) Coins.Add(new PickupSpot(x0 + i * spacing, y));
            return this;
        }

        /// <summary>Arco de moedas (meia senoide) começando em x0 na altura y, com amplitude a.</summary>
        public SegmentDef CoinArc(float x0, float y, int count, float amplitude, float spacing = 0.6f)
        {
            for (int i = 0; i < count; i++)
            {
                float t = count == 1 ? 0.5f : (float)i / (count - 1);
                Coins.Add(new PickupSpot(x0 + i * spacing, y + amplitude * (float)System.Math.Sin(t * System.Math.PI)));
            }
            return this;
        }

        public SegmentDef CoinBlock(float x0, float y0, int cols, int rows, float spacing = 0.6f)
        {
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    Coins.Add(new PickupSpot(x0 + c * spacing, y0 + r * spacing));
            return this;
        }

        public SegmentDef Slot(float x, float y)
        {
            TokenSlots.Add(new PickupSpot(x, y));
            return this;
        }
    }
}
