using Propulsor.Core.Physics;
using UnityEngine;

namespace Propulsor.Data
{
    /// <summary>
    /// Asset editável com os parâmetros de física do jogador.
    /// Criar via: Assets ▸ Create ▸ Propulsor ▸ Player Physics.
    /// Valores padrão = estimativas [E] da documentação.
    /// </summary>
    [CreateAssetMenu(menuName = "Propulsor/Player Physics", fileName = "PlayerPhysics")]
    public sealed class PlayerPhysicsDef : ScriptableObject
    {
        [Header("Vertical")]
        public float gravity = -38f;
        public float thrustAcceleration = 70f;
        public float maxRiseSpeed = 11f;
        public float maxFallSpeed = -14f;

        [Header("Horizontal")]
        public float startSpeed = 8f;
        public float horizontalAcceleration = 0.05f;
        public float maxSpeed = 20f;

        [Header("Corredor")]
        public float floorY = 0f;
        public float ceilingY = 7f;
        public float playerHeight = 1f;

        [Header("Morte")]
        public float deathPopSpeed = 5f;
        [Range(0f, 1f)] public float deathRestitution = 0.4f;
        public float deathFriction = 0.3f;
        public float deathStopSpeed = 0.5f;
        public float minBounceSpeed = 1f;

        public PlayerPhysicsParams ToParams() => CopyTo(new PlayerPhysicsParams());

        /// <summary>Copia os valores para uma instância existente (permite ajuste ao vivo no Inspector).</summary>
        public PlayerPhysicsParams CopyTo(PlayerPhysicsParams p)
        {
            p.Gravity = gravity;
            p.ThrustAcceleration = thrustAcceleration;
            p.MaxRiseSpeed = maxRiseSpeed;
            p.MaxFallSpeed = maxFallSpeed;
            p.StartSpeed = startSpeed;
            p.HorizontalAcceleration = horizontalAcceleration;
            p.MaxSpeed = maxSpeed;
            p.FloorY = floorY;
            p.CeilingY = ceilingY;
            p.PlayerHeight = playerHeight;
            p.DeathPopSpeed = deathPopSpeed;
            p.DeathRestitution = deathRestitution;
            p.DeathFriction = deathFriction;
            p.DeathStopSpeed = deathStopSpeed;
            p.MinBounceSpeed = minBounceSpeed;
            return p;
        }
    }
}
