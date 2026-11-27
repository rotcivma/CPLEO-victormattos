using System;
using Propulsor.Core.Audio;
using UnityEngine;

namespace Propulsor.Gameplay
{
    /// <summary>
    /// Sintetizador simples: gera todos os efeitos e músicas do jogo em código
    /// (sem arquivos de áudio). Sons e trilhas são próprios do projeto.
    /// </summary>
    public static class ProceduralSynth
    {
        public const int Rate = 44100;
        private static readonly System.Random Rng = new System.Random(1234);

        private enum Wave { Sine, Square, Triangle, Saw, Noise }

        // ---------------- efeitos ----------------

        public static AudioClip Make(Sfx s)
        {
            switch (s)
            {
                case Sfx.Coin: return Clip(s, Seq(Tone(1320, 0.05f, Wave.Square, 0.25f), Tone(1760, 0.09f, Wave.Square, 0.25f)));
                case Sfx.Token: return Clip(s, Seq(Tone(660, 0.07f, Wave.Triangle), Tone(880, 0.07f, Wave.Triangle), Tone(1320, 0.14f, Wave.Triangle)));
                case Sfx.VehicleMount: return Clip(s, Sweep(200, 800, 0.3f, Wave.Square, 0.3f));
                case Sfx.Explosion: return Clip(s, Noise(0.7f, 0.85f, 1.0f));
                case Sfx.ArmorHit: return Clip(s, Mix(Tone(300, 0.2f, Wave.Square, 0.3f), Noise(0.2f, 0.3f, 0.4f)));
                case Sfx.Warning: return Clip(s, Seq(Tone(880, 0.1f, Wave.Square, 0.25f), Silence(0.05f), Tone(880, 0.1f, Wave.Square, 0.25f)));
                case Sfx.MissileWhoosh: return Clip(s, Noise(0.45f, 0.2f, 0.5f, rising: true));
                case Sfx.LaserCharge: return Clip(s, Sweep(300, 1200, 1.0f, Wave.Sine, 0.35f, tremolo: 18f));
                case Sfx.Hit: return Clip(s, Mix(Sweep(160, 60, 0.3f, Wave.Sine, 0.8f), Noise(0.25f, 0.6f, 0.5f)));
                case Sfx.Land: return Clip(s, Noise(0.06f, 0.9f, 0.25f));
                case Sfx.Bonk: return Clip(s, Sweep(520, 240, 0.12f, Wave.Square, 0.3f));
                case Sfx.NearMiss: return Clip(s, Sweep(600, 1800, 0.15f, Wave.Sine, 0.35f));
                case Sfx.Mission: return Clip(s, Seq(Tone(784, 0.1f, Wave.Triangle), Tone(988, 0.1f, Wave.Triangle), Tone(1175, 0.25f, Wave.Triangle)));
                case Sfx.RankUp: return Clip(s, Seq(Tone(523, 0.12f, Wave.Square, 0.3f), Tone(659, 0.12f, Wave.Square, 0.3f),
                                                   Tone(784, 0.12f, Wave.Square, 0.3f), Tone(1047, 0.35f, Wave.Square, 0.3f)));
                case Sfx.Achievement: return Clip(s, Seq(Tone(1047, 0.1f, Wave.Triangle), Tone(1319, 0.1f, Wave.Triangle), Tone(1568, 0.3f, Wave.Triangle)));
                case Sfx.Purchase: return Clip(s, Seq(Tone(1200, 0.06f, Wave.Square, 0.25f), Tone(1600, 0.14f, Wave.Square, 0.25f)));
                case Sfx.Boost: return Clip(s, Sweep(200, 1500, 0.5f, Wave.Saw, 0.3f));
                case Sfx.Revive: return Clip(s, Seq(Tone(523, 0.1f, Wave.Sine), Tone(784, 0.1f, Wave.Sine), Tone(1047, 0.4f, Wave.Sine)));
                case Sfx.UiClick: return Clip(s, Tone(1000, 0.03f, Wave.Square, 0.2f));
                case Sfx.ClawWin: return Clip(s, Seq(Tone(659, 0.08f, Wave.Square, 0.3f), Tone(880, 0.08f, Wave.Square, 0.3f), Tone(1319, 0.25f, Wave.Square, 0.3f)));
                case Sfx.ClawLose: return Clip(s, Seq(Tone(392, 0.15f, Wave.Triangle), Tone(262, 0.3f, Wave.Triangle)));
                default: return Clip(s, Tone(440, 0.1f, Wave.Sine));
            }
        }

