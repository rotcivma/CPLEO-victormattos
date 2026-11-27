using System;
using System.Collections.Generic;
using Propulsor.Core.Events;
using Propulsor.Core.Gameplay.Vehicles;
using Propulsor.Core.Physics;

namespace Propulsor.Core.Gameplay
{
    /// <summary>Configuração de uma corrida (vem dos Defs e, na Fase 8, dos gadgets/inventário).</summary>
    public sealed class RunConfig
    {
        public PlayerPhysicsParams Physics = new PlayerPhysicsParams();
        public IReadOnlyList<SegmentDef> Segments = SegmentLibrary.CreateDefault();
        public VehicleKind[] UnlockedVehicles = VehicleFactory.All;
        /// <summary>Raio do ímã de moedas (0 = sem ímã).</summary>
        public float MagnetRadius = 0f;
        public float MagnetSpeed = 25f;
        public int CoinValue = 1;
        /// <summary>Moedas dadas por um token de veículo coletado já montado [H].</summary>
        public int VehicleTokenWhileMountedCoins = 10;
        public float VehicleTokenMin = 250f, VehicleTokenMax = 450f;
        public float TokenMin = 300f, TokenMax = 600f;
        public bool MissilesEnabled = true;
        /// <summary>Trecho inicial vazio (u).</summary>
        public float StartBuffer = 20f;
        public float SpawnAhead = 60f;
        public float CullBehind = 25f;
        public float InvulnerableAfterVehicle = 1.0f;
        public float InvulnerableAfterRevive = 2.0f;
        public float InvulnerableAfterArmorHit = 0.5f;
        public float EjectSpeed = 8f;
        public float PickupRadius = 0.35f;

        // ---- efeitos de gadgets / bônus (Fase 8) ----
        /// <summary>Quique ao pousar (velocidade vertical; 0 = sem botas de mola).</summary>
        public float LandingBounceSpeed = 0f;
        /// <summary>Alcance do robô coletor à frente (u; 0 = sem ajudante). Pega porcas abaixo de 1,6 u.</summary>
        public float HelperRange = 0f;
        /// <summary>Repassado à Garra da Sorte.</summary>
        public bool LuckyLastToken = false;
        /// <summary>Arrancada automática no início (m; 0 = nenhuma).</summary>
        public float HeadStartMeters = 0f;
        public float HeadStartSpeedMultiplier = 4f;
        public float HeadStartCruiseY = 3f;
    }

    /// <summary>Trecho posicionado no mundo.</summary>
    public sealed class PlacedSegment
    {
        public int Id;
        public SegmentDef Def;
        public float StartX;
        public bool LaserTriggered;
        public float EndX => StartX + Def.Width;
    }

    public sealed class Pickup
    {
        public int Id;
        public PickupKind Kind;
        public float X, Y;
        public bool Collected;
    }

    /// <summary>
    /// Simulação completa de uma corrida, sem Unity: movimento, veículos, geração do
    /// corredor, perigos, coletáveis e colisões. Publica eventos no <see cref="EventBus"/>.
    /// A camada Unity apenas lê o estado e desenha.
    /// </summary>
    public sealed class RunSimulation
    {
        public RunConfig Config { get; }
        public PlayerMotor Motor { get; }
        public EventBus Bus { get; }
        public int Seed { get; }
        public float Time { get; private set; }
        public IVehicleMotor Vehicle { get; private set; }
        public LaserSequence ActiveLaser { get; private set; }
        public int RunCoins { get; private set; }
        public int RunTokens { get; private set; }
        public float InvulnerableTime { get; private set; }
        public HazardType? DeathCause { get; private set; }
        /// <summary>Metros restantes de arrancada (0 = nenhuma ativa).</summary>
        public float HeadStartRemaining { get; private set; }
        public bool IsHeadStarting => HeadStartRemaining > 0f;

        public IReadOnlyList<PlacedSegment> Segments => _segments;
        public IReadOnlyList<Pickup> Pickups => _pickups;
        public IReadOnlyList<Missile> Missiles => _missiles.Missiles;

