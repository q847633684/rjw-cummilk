using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace Milk
{
    public static class PawnExtensions
    {
        public static bool HasMilk(this Pawn pawn)
        {
            var phs = pawn.health.hediffSet;
            bool isLactatingBT = false;
            if (ModsConfig.BiotechActive)
                isLactatingBT = phs.HasHediff(HediffDef.Named("Lactating"));
            bool isLactatingDrug = phs.HasHediff(HediffDef.Named("Lactating_Drug"));
            bool isLactatingNatural = phs.HasHediff(HediffDef.Named("Lactating_Natural"));
            bool isLactatingPermanent = phs.HasHediff(HediffDef.Named("Lactating_Permanent"));
            bool isHeavyLactatingPermanent = phs.HasHediff(HediffDef.Named("Heavy_Lactating_Permanent"));

            return (isLactatingBT || isLactatingDrug || isLactatingNatural || isLactatingPermanent || isHeavyLactatingPermanent);
        }

        public static float MilkFullness(this Pawn pawn)
        {
            var comp = ThingCompUtility.TryGetComp<CompMilkableHuman>(pawn);
            return comp.Fullness;
        }

        public static PawnData GetMilkPawnData(this Pawn pawn)
        {
            return SaveStorage.DataStore.GetPawnData(pawn);
        }
    }
}
