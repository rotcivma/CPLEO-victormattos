using System.Collections.Generic;
using Propulsor.Core.Flow;
using Propulsor.Core.Gameplay.Vehicles;

namespace Propulsor.Core.Economy
{
    public enum ItemCategory { Jetpack, VehicleSkin, Gadget, Consumable, Outfit, Hat, Upgrade }

    /// <summary>Efeitos de gadgets sobre a corrida (combinados em <see cref="RunModifiers"/>).</summary>
    public enum GadgetEffect
    {
        None,
        /// <summary>Ímã de porcas (raio em u).</summary>
        Magnet,
        /// <summary>Multiplica a gravidade (queda mais rápida).</summary>
        Gravity,
        /// <summary>Quica ao tocar o chão (velocidade vertical do quique).</summary>
        LandingBounce,
        /// <summary>Robô ajudante recolhe porcas baixas à frente (alcance em u).</summary>
        Helper,
        /// <summary>A última ficha da Garra sempre ganha.</summary>
        LuckyLastToken,
        /// <summary>Segundos de invulnerabilidade ao perder um veículo.</summary>
        LongerShield
    }

    /// <summary>Item da Oficina. Nomes, efeitos e preços são próprios do projeto [E].</summary>
    public sealed class ItemDef
    {
        public string Id;
        public string Name;
        public string Description;
        public ItemCategory Category;
        public int Price;
        /// <summary>Espaço de equipamento (jetpack, outfit, hat, skin:&lt;veículo&gt;) ou null.</summary>
        public string Slot;
        public bool FreeByDefault;
        /// <summary>Quantidade concedida por compra (consumíveis).</summary>
        public int Amount = 1;
        public int MaxStack = 99;
        public GadgetEffect Effect;
        public float EffectValue;
        /// <summary>Cores de apresentação (RGBA hex), usadas por cosméticos.</summary>
        public uint ColorA, ColorB;

        public bool IsConsumable => Category == ItemCategory.Consumable;
        public bool IsEquippable => Slot != null || Category == ItemCategory.Gadget;
    }

    public static class ItemCatalog
    {
        public static string SkinSlot(VehicleKind k) => "skin:" + k;

        public static List<ItemDef> CreateDefault()
        {
            var items = new List<ItemDef>
            {
                // ---- Propulsores (cosméticos: só mudam o jato) ----
                Jet(ItemIds.DefaultJetpack, "Extintor Clássico", "O propulsor original da Nina.", 0, 0xFFD23FFF, 0xE71D36FF, free: true),
                Jet("jet_bolhas", "Soprador de Bolhas", "Solta bolhas em vez de fogo.", 2000, 0x7DF9FFFF, 0xFFFFFFFF),
                Jet("jet_confete", "Canhão de Confete", "Festa a cada impulso.", 3500, 0xFF9F1CFF, 0xB388FFFF),
                Jet("jet_vapor", "Caldeira a Vapor", "Nuvens brancas e muito barulho.", 5000, 0xF6F7FBFF, 0xA8AEC9FF),

                // ---- Visual ----
                Look(ItemIds.DefaultOutfit, "Macacão Verde-água", ItemCategory.Outfit, "outfit", 0, 0x2EC4B6FF, free: true),
                Look("macacao_laranja", "Macacão Laranja", ItemCategory.Outfit, "outfit", 1500, 0xFF9F1CFF),
                Look("macacao_roxo", "Macacão Roxo", ItemCategory.Outfit, "outfit", 2500, 0xB388FFFF),
                Look("macacao_dourado", "Macacão Dourado", ItemCategory.Outfit, "outfit", 9000, 0xFFD23FFF),
                Look("capacete", "Capacete de Obra", ItemCategory.Hat, "hat", 1200, 0xFFD23FFF),
                Look("bone", "Boné da Fábrica", ItemCategory.Hat, "hat", 800, 0xE71D36FF),

                // ---- Gadgets ----
                Gadget("ima_porcas", "Ímã de Porcas", "Puxa porcas num raio de 4 m.", 4000, GadgetEffect.Magnet, 4f),
                Gadget("cinto_lastro", "Cinto de Lastro", "Cai 30% mais rápido.", 1500, GadgetEffect.Gravity, 1.3f),
                Gadget("botas_mola", "Botas de Mola", "Quica ao tocar o chão.", 2500, GadgetEffect.LandingBounce, 9f),
                Gadget("robo_coletor", "Robô Coletor", "Um robozinho pega porcas baixas à sua frente.", 6000, GadgetEffect.Helper, 3f),
                Gadget("ficha_sortuda", "Ficha Sortuda", "A última ficha da Garra sempre ganha algo.", 3000, GadgetEffect.LuckyLastToken, 1f),
                Gadget("para_choque", "Para-choque", "Fica 2 s invulnerável ao perder um veículo.", 2000, GadgetEffect.LongerShield, 2f),

                // ---- Consumíveis ----
                new ItemDef { Id = ItemIds.SecondChance, Name = "Segunda Chance", Description = "Revive uma vez ao morrer.",
                              Category = ItemCategory.Consumable, Price = 1500, MaxStack = 9 },
                new ItemDef { Id = ItemIds.HeadStart, Name = "Arrancada", Description = "Dispara 750 m no início da corrida.",
                              Category = ItemCategory.Consumable, Price = 1000, MaxStack = 9 },
                new ItemDef { Id = ItemIds.TokenPack, Name = "Pacote de Fichas", Description = "+3 fichas para a Garra da Sorte.",
                              Category = ItemCategory.Consumable, Price = 900, Amount = 3, MaxStack = 999 },

                // ---- Melhorias ----
                new ItemDef { Id = ItemIds.GadgetSlot3, Name = "Terceiro Espaço de Gadget", Description = "Equipe 3 gadgets ao mesmo tempo.",
                              Category = ItemCategory.Upgrade, Price = 25000 },
            };

            // ---- Pinturas de veículos (uma dourada por veículo) ----
            foreach (var k in VehicleFactory.All)
                items.Add(new ItemDef
                {
                    Id = "pintura_dourada_" + k.ToString().ToLowerInvariant(),
                    Name = "Pintura Dourada",
                    Description = "Deixa o veículo brilhando.",
                    Category = ItemCategory.VehicleSkin,
                    Price = 3000,
                    Slot = SkinSlot(k),
                    ColorA = 0xFFD23FFF
                });

            return items;
        }

        private static ItemDef Jet(string id, string name, string desc, int price, uint a, uint b, bool free = false) =>
            new ItemDef { Id = id, Name = name, Description = desc, Category = ItemCategory.Jetpack, Price = price,
                          Slot = "jetpack", FreeByDefault = free, ColorA = a, ColorB = b };

        private static ItemDef Look(string id, string name, ItemCategory cat, string slot, int price, uint color, bool free = false) =>
            new ItemDef { Id = id, Name = name, Category = cat, Price = price, Slot = slot, FreeByDefault = free, ColorA = color,
                          Description = cat == ItemCategory.Hat ? "Acessório de cabeça." : "Roupa da Nina." };

        private static ItemDef Gadget(string id, string name, string desc, int price, GadgetEffect e, float v) =>
            new ItemDef { Id = id, Name = name, Description = desc, Category = ItemCategory.Gadget, Price = price,
                          Effect = e, EffectValue = v };
    }
}