        public float Distance => Motor.Distance;
        public bool IsInvulnerable => InvulnerableTime > 0f;
        public bool IsDead => Motor.IsDead;
        public bool IsOver => Motor.State == MotorState.DeadStopped;
        /// <summary>Altura da base do que está na tela (veículo ou personagem).</summary>
        public float PlayerY => Vehicle?.Y ?? Motor.Y;
        public Aabb PlayerBox => Vehicle != null ? Vehicle.Hitbox(Motor.X) : PathValidator.PlayerHitbox(Motor.X, Motor.Y);

        private readonly IRandom _rng;
        private readonly SegmentSelector _selector;
        private readonly MissileDirector _missiles;
        private readonly List<PlacedSegment> _segments = new List<PlacedSegment>();
        private readonly List<Pickup> _pickups = new List<Pickup>();
        private float _nextSegmentX;
        private float _nextVehicleTokenAt, _nextTokenAt;
        private int _nextId = 1;
        private float _lastMeters = -1f;
        private bool _landedThisStep;

        public RunSimulation(RunConfig config, int seed, EventBus bus = null)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Bus = bus ?? new EventBus();
            Seed = seed;
            _rng = new XorShiftRandom(seed);
            Motor = new PlayerMotor(config.Physics);
            _selector = new SegmentSelector(config.Segments, _rng);
            _missiles = new MissileDirector(_rng, config.Physics.FloorY, config.Physics.CeilingY);
            _missiles.WarningStarted = m => Bus.Publish(new MissileWarning(m.Id));
            _missiles.Launch = m => Bus.Publish(new MissileLaunched(m.Id));

            Motor.Landed += () => { _landedThisStep = true; Bus.Publish(new PlayerLanded()); };
            Motor.StateChanged += (from, to) => Bus.Publish(new PlayerStateChanged(from, to));
            Motor.Stopped += () => Bus.Publish(new PlayerStopped(Motor.Distance));

            _nextSegmentX = Motor.X + config.StartBuffer;
            _nextVehicleTokenAt = _rng.Range(config.VehicleTokenMin, config.VehicleTokenMax);
            _nextTokenAt = _rng.Range(config.TokenMin, config.TokenMax);
            StreamWorld();
            Bus.Publish(new RunStarted(seed));
            if (config.HeadStartMeters > 0f) StartHeadStart(config.HeadStartMeters);
        }

        /// <summary>
        /// Arrancada: dispara em velocidade alta, a meia altura e invulnerável, até cobrir
        /// <paramref name="meters"/>; depois fica 1 s invulnerável.
        /// </summary>
        public void StartHeadStart(float meters)
        {
            if (IsDead || meters <= 0f) return;
            HeadStartRemaining = Math.Max(HeadStartRemaining, meters);
            Bus.Publish(new HeadStartStarted(meters));
        }

        /// <param name="held">botão pressionado neste passo</param>
        /// <param name="pressed">botão foi pressionado (borda) desde o passo anterior</param>
        public void Step(float dt, bool held, bool pressed)
        {
            if (dt <= 0f) return;
            Time += dt;
            if (InvulnerableTime > 0f) InvulnerableTime = Math.Max(0f, InvulnerableTime - dt);

            float prevX = Motor.X;
            if (IsDead)
            {
                Motor.Step(dt, false);
                return;
            }

            _landedThisStep = false;
            if (IsHeadStarting)
            {
                Motor.AdvanceHorizontal(dt, Config.HeadStartSpeedMultiplier);
                float y = Motor.Y + (Config.HeadStartCruiseY - Motor.Y) * Math.Min(1f, 5f * dt);
                Motor.PlaceVertical(y, 0f);
                HeadStartRemaining = Math.Max(0f, HeadStartRemaining - (Motor.X - prevX));
                InvulnerableTime = Math.Max(InvulnerableTime, IsHeadStarting ? 0.1f : 1f);
                if (!IsHeadStarting) Bus.Publish(new HeadStartEnded());
            }
            else if (Vehicle != null)
            {
                Vehicle.Step(dt, held, pressed);
                Motor.AdvanceHorizontal(dt, Vehicle.SpeedMultiplier);
            }
            else
            {
                Motor.Step(dt, held);
                // Botas de mola: quica ao pousar (se não estiver segurando o botão).
                if (_landedThisStep && Config.LandingBounceSpeed > 0f && !held)
                    Motor.PlaceVertical(Motor.Y, Config.LandingBounceSpeed);
            }

            StreamWorld();
            UpdateLasers(dt);

            _missiles.FollowWorld(Motor.X - prevX);
            if (Config.MissilesEnabled)
            {
                bool blocked = ActiveLaser != null || IsHeadStarting || (Vehicle != null && Vehicle.BlocksMissiles);
                var box = PlayerBox;
                _missiles.Step(dt, Distance, Motor.X, box.CenterY, blocked);
            }

            CollectPickups(dt);
            CheckHazards();

            float meters = (float)Math.Floor(Distance);
            if (meters != _lastMeters)
            {
                _lastMeters = meters;
                Bus.Publish(new DistanceChanged(meters));
            }
        }

