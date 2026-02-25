using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace EqualMilking.Comps;

/// <summary>
/// 精液桶关联床：可链接到一张床，床主在桶空时默认唯一可用；有占用时提示“不属于他的精液桶”。
/// 共享卧室（房间内多床或无主床）时默认全员可用，排放为混合精液。
/// </summary>
public class CompCumBucketLink : ThingComp
{
    public const string CumThingDefName = "Cumpilation_Cum";

    private Building_Bed _linkedBed;
    private List<Pawn> _usedByPawns = new();

    public Building_Bed LinkedBed
    {
        get => _linkedBed;
        set => _linkedBed = value;
    }

    public List<Pawn> UsedByPawns => _usedByPawns;

    /// <summary>床主（床的 AssigningPawn）。</summary>
    public Pawn Owner => _linkedBed?.AssigningPawn;

    /// <summary>共享卧室：房间内多张床，或链接的床无主。</summary>
    public bool IsSharedRoom
    {
        get
        {
            if (_linkedBed == null) return true;
            if (_linkedBed.AssigningPawn == null) return true;
            var room = _linkedBed.GetRoom();
            if (room == null) return false;
            int bedCount = room.ContainedAndAdjacentThings.OfType<Building_Bed>().Count();
            return bedCount > 1;
        }
    }

    /// <summary>桶格上有精液物品即视为有占用。</summary>
    public bool HasContent
    {
        get
        {
            if (parent?.Map == null) return false;
            var things = parent.Map.thingGrid.ThingsListAt(parent.Position);
            for (int i = 0; i < things.Count; i++)
            {
                if (things[i].def?.defName == CumThingDefName)
                    return true;
            }
            return false;
        }
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_References.Look(ref _linkedBed, "linkedBed");
        Scribe_Collections.Look(ref _usedByPawns, "usedByPawns", LookMode.Reference);
        if (_usedByPawns == null) _usedByPawns = new List<Pawn>();
    }

    public override void CompTick()
    {
        base.CompTick();
        if (parent.IsHashIntervalTick(60) && !HasContent && _usedByPawns.Count > 0)
            _usedByPawns.Clear();
    }

    /// <summary>该小人是否被允许使用此桶（做 DeflateBucket 等）。</summary>
    public bool CanPawnUse(Pawn pawn)
    {
        if (pawn == null) return false;
        if (IsSharedRoom) return true;
        if (Owner == null) return true;
        if (!HasContent) return pawn == Owner;
        if (pawn == Owner && _usedByPawns.Count == 1 && _usedByPawns[0] == Owner) return true;
        return false;
    }

    /// <summary>是否应提示“不属于他的精液桶”（有占用且不是他的桶）。</summary>
    public bool ShouldWarnNotYourBucket(Pawn pawn)
    {
        if (pawn == null || !HasContent) return false;
        if (IsSharedRoom) return false;
        if (Owner == null) return false;
        if (pawn == Owner && _usedByPawns.Count == 1 && _usedByPawns[0] == Owner) return false;
        return true;
    }

    /// <summary>记录该小人向此桶排放了一次；若多人用过则后续产出为混合精液。</summary>
    public void NotifyPawnUsed(Pawn pawn)
    {
        if (pawn == null) return;
        if (!_usedByPawns.Contains(pawn))
            _usedByPawns.Add(pawn);
    }

    /// <summary>是否已有多人使用过（产出应为混合精液）。</summary>
    public bool IsMixed => _usedByPawns.Count > 1;

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (var g in base.CompGetGizmosExtra())
            yield return g;

        if (parent.Faction != Faction.OfPlayer) yield break;

        if (_linkedBed != null)
        {
            yield return new Command_Action
            {
                defaultLabel = "EM_CumBucket_UnlinkBed".Translate(),
                defaultDesc = "EM_CumBucket_UnlinkBed_Desc".Translate(),
                icon = TexCommand.ClearPrioritizedWork,
                action = () => _linkedBed = null
            };
            yield return new Command_Action
            {
                defaultLabel = "EM_CumBucket_LinkBed".Translate(),
                defaultDesc = "EM_CumBucket_LinkBed_Desc".Translate(),
                icon = TexCommand.Attack,
                action = () => Find.WindowStack.Add(new Dialog_SelectBedForBucket(parent.Map, this))
            };
        }
        else
        {
            yield return new Command_Action
            {
                defaultLabel = "EM_CumBucket_LinkBed".Translate(),
                defaultDesc = "EM_CumBucket_LinkBed_Desc".Translate(),
                icon = TexCommand.Attack,
                action = () => Find.WindowStack.Add(new Dialog_SelectBedForBucket(parent.Map, this))
            };
        }
    }

    public override string CompInspectStringExtra()
    {
        if (_linkedBed == null)
            return "EM_CumBucket_NotLinked".Translate();
        if (IsSharedRoom)
            return "EM_CumBucket_SharedRoom".Translate();
        if (Owner != null)
            return "EM_CumBucket_Owner".Translate(Owner.LabelShort);
        return "EM_CumBucket_UnassignedBed".Translate();
    }
}
