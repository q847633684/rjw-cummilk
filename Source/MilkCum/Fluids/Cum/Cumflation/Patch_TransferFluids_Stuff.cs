using HarmonyLib;
using rjw;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MilkCum.Fluids.Cum.Cumflation
{
    [HarmonyPatch(typeof(SexUtility), nameof(SexUtility.TransferFluids))]
    public static class Patch_TransferFluids_Stuff
    {
        public static void Postfix(SexProps props)
        {
            if (Settings.EnableStuffing)
            {
                if (!props.isReceiver && !props.isRevese)
                {
                    // Case A: The pawn fucks the partner, and shoots.
                    StuffingUtility.StuffPawn(props.pawn, props.partner, props);
                }
                if (props.isReceiver && props.isRevese)
                {
                    // Case B: The pawn reverse-fucks the partner, and the partner shoots.
                    StuffingUtility.StuffPawn(props.pawn, props.partner, props);
                }
            }
        }
    }

}