        /// <summary>Monta um veículo (usado por tokens e por testes).</summary>
        public void Mount(VehicleKind kind)
        {
            if (IsDead) return;
            var v = VehicleFactory.Create(kind);
            v.Mount(Motor.Y, Config.Physics);
            Vehicle = v;
            Bus.Publish(new VehicleMounted(kind));
        }

        /// <summary>Segunda Chance: revive no ponto da morte, invulnerável por 2 s.</summary>
        public void Revive()
        {
            if (!IsDead) return;
            Motor.Revive();
            DeathCause = null;
            InvulnerableTime = Config.InvulnerableAfterRevive;
            Bus.Publish(new PlayerRevived());
        }

        /// <summary>Mata o jogador imediatamente (teste/depuração), ignorando veículo e invulnerabilidade.</summary>
        public void Kill(HazardType cause)
        {
            if (IsDead) return;
            if (Vehicle != null)
            {
                Motor.PlaceVertical(Vehicle.Y, 0f);
                Vehicle = null;
            }
            DeathCause = cause;
            Motor.Kill();
            Bus.Publish(new PlayerDied(cause));
        }

        // ---------------- mundo ----------------

        private void StreamWorld()
        {
            float aheadLimit = Motor.X + Config.SpawnAhead;
            while (_nextSegmentX < aheadLimit)
            {
                float startDistance = _nextSegmentX - (Motor.X - Motor.Distance);
                var def = _selector.Next(startDistance);
                var placed = new PlacedSegment { Id = _nextId++, Def = def, StartX = _nextSegmentX };
                _segments.Add(placed);

                foreach (var c in def.Coins)
                    _pickups.Add(new Pickup { Id = _nextId++, Kind = PickupKind.Coin, X = placed.StartX + c.X, Y = c.Y });

                if (def.TokenSlots.Count > 0)
                {
                    if (startDistance >= _nextVehicleTokenAt && Vehicle == null && Config.UnlockedVehicles.Length > 0)
                    {
                        var s = def.TokenSlots[0];
                        _pickups.Add(new Pickup { Id = _nextId++, Kind = PickupKind.VehicleToken, X = placed.StartX + s.X, Y = s.Y });
                        _nextVehicleTokenAt = startDistance + _rng.Range(Config.VehicleTokenMin, Config.VehicleTokenMax);
                    }
                    else if (startDistance >= _nextTokenAt)
                    {
                        var s = def.TokenSlots[def.TokenSlots.Count - 1];
                        _pickups.Add(new Pickup { Id = _nextId++, Kind = PickupKind.Token, X = placed.StartX + s.X, Y = s.Y });
                        _nextTokenAt = startDistance + _rng.Range(Config.TokenMin, Config.TokenMax);
                    }
                }

                _nextSegmentX += def.Width;
            }

            float behind = Motor.X - Config.CullBehind;
            _segments.RemoveAll(s => s.EndX < behind);
            _pickups.RemoveAll(p => p.Collected || p.X < behind);
        }

