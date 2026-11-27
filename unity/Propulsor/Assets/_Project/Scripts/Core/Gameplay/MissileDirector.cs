using System.Collections.Generic;

namespace Propulsor.Core.Gameplay
{
    public enum MissilePhase { Waiting, Tracking, Locked, Flying, Gone }

    /// <summary>Drone-foguete: aviso que segue a altura do jogador → trava → voa da direita para a esquerda.</summary>
    public sealed class Missile
    {
        public int Id;
        public MissilePhase Phase;
        /// <summary>Altura do centro do aviso/drone.</summary>
        public float Y;
        /// <summary>Posição X no mundo (válida em <see cref="MissilePhase.Flying"/>).</summary>
        public float X;
        /// <summary>Deslocamento vertical em relação ao jogador (rajadas em leque).</summary>
        public float Offset;
        public float Timer;
        public float Delay;
        public float TrackTime;
        /// <summary>Já avaliado para "raspão" ao passar pela personagem.</summary>
        public bool NearChecked;

        public const float Width = 0.9f, Height = 0.35f;
        public Aabb Box => new Aabb(X - Width * 0.5f, Y - Height * 0.5f, X + Width * 0.5f, Y + Height * 0.5f);
        public bool IsWarning => Phase == MissilePhase.Tracking || Phase == MissilePhase.Locked;
    }

    /// <summary>
    /// Agenda e conduz os drones-foguete (docs/fase-3/01 §4.3). Não lança enquanto
    /// bloqueado (feixes ativos ou veículo que bloqueia drones); avisos pendentes são cancelados.
    /// </summary>
    public sealed class MissileDirector
    {
        public const float LockTime = 0.4f;
        public const float TrackLerp = 6f;
        public const float RelativeSpeed = 30f;
        public const float SpawnAhead = 16f;
        public const float DespawnBehind = 12f;
        public const float BurstSpacing = 0.25f;
        public const float BurstFan = 1.2f;

        private readonly IRandom _rng;
        private readonly List<Missile> _missiles = new List<Missile>();
        private float _timer = -1f;
        private int _nextId = 1;
        private readonly float _floor, _ceiling;

        public IReadOnlyList<Missile> Missiles => _missiles;
        /// <summary>Mesma lista, tipada (iterar sem alocar).</summary>
        internal List<Missile> List => _missiles;
        private readonly Stack<Missile> _pool = new Stack<Missile>();
        public int Launched { get; private set; }

        public System.Action<Missile> WarningStarted;
        public System.Action<Missile> Launch;

        public MissileDirector(IRandom rng, float floorY, float ceilingY)
        {
            _rng = rng;
            _floor = floorY;
            _ceiling = ceilingY;
        }

        public void Step(float dt, float distance, float playerX, float playerCenterY, bool blocked)
        {
            if (blocked) CancelPending();

            // Agendamento.
            if (distance >= Difficulty.FirstMissileAt)
            {
                if (_timer < 0f) _timer = NextInterval(distance);
                if (!blocked) _timer -= dt;
                if (_timer <= 0f && !blocked && !HasPending())
                {
                    SpawnEvent(distance, playerCenterY);
                    _timer = NextInterval(distance);
                }
            }

            for (int i = _missiles.Count - 1; i >= 0; i--)
            {
                var m = _missiles[i];
                m.Timer += dt;
                switch (m.Phase)
                {
                    case MissilePhase.Waiting:
                        if (m.Timer >= m.Delay) { m.Phase = MissilePhase.Tracking; m.Timer = 0f; WarningStarted?.Invoke(m); }
                        break;
                    case MissilePhase.Tracking:
                        float target = Clamp(playerCenterY + m.Offset);
                        m.Y += (target - m.Y) * System.Math.Min(1f, TrackLerp * dt);
                        if (m.Timer >= m.TrackTime) { m.Phase = MissilePhase.Locked; m.Timer = 0f; }
                        break;
                    case MissilePhase.Locked:
                        if (m.Timer >= LockTime)
                        {
                            m.Phase = MissilePhase.Flying;
                            m.X = playerX + SpawnAhead;
                            Launched++;
                            Launch?.Invoke(m);
                        }
                        break;
                    case MissilePhase.Flying:
                        m.X -= RelativeSpeed * dt; // velocidade relativa ao jogador/câmera
                        if (m.X < playerX - DespawnBehind) m.Phase = MissilePhase.Gone;
                        break;
                }
                if (m.Phase == MissilePhase.Gone) { _pool.Push(m); _missiles.RemoveAt(i); }
            }
        }

        /// <summary>Avança os drones em voo junto com o mundo (chamar com o deslocamento do jogador no passo).</summary>
        public void FollowWorld(float playerDeltaX)
        {
            foreach (var m in _missiles)
                if (m.Phase == MissilePhase.Flying) m.X += playerDeltaX;
        }

        public void Remove(Missile m) => m.Phase = MissilePhase.Gone;

        private void CancelPending()
        {
            for (int i = _missiles.Count - 1; i >= 0; i--)
                if (_missiles[i].Phase != MissilePhase.Flying) { _pool.Push(_missiles[i]); _missiles.RemoveAt(i); }
        }

        private bool HasPending()
        {
            foreach (var m in _missiles) if (m.Phase != MissilePhase.Flying) return true;
            return false;
        }

        private void SpawnEvent(float distance, float playerCenterY)
        {
            int count = _rng.NextFloat() < Difficulty.BurstChance(distance) ? _rng.Range(3, 7) : 1;
            float track = Difficulty.MissileTrackTime(distance);
            for (int i = 0; i < count; i++)
            {
                float offset = count == 1 ? 0f : (i - (count - 1) * 0.5f) * BurstFan;
                var m = _pool.Count > 0 ? _pool.Pop() : new Missile();
                m.Id = _nextId++;
                m.Phase = MissilePhase.Waiting;
                m.Offset = offset;
                m.Y = Clamp(playerCenterY + offset);
                m.X = 0f;
                m.Timer = 0f;
                m.Delay = i * BurstSpacing;
                m.TrackTime = track;
                m.NearChecked = false;
                _missiles.Add(m);
            }
        }

        private float NextInterval(float d) => _rng.Range(Difficulty.MissileIntervalMin(d), Difficulty.MissileIntervalMax(d));

        private float Clamp(float y) => MathF2.Clamp(y, _floor + 0.4f, _ceiling - 0.4f);
    }
}
