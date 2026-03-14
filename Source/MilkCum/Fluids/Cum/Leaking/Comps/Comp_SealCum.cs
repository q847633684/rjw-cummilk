using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;
using Verse.Noise;
using RimWorld.QuestGen;
using rjw;
using MilkCum.Fluids.Cum.Cumflation;
using MilkCum.Fluids.Cum;
using MilkCum.Fluids.Cum.Gathering;

namespace MilkCum.Fluids.Cum.Leaking
{
    public class Comp_SealCum : ThingComp
    {
        private CompProperties_SealCum Props => (CompProperties_SealCum)props;
        private bool cumSealed = false;
        private bool canDeflate = true;
        private static readonly CachedTexture Icon1 = new CachedTexture("UI/Plug");
        private static readonly CachedTexture Icon2 = new CachedTexture("UI/DeflateAllowed");
        private static readonly CachedTexture Icon3 = new CachedTexture("UI/DeflateForbidden");

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref cumSealed, "cumSealed", false);
            Scribe_Values.Look(ref canDeflate, "canDeflate", true);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }
            // 濉炰綇/鍏佽娉勭簿浠呴€氳繃濂惰〃鏍兼搷浣滐紝涓嶅啀鍦ㄩ€変腑灏忎汉鏃剁殑 Gizmo 鏍忔樉绀?
        }

        public bool CanDeflate()
        {
            return canDeflate;
        }

        public void SetSealed(bool value) { cumSealed = value; }
        public void SetCanDeflate(bool value) { canDeflate = value; }

        public bool PlayerControlled
        {
            get
            {
                Pawn pawn = (Pawn)parent;
                if (pawn.IsColonist)
                {
                    if (pawn.HostFaction != null)
                    {
                        return pawn.IsSlave;
                    }
                    return true;
                }
                return false;
            }
        }

        public bool IsSealed()
        {
            return canSeal() && cumSealed;
        }

        public bool canSeal()
        {
            Pawn pawn = (Pawn)parent;

            if (pawn.Dead || !PlayerControlled)
            {
                return false;
            }

            var GenitalBPR = Genital_Helper.get_genitalsBPR(pawn);
            IEnumerable<ISexPartHediff> vaginas = Genital_Helper.get_PartsHediffList(pawn, GenitalBPR).Where(x => Genital_Helper.is_vagina(x)).Cast<ISexPartHediff>();
            if (!vaginas.Any())
            {
                return false;
            }

            if (!vaginas.Any(v => !v.Def.partTags.Contains("Resizable")))
            {
                return true;
            }

            if (pawn.health.hediffSet.HasHediff(DefOfs.Cumpilation_Sealed))
            {
                return true;
            }

            return false;
        }

    }
}
