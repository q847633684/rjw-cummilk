using HarmonyLib;
using rjw;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Cumpilation.Fluids.Slug
{
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
    public static class Patch_OnDeath_TriggerSlugExplosions
    {
        public static void Postfix(Pawn __instance)
        {
            // DevNote: Pawn.Kill also first checks for corpse to be not null before triggering DeathWorkers; Kill might not really "kill" the pawn.
            if (__instance == null) return;
            if (__instance.Corpse == null) return;
            if (__instance.health == null) return;

            foreach (Hediff hediff in __instance.health.hediffSet.hediffs) {
                HediffComp_SlugExplosionOnDeath comp  = hediff.TryGetComp<HediffComp_SlugExplosionOnDeath>();
                if (comp != null) {
                    comp.TriggerExplosion();
                }
            }
        }
    }
}
