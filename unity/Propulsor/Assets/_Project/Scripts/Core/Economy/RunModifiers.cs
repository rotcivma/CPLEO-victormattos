using System;
using System.Collections.Generic;
using Propulsor.Core.Gameplay;

namespace Propulsor.Core.Economy
{
    /// <summary>
    /// Combina gadgets equipados e bônus pendentes numa <see cref="RunConfig"/>.
    /// Regras de combinação: ímã/ajudante/escudo = maior valor; gravidade = multiplica; booleanos = OU.
    /// </summary>
    public static class RunModifiers
    {
        public const float HeadStartMeters = 750f;

        public static RunConfig Apply(RunConfig config, IEnumerable<ItemDef> gadgets, bool doubleCoins, bool headStart)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            config.Physics = config.Physics.Clone();

            foreach (var g in gadgets)
            {
                switch (g.Effect)
                {
                    case GadgetEffect.Magnet: config.MagnetRadius = Math.Max(config.MagnetRadius, g.EffectValue); break;
                    case GadgetEffect.Gravity: config.Physics.Gravity *= g.EffectValue; break;
                    case GadgetEffect.LandingBounce: config.LandingBounceSpeed = Math.Max(config.LandingBounceSpeed, g.EffectValue); break;
                    case GadgetEffect.Helper: config.HelperRange = Math.Max(config.HelperRange, g.EffectValue); break;
                    case GadgetEffect.LuckyLastToken: config.LuckyLastToken = true; break;
                    case GadgetEffect.LongerShield:
                        config.InvulnerableAfterVehicle = Math.Max(config.InvulnerableAfterVehicle, g.EffectValue);
                        break;
                }
            }

            if (doubleCoins) config.CoinValue *= 2;
            if (headStart) config.HeadStartMeters = Math.Max(config.HeadStartMeters, HeadStartMeters);
            return config;
        }
    }
}