        private void UpdateLasers(float dt)
        {
            if (ActiveLaser != null)
            {
                ActiveLaser.Step(dt);
                if (ActiveLaser.IsDone)
                {
                    ActiveLaser = null;
                    Bus.Publish(new LaserSequenceEnded());
                }
                return;
            }

            foreach (var s in _segments)
            {
                if (!s.Def.HasLasers || s.LaserTriggered || Motor.X < s.StartX) continue;
                s.LaserTriggered = true;
                ActiveLaser = new LaserSequence(s.Def.LaserWaves);
                Bus.Publish(new LaserSequenceStarted());
                break;
            }
        }

        private void CollectPickups(float dt)
        {
            var box = PlayerBox;
            float cx = box.CenterX, cy = box.CenterY;
            foreach (var p in _pickups)
            {
                if (p.Collected) continue;

                if (Config.MagnetRadius > 0f && p.Kind == PickupKind.Coin)
                {
                    float dx = cx - p.X, dy = cy - p.Y;
                    float d = (float)Math.Sqrt(dx * dx + dy * dy);
                    if (d < Config.MagnetRadius && d > 1e-4f)
                    {
                        float step = Math.Min(d, Config.MagnetSpeed * dt);
                        p.X += dx / d * step;
                        p.Y += dy / d * step;
                    }
                }

                bool helperGrabs = Config.HelperRange > 0f && p.Kind == PickupKind.Coin && p.Y < 1.6f &&
                                   p.X >= box.MinX && p.X <= box.MaxX + Config.HelperRange;
                if (!helperGrabs && box.DistanceTo(p.X, p.Y) > Config.PickupRadius) continue;
                p.Collected = true;
                switch (p.Kind)
                {
                    case PickupKind.Coin:
                        AddCoins(Config.CoinValue);
                        break;
                    case PickupKind.Token:
                        RunTokens++;
                        Bus.Publish(new TokenCollected(RunTokens));
                        break;
                    case PickupKind.VehicleToken:
                        if (Vehicle == null)
                            Mount(Config.UnlockedVehicles[_rng.Range(0, Config.UnlockedVehicles.Length)]);
                        else
                            AddCoins(Config.VehicleTokenWhileMountedCoins);
                        break;
                }
            }
        }

        private void AddCoins(int value)
        {
            RunCoins += value;
            Bus.Publish(new CoinCollected(value, RunCoins));
        }

        private void CheckHazards()
        {
            if (IsInvulnerable) return;
            var box = PlayerBox;

            foreach (var s in _segments)
            {
                if (s.EndX < box.MinX - 3f || s.StartX > box.MaxX + 3f) continue;
                foreach (var z in s.Def.Zappers)
                {
                    if (Hazards.ZapperHits(z, s.StartX, Time, box)) { OnHit(HazardType.Zapper, null); return; }
                }
            }

            if (ActiveLaser != null && ActiveLaser.Hits(box)) { OnHit(HazardType.Laser, null); return; }

            foreach (var m in _missiles.Missiles)
            {
                if (m.Phase == MissilePhase.Flying && m.Box.Overlaps(box)) { OnHit(HazardType.Missile, m); return; }
            }
        }

        private void OnHit(HazardType cause, Missile missile)
        {
            if (missile != null) _missiles.Remove(missile);

            if (Vehicle == null)
            {
                DeathCause = cause;
                Motor.Kill();
                Bus.Publish(new PlayerDied(cause));
                return;
            }

            if (cause == HazardType.Missile && Vehicle.HitPoints > 1)
            {
                Vehicle.HitPoints--;
                InvulnerableTime = Config.InvulnerableAfterArmorHit;
                Bus.Publish(new VehicleDamaged(Vehicle.Kind, Vehicle.HitPoints));
                return;
            }

            var kind = Vehicle.Kind;
            float y = Vehicle.Y;
            Vehicle = null;
            Motor.PlaceVertical(y, Config.EjectSpeed);
            InvulnerableTime = Config.InvulnerableAfterVehicle;
            Bus.Publish(new VehicleDestroyed(kind, cause));
        }
    }
}
