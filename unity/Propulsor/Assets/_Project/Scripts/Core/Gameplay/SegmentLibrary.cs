using System.Collections.Generic;

namespace Propulsor.Core.Gameplay
{
    /// <summary>
    /// Biblioteca inicial de trechos (layouts próprios). Corredor: y 0..7.
    /// Regra: os 2 u iniciais e finais de cada trecho ficam livres de arcos, para que
    /// qualquer altura de entrada seja válida (verificado por <see cref="PathValidator"/>).
    /// </summary>
    public static class SegmentLibrary
    {
        public static List<SegmentDef> CreateDefault()
        {
            var list = new List<SegmentDef>
            {
                // ---- Fácil: aquecimento e moedas ----
                new SegmentDef("corrida_livre", 20f, 0f) { BaseWeight = 0.6f }
                    .CoinLine(3f, 1f, 20).Slot(12f, 4f),
                new SegmentDef("arco_moedas", 20f, 0f) { BaseWeight = 0.6f }
                    .CoinArc(3f, 2f, 20, 3f).Slot(17f, 5.5f),

                // ---- Arcos de faísca estáticos ----
                new SegmentDef("faisca_baixa", 16f, 0.1f)
                    .Zap(8f, 1.5f, 3f, 90f).CoinLine(5f, 5f, 8).Slot(13.5f, 5f),
                new SegmentDef("faisca_alta", 16f, 0.1f)
                    .Zap(8f, 5.5f, 3f, 90f).CoinLine(5f, 1.2f, 8).Slot(13.5f, 1.5f),
                new SegmentDef("faisca_horizontal", 18f, 0.2f)
                    .Zap(9f, 3.5f, 4f, 0f).CoinLine(6f, 5.5f, 10).CoinLine(6f, 1.2f, 10).Slot(15.5f, 3.5f),
                new SegmentDef("portao_zigue", 20f, 0.3f, 100f)
                    .Zap(5f, 1.5f, 3f, 90f).Zap(14f, 5.5f, 3f, 90f)
                    .CoinArc(6f, 3.6f, 10, -1.5f).Slot(18f, 2f),
                new SegmentDef("diagonal_dupla", 22f, 0.4f, 200f)
                    .Zap(7f, 2f, 3.5f, 45f).Zap(15f, 5f, 3.5f, 45f)
                    .CoinLine(4f, 5.5f, 6).CoinLine(12f, 1.5f, 6).Slot(19.5f, 3.5f),
                new SegmentDef("escada", 24f, 0.5f, 300f)
                    .Zap(6f, 1.5f, 3f, 90f).Zap(12f, 4f, 3f, 0f).Zap(18f, 5.5f, 3f, 90f)
                    .CoinLine(4f, 4.5f, 4).CoinLine(13f, 1.5f, 10).Slot(21.5f, 1.5f),
                new SegmentDef("tunel", 24f, 0.6f, 500f)
                    .Zap(8f, 1.2f, 4f, 0f).Zap(8f, 5.8f, 4f, 0f)
                    .Zap(16f, 2.2f, 4f, 0f).Zap(16f, 6.2f, 4f, 0f)
                    .CoinLine(5f, 3.6f, 22).Slot(21.5f, 3.5f),

                // ---- Arcos rotativos ----
                new SegmentDef("giratorio", 20f, 0.5f, 300f)
                    .Zap(10f, 3.5f, 3f, 0f, 90f).CoinLine(6f, 6.2f, 8).Slot(16.5f, 0.5f),
                new SegmentDef("giratorio_duplo", 26f, 0.7f, 800f)
                    .Zap(8f, 2f, 2.4f, 0f, 120f).Zap(18f, 5f, 2.4f, 90f, -120f)
                    .CoinLine(5f, 5f, 6).CoinLine(15f, 1.2f, 6).Slot(23.5f, 3.5f),

                // ---- Feixes de corte (sequência presa à tela) ----
                new SegmentDef("feixes_simples", 40f, 0.3f, 200f) { BaseWeight = 0.7f }
                    .Lasers(1.0f, 6.0f).Lasers(3.5f).CoinLine(4f, 3.5f, 30).Slot(36f, 3.5f),
                new SegmentDef("feixes_alternados", 50f, 0.6f, 900f) { BaseWeight = 0.7f }
                    .Lasers(0.6f, 2.2f, 3.8f).Lasers(3.2f, 4.8f, 6.4f).Lasers(1.0f, 6.0f)
                    .CoinLine(4f, 5.5f, 20).Slot(46f, 3.5f),

                // ---- Difícil ----
                new SegmentDef("denso", 30f, 0.85f, 1500f)
                    .Zap(6f, 1.5f, 3f, 90f).Zap(11f, 5.5f, 3f, 90f)
                    .Zap(17.5f, 2f, 3f, 0f, 100f).Zap(25f, 5.5f, 3f, 90f)
                    .CoinLine(8f, 1.2f, 4).CoinLine(15f, 5.5f, 5).Slot(27.5f, 1.5f),
            };
            return list;
        }
    }
}
