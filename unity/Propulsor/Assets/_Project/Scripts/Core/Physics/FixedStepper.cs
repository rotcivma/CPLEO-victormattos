using System;

namespace Propulsor.Core.Physics
{
    /// <summary>
    /// Converte deltas de quadro variáveis em passos fixos, garantindo que a
    /// simulação seja independente do framerate (teste F-05).
    /// </summary>
    public sealed class FixedStepper
    {
        public float StepSeconds { get; }
        /// <summary>Limite de passos por chamada, evita "espiral da morte" após travamentos.</summary>
        public int MaxStepsPerAdvance { get; }

        private float _accumulator;

        /// <summary>Fração (0..1) do próximo passo já acumulada — útil para interpolar a renderização.</summary>
        public float Alpha => _accumulator / StepSeconds;

        public FixedStepper(float stepSeconds = 0.02f, int maxStepsPerAdvance = 10)
        {
            if (stepSeconds <= 0f) throw new ArgumentOutOfRangeException(nameof(stepSeconds));
            StepSeconds = stepSeconds;
            MaxStepsPerAdvance = maxStepsPerAdvance;
        }

        /// <summary>Acumula <paramref name="frameDelta"/> e chama <paramref name="step"/> para cada passo fixo completo.</summary>
        /// <returns>Número de passos executados.</returns>
        public int Advance(float frameDelta, Action<float> step)
        {
            _accumulator += Math.Max(0f, frameDelta);
            int steps = 0;
            while (_accumulator >= StepSeconds && steps < MaxStepsPerAdvance)
            {
                step(StepSeconds);
                _accumulator -= StepSeconds;
                steps++;
            }
            if (steps == MaxStepsPerAdvance && _accumulator > StepSeconds)
                _accumulator = 0f; // descarta o atraso excedente
            return steps;
        }

        public void Reset() => _accumulator = 0f;
    }
}