        /// <summary>Chiado contínuo do propulsor (ruído filtrado, emenda sem estalos).</summary>
        public static AudioClip JetLoop()
        {
            int n = Rate; // 1 s
            var d = new float[n];
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                lp += ((float)Rng.NextDouble() * 2f - 1f - lp) * 0.08f;
                d[i] = lp * 0.9f;
            }
            // cruzamento suave nas pontas para o loop
            int fade = Rate / 20;
            for (int i = 0; i < fade; i++)
            {
                float k = (float)i / fade;
                d[i] = d[i] * k + d[n - fade + i] * (1f - k);
            }
            Array.Resize(ref d, n - fade);
            return Clip("Jato", d);
        }

        // ---------------- músicas ----------------

        /// <summary>Trilhas originais e curtas em loop (acordes, baixo, arpejo e chimbal sintetizados).</summary>
        public static AudioClip MakeMusic(Music m)
        {
            switch (m)
            {
                // Menu: calmo, 100 BPM, Lá menor – Fá – Dó – Sol.
                case Music.Menu: return Song("Menu", 100, new[] { 57, 53, 48, 55 }, new[] { false, false, false, false },
                                             Wave.Triangle, new[] { 0, 2, 1, 2, 3, 2, 1, 2 }, hats: false, gain: 0.5f);
                // Corrida: 140 BPM, Mi menor – Dó – Sol – Ré, com chimbal.
                case Music.Run: return Song("Corrida", 140, new[] { 52, 48, 55, 50 }, new[] { true, false, false, false },
                                            Wave.Square, new[] { 0, 1, 2, 3, 2, 1, 2, 3 }, hats: true, gain: 0.35f);
                // Garra: saltitante, 120 BPM, Dó – Fá – Sol – Dó.
                case Music.Claw: return Song("Garra", 120, new[] { 48, 53, 55, 48 }, new[] { false, false, false, false },
                                             Wave.Square, new[] { 0, 2, 3, 2, 1, 3, 2, 3 }, hats: true, gain: 0.3f);
                default: return null;
            }
        }

        /// <param name="roots">Notas MIDI das fundamentais (1 compasso cada).</param>
        /// <param name="minor">Se o acorde do compasso é menor.</param>
        /// <param name="pattern">Índices dos tons do acorde tocados em colcheias (0..3).</param>
        private static AudioClip Song(string name, int bpm, int[] roots, bool[] minor, Wave arpWave, int[] pattern,
                                      bool hats, float gain)
        {
            float beat = 60f / bpm;
            float eighth = beat / 2f;
            int barSamples = (int)(beat * 4 * Rate);
            var d = new float[barSamples * roots.Length];

            for (int bar = 0; bar < roots.Length; bar++)
            {
                int root = roots[bar];
                // Primeira, terça (maior/menor), quinta e oitava.
                int[] chord = { root, root + (IsMinor(root, minor[bar], bar, roots) ? 3 : 4), root + 7, root + 12 };
                int offset = bar * barSamples;

                // Baixo: semínimas na fundamental, uma oitava abaixo.
                for (int b = 0; b < 4; b++)
                    Add(d, offset + (int)(b * beat * Rate), Tone(Midi(root - 12), beat * 0.9f, Wave.Triangle, 0.5f * gain * 2f));

                // Arpejo: colcheias segundo o padrão, uma oitava acima.
                for (int e = 0; e < 8; e++)
                    Add(d, offset + (int)(e * eighth * Rate), Tone(Midi(chord[pattern[e]] + 12), eighth * 0.8f, arpWave, 0.18f * gain * 2f));

                // Chimbal nos contratempos.
                if (hats)
                    for (int e = 1; e < 8; e += 2)
                        Add(d, offset + (int)(e * eighth * Rate), Noise(0.04f, 0.0f, 0.12f * gain * 2f));
            }
            return Clip(name, d);
        }

