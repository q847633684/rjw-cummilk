using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace EqualMilking.Helpers;

internal static class VersionDiffHelper
{
    internal static bool IsNormalAnimal(this Pawn pawn)
    {
#if v1_5
        return pawn.IsNonMutantAnimal;
#else
        return pawn.IsAnimal && !pawn.IsMutant;
#endif
    }
    internal static void ResizeTo(this Texture2D texture, int width, int height, TextureFormat format, bool hasMipMap)
    {
#if v1_5
        texture.Resize(width, height, format, hasMipMap);
#else
        texture.Reinitialize(width, height, format, hasMipMap);
#endif
    }
    internal static void AddSuckleTickAction(this Toil toil, Pawn pawn, Pawn baby, Action readyForNextToil)
    {
#if v1_5
        toil.tickAction = delegate
        {
            pawn.GainComfortFromCellIfPossible();
            if (!ChildcareUtility.SuckleFromLactatingPawn(baby, pawn))
            {
                readyForNextToil.Invoke();
            }
        };
#else
        toil.tickIntervalAction = delegate (int interval)
        {
            pawn.GainComfortFromCellIfPossible(interval);
            if (!ChildcareUtility.SuckleFromLactatingPawn(baby, pawn, interval))
            {
                readyForNextToil.Invoke();
            }
        };
#endif
    }
}