using System.Collections.Generic;
using rjw;
using Verse;

namespace MilkCum.Fluids.Cum.Common;

/// <summary>单次 <see cref="rjw.SexUtility.TransferFluids"/> 内，口交摄入实际体量与记录补丁对齐。</summary>
public static class VirtualSemenRecordLedger
{
    private static readonly List<(Pawn pawn, int loadId, float amt)> Queue = new List<(Pawn, int, float)>();

    public static void BeginTransfer() => Queue.Clear();

    public static void Push(Pawn donor, ISexPartHediff part, float amount)
    {
        if (donor == null || part == null) return;
        if (part.AsHediff is not { } h) return;
        Queue.Add((donor, h.loadID, amount));
    }

    /// <summary>若本帧口交已扣过池， stuffing 复用量体而不二次扣减。</summary>
    public static bool TryPeekLastForPart(Pawn donor, ISexPartHediff part, out float amount)
    {
        amount = 0f;
        if (donor == null || part?.AsHediff is not { } h) return false;
        for (int i = Queue.Count - 1; i >= 0; i--)
        {
            if (Queue[i].pawn == donor && Queue[i].loadId == h.loadID)
            {
                amount = Queue[i].amt;
                return true;
            }
        }

        return false;
    }

    public static float TryMatchAndRemove(Pawn donor, ISexPartHediff part, float fallback)
    {
        if (donor == null || part?.AsHediff is not { } h) return fallback;
        for (int i = Queue.Count - 1; i >= 0; i--)
        {
            if (Queue[i].pawn == donor && Queue[i].loadId == h.loadID)
            {
                float a = Queue[i].amt;
                Queue.RemoveAt(i);
                return a;
            }
        }

        return fallback;
    }

    public static void ClearRemaining() => Queue.Clear();
}