        private static bool IsMinor(int root, bool flagged, int bar, int[] roots) =>
            flagged || (bar == 0 && (root % 12 == 9 || root % 12 == 4)); // Lá e Mi da 1ª barra: menores

        // ---------------- blocos ----------------

        private static float Midi(int note) => 440f * Mathf.Pow(2f, (note - 69) / 12f);

        private static float[] Tone(float freq, float seconds, Wave w, float vol = 0.4f)
        {
            int n = Mathf.Max(1, (int)(seconds * Rate));
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                d[i] = Osc(w, freq * t) * vol * Env(i, n);
            }
            return d;
        }

        private static float[] Sweep(float f0, float f1, float seconds, Wave w, float vol, float tremolo = 0f)
        {
            int n = (int)(seconds * Rate);
            var d = new float[n];
            double phase = 0;
            for (int i = 0; i < n; i++)
            {
                float k = (float)i / n;
                float f = Mathf.Lerp(f0, f1, k);
                phase += f / Rate;
                float trem = tremolo > 0f ? 0.7f + 0.3f * Mathf.Sin(2f * Mathf.PI * tremolo * i / Rate) : 1f;
                d[i] = Osc(w, (float)phase) * vol * Env(i, n) * trem;
            }
            return d;
        }

        /// <param name="lowpass">0 = chiado agudo, 1 = estrondo grave.</param>
        private static float[] Noise(float seconds, float lowpass, float vol, bool rising = false)
        {
            int n = (int)(seconds * Rate);
            var d = new float[n];
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float k = (float)i / n;
                float a = Mathf.Lerp(1f, 0.02f, rising ? 1f - k : lowpass);
                lp += ((float)Rng.NextDouble() * 2f - 1f - lp) * a;
                float env = rising ? Mathf.Sin(k * Mathf.PI) : Mathf.Pow(1f - k, 2f);
                d[i] = lp * vol * env * (lowpass > 0.5f ? 2.5f : 1f);
            }
            return d;
        }

        private static float[] Silence(float seconds) => new float[(int)(seconds * Rate)];

        private static float Osc(Wave w, float phase)
        {
            float p = phase - Mathf.Floor(phase);
            switch (w)
            {
                case Wave.Square: return p < 0.5f ? 1f : -1f;
                case Wave.Triangle: return 4f * Mathf.Abs(p - 0.5f) - 1f;
                case Wave.Saw: return 2f * p - 1f;
                case Wave.Noise: return (float)Rng.NextDouble() * 2f - 1f;
                default: return Mathf.Sin(2f * Mathf.PI * p);
            }
        }

        /// <summary>Envelope curto: ataque de 5 ms e decaimento exponencial.</summary>
        private static float Env(int i, int n)
        {
            int attack = Mathf.Min(n / 4, Rate / 200);
            if (i < attack) return (float)i / attack;
            float k = (float)(i - attack) / Mathf.Max(1, n - attack);
            return Mathf.Pow(1f - k, 1.6f);
        }

        private static float[] Seq(params float[][] parts)
        {
            int n = 0;
            foreach (var p in parts) n += p.Length;
            var d = new float[n];
            int o = 0;
            foreach (var p in parts) { Array.Copy(p, 0, d, o, p.Length); o += p.Length; }
            return d;
        }

        private static float[] Mix(float[] a, float[] b)
        {
            var d = new float[Math.Max(a.Length, b.Length)];
            for (int i = 0; i < d.Length; i++) d[i] = (i < a.Length ? a[i] : 0f) + (i < b.Length ? b[i] : 0f);
            return d;
        }

        private static void Add(float[] dst, int offset, float[] src)
        {
            for (int i = 0; i < src.Length && offset + i < dst.Length; i++) dst[offset + i] += src[i];
        }

        private static AudioClip Clip(Sfx s, float[] data) => Clip(s.ToString(), data);

        private static AudioClip Clip(string name, float[] data)
        {
            for (int i = 0; i < data.Length; i++) data[i] = Mathf.Clamp(data[i], -1f, 1f);
            var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
