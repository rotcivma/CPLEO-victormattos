namespace Propulsor.Core.Physics
{
    /// <summary>
    /// Parâmetros de física do jogador. Unidades: 1 u = 1 m = altura do personagem.
    /// Valores padrão são ESTIMATIVAS [E] (docs/fase-1/03-gameplay-fisica.md §4),
    /// a calibrar por comparação com vídeo de referência.
    /// </summary>
    public sealed class PlayerPhysicsParams
    {
        /// <summary>Aceleração da gravidade (u/s², negativa).</summary>
        public float Gravity = -38f;
        /// <summary>Aceleração do propulsor somada à gravidade enquanto o input está pressionado (u/s²).</summary>
        public float ThrustAcceleration = 70f;
        /// <summary>Velocidade vertical máxima subindo (u/s).</summary>
        public float MaxRiseSpeed = 11f;
        /// <summary>Velocidade vertical máxima caindo (u/s, negativa).</summary>
        public float MaxFallSpeed = -14f;

        /// <summary>Velocidade horizontal no início da corrida (u/s).</summary>
        public float StartSpeed = 8f;
        /// <summary>Aceleração horizontal contínua (u/s²): 8 → 20 u/s em ~90 s (v2, antes 0,05 = 4 min).</summary>
        public float HorizontalAcceleration = 0.13f;
        /// <summary>Velocidade horizontal máxima (u/s).</summary>
        public float MaxSpeed = 20f;

        /// <summary>Altura do chão (base do corredor).</summary>
        public float FloorY = 0f;
        /// <summary>Altura do teto (topo do corredor).</summary>
        public float CeilingY = 7f;
        /// <summary>Altura do personagem; a posição Y é a base (pés).</summary>
        public float PlayerHeight = 1f;

        /// <summary>Impulso vertical aplicado no momento da morte (u/s).</summary>
        public float DeathPopSpeed = 5f;
        /// <summary>Restituição dos quiques do corpo após a morte (0..1).</summary>
        public float DeathRestitution = 0.4f;
        /// <summary>Coeficiente de atrito ao deslizar no chão após a morte (desaceleração = atrito × |g|).</summary>
        public float DeathFriction = 0.3f;
        /// <summary>Abaixo desta velocidade horizontal o corpo é considerado parado (u/s).</summary>
        public float DeathStopSpeed = 0.5f;
        /// <summary>Velocidade vertical mínima para quicar; abaixo disso assenta no chão (u/s).</summary>
        public float MinBounceSpeed = 1f;

        /// <summary>Limite superior para a posição Y (base) do jogador.</summary>
        public float MaxY => CeilingY - PlayerHeight;

        public PlayerPhysicsParams Clone() => (PlayerPhysicsParams)MemberwiseClone();
    }
}
